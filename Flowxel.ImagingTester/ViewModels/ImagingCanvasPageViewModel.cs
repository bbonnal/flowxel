using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Flowxel.Core.Geometry.Primitives;
using Flowxel.Core.Geometry.Shapes;
using Flowxel.Graph;
using Flowxel.Imaging.Operations.Constructions;
using Flowxel.Imaging.Operations.Extractions;
using Flowxel.Imaging.Operations.Filters;
using Flowxel.Imaging.Operations.IO;
using OpenCvSharp;
using Flowxel.UI.Controls;
using Flowxel.UI.Controls.Processing;
using Flowxel.UI.Services;
using Flowxel.UI.Controls.Drawing;
using Flowxel.UI.Controls.Drawing.Scene;
using Flowxel.UI.Controls.Drawing.Shapes;
using Shape = Flowxel.Core.Geometry.Shapes.Shape;
using FlowLine = Flowxel.Core.Geometry.Shapes.Line;
using FlowPoint = Flowxel.Core.Geometry.Shapes.Point;
using FlowRectangle = Flowxel.Core.Geometry.Shapes.Rectangle;

namespace Flowxel.ImagingTester.ViewModels;

public partial class ImagingCanvasPageViewModel : ViewModelBase
{
    private readonly IContentDialogService _dialogService;
    private readonly IFileDialogService _fileDialogService;
    private readonly IInfoBarService _infoBarService;
    private readonly ISceneSerializer _sceneSerializer = new JsonSceneSerializer();

    private readonly Dictionary<string, string> _loadPathByNodeId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _gaussianKernelSizeByNodeId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, double> _gaussianSigmaByNodeId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _extractRoiShapeIdByNodeId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ShapeOutputProvenance> _shapeProvenanceByShapeId = new(StringComparer.Ordinal);
    private readonly HashSet<string> _bindingSelectedShapeIds = new(StringComparer.Ordinal);
    private readonly List<ProcessPortDescriptor> _pendingBindPorts = [];

    private int _nodeCounter;
    private string? _pendingRoiNodeId;
    private string? _pendingBindNodeId;
    private int _pendingBindPortIndex;

    public ImagingCanvasPageViewModel(
        IContentDialogService dialogService,
        IFileDialogService fileDialogService,
        IInfoBarService infoBarService)
    {
        _dialogService = dialogService;
        _fileDialogService = fileDialogService;
        _infoBarService = infoBarService;

        SelectToolCommand = new RelayCommand(() =>
        {
            ActiveTool = DrawingTool.Select;
            if (InteractionMode != DrawingInteractionMode.Bind)
                InteractionMode = DrawingInteractionMode.Standard;
        });
        SelectRectangleToolCommand = new RelayCommand(() =>
        {
            ActiveTool = DrawingTool.CenterlineRectangle;
            InteractionMode = DrawingInteractionMode.Draw;
        });

        AddLoadImageOperationCommand = new RelayCommand(AddLoadImageOperation);
        AddGaussianBlurOperationCommand = new RelayCommand(AddGaussianBlurOperation);
        AddExtractLineInRegionOperationCommand = new RelayCommand(AddExtractLineInRegionOperation);
        AddConstructLineLineIntersectionOperationCommand = new RelayCommand(AddConstructLineLineIntersectionOperation);
        RemoveSelectedOperationCommand = new RelayCommand(RemoveSelectedOperation);
        ClearPipelineCommand = new RelayCommand(ClearPipeline);

        RunPipelineCommand = new AsyncRelayCommand(RunPipelineAsync);
        ClearComputedResultsCommand = new RelayCommand(ClearComputedResults);

        ConnectPortsCommand = new RelayCommand(ConnectPorts);
        RemoveConnectionCommand = new RelayCommand(RemoveSelectedConnection);
        BeginBindSelectedOperationCommand = new RelayCommand(BeginBindSelectedOperation);
        BindFromCanvasShapeCommand = new RelayCommand<string?>(BindFromCanvasShape);
        ViewResourceCommand = new AsyncRelayCommand(ViewSelectedResourceAsync);
        ClearResourcesCommand = new RelayCommand(ClearResources);

        DrawRoiForSelectedOperationCommand = new RelayCommand(BeginRoiDrawingForSelectedOperation);
        RemoveRoiForSelectedOperationCommand = new RelayCommand(RemoveRoiForSelectedOperation);
        BrowseLoadPathForSelectedOperationCommand = new AsyncRelayCommand(BrowseLoadPathForSelectedOperationAsync);
        OpenResourceInspectorCommand = new AsyncRelayCommand(OpenResourceInspectorAsync);

        SaveSceneCommand = new AsyncRelayCommand(SaveSceneAsync);
        LoadSceneCommand = new AsyncRelayCommand(LoadSceneAsync);

        ClearCanvasCommand = new RelayCommand(ClearCanvas);
        ResetViewCommand = new RelayCommand(ResetView);

        Shapes.CollectionChanged += OnShapesCollectionChanged;
        ComputedShapeIds.CollectionChanged += OnComputedShapeIdsCollectionChanged;
        Resources.CollectionChanged += OnResourcesCollectionChanged;
        BindingCandidateShapeIds.CollectionChanged += OnBindingCandidateShapeIdsCollectionChanged;
        ProcessNodes.CollectionChanged += OnProcessNodesCollectionChanged;

        DiscoverAvailableOperations();
        InitializeDefaultPipelinePreset();
        RefreshSelectedNodeProperties();
    }

    public IContentDialogService DialogService => _dialogService;

    public IInfoBarService InfoBarService => _infoBarService;

    public ObservableCollection<Shape> Shapes { get; } = [];

    public ObservableCollection<string> ComputedShapeIds { get; } = [];

    public ObservableCollection<ProcessNodeDescriptor> ProcessNodes { get; } = [];

    public ObservableCollection<ProcessLinkDescriptor> ProcessLinks { get; } = [];

    public ObservableCollection<ResourceEntryDescriptor> Resources { get; } = [];

    public ObservableCollection<ProcessPortDescriptor> AvailableOutputPorts { get; } = [];

    public ObservableCollection<ProcessPortDescriptor> AvailableInputPorts { get; } = [];

    public ObservableCollection<string> AvailableFlowxelOperations { get; } = [];

    [ObservableProperty]
    private ProcessNodeDescriptor? selectedProcessNode;

    [ObservableProperty]
    private ProcessNodeDescriptor? selectedFromOperation;

    [ObservableProperty]
    private ProcessNodeDescriptor? selectedToOperation;

    [ObservableProperty]
    private ProcessPortDescriptor? selectedFromPort;

    [ObservableProperty]
    private ProcessPortDescriptor? selectedToPort;

    [ObservableProperty]
    private ProcessLinkDescriptor? selectedProcessLink;

    [ObservableProperty]
    private ResourceEntryDescriptor? selectedResource;

    [ObservableProperty]
    private DrawingTool activeTool = DrawingTool.Select;

    [ObservableProperty]
    private double zoom = 1d;

    [ObservableProperty]
    private global::Avalonia.Vector pan;

    [ObservableProperty]
    private global::Avalonia.Point cursorAvaloniaPosition;

    [ObservableProperty]
    private global::Avalonia.Point cursorCanvasPosition;

    [ObservableProperty]
    private string selectedLoadPath = string.Empty;

    [ObservableProperty]
    private string selectedGaussianKernelSize = "5";

    [ObservableProperty]
    private string selectedGaussianSigma = "1.0";

    [ObservableProperty]
    private string selectedExtractRoiStatus = "ROI: not configured";

    [ObservableProperty]
    private bool canEditLoadPath;

    [ObservableProperty]
    private bool canEditGaussian;

    [ObservableProperty]
    private bool canConfigureExtractLine;

    [ObservableProperty]
    private bool canRemoveSelectedOperation;

    [ObservableProperty]
    private DrawingInteractionMode interactionMode = DrawingInteractionMode.Standard;

    public IRelayCommand SelectToolCommand { get; }
    public IRelayCommand SelectRectangleToolCommand { get; }

    public IRelayCommand AddLoadImageOperationCommand { get; }
    public IRelayCommand AddGaussianBlurOperationCommand { get; }
    public IRelayCommand AddExtractLineInRegionOperationCommand { get; }
    public IRelayCommand AddConstructLineLineIntersectionOperationCommand { get; }
    public IRelayCommand RemoveSelectedOperationCommand { get; }
    public IRelayCommand ClearPipelineCommand { get; }

    public IAsyncRelayCommand RunPipelineCommand { get; }
    public IRelayCommand ClearComputedResultsCommand { get; }

    public IRelayCommand ConnectPortsCommand { get; }
    public IRelayCommand RemoveConnectionCommand { get; }
    public IRelayCommand BeginBindSelectedOperationCommand { get; }
    public IRelayCommand<string?> BindFromCanvasShapeCommand { get; }
    public IAsyncRelayCommand ViewResourceCommand { get; }
    public IRelayCommand ClearResourcesCommand { get; }

    public IRelayCommand DrawRoiForSelectedOperationCommand { get; }
    public IRelayCommand RemoveRoiForSelectedOperationCommand { get; }
    public IAsyncRelayCommand BrowseLoadPathForSelectedOperationCommand { get; }
    public IAsyncRelayCommand OpenResourceInspectorCommand { get; }

    public IAsyncRelayCommand SaveSceneCommand { get; }
    public IAsyncRelayCommand LoadSceneCommand { get; }

    public IRelayCommand ClearCanvasCommand { get; }
    public IRelayCommand ResetViewCommand { get; }

    public ObservableCollection<string> BindingCandidateShapeIds { get; } = [];

    public string StatusText
    {
        get
        {
            var baseStatus =
                $"Tool: {ActiveTool} | Shapes: {Shapes.Count} (Computed: {ComputedShapeIds.Count}) | Ops: {ProcessNodes.Count} | Links: {ProcessLinks.Count} | Resources: {Resources.Count}";

            if (_pendingBindNodeId is null || _pendingBindPortIndex < 0 || _pendingBindPortIndex >= _pendingBindPorts.Count)
                return baseStatus;

            var port = _pendingBindPorts[_pendingBindPortIndex];
            return $"{baseStatus} | Bind: {port.Name} ({port.TypeName}) [{BindingCandidateShapeIds.Count} candidate(s)]";
        }
    }

    public string PipelineSummary => ProcessNodes.Count == 0
        ? "No operation in pipeline"
        : string.Join(" -> ", ProcessNodes.Select(p => p.Name));

    partial void OnActiveToolChanged(DrawingTool value) => OnPropertyChanged(nameof(StatusText));
    partial void OnInteractionModeChanged(DrawingInteractionMode value) => OnPropertyChanged(nameof(StatusText));
    partial void OnZoomChanged(double value) => OnPropertyChanged(nameof(StatusText));
    partial void OnPanChanged(global::Avalonia.Vector value) => OnPropertyChanged(nameof(StatusText));
    partial void OnCursorCanvasPositionChanged(global::Avalonia.Point value) => OnPropertyChanged(nameof(StatusText));

    partial void OnSelectedProcessNodeChanged(ProcessNodeDescriptor? value)
    {
        if (_pendingBindNodeId is not null && !string.Equals(_pendingBindNodeId, value?.Id, StringComparison.Ordinal))
            CancelBindingSession();
        if (_pendingRoiNodeId is not null && !string.Equals(_pendingRoiNodeId, value?.Id, StringComparison.Ordinal))
        {
            _pendingRoiNodeId = null;
            ActiveTool = DrawingTool.Select;
            if (InteractionMode != DrawingInteractionMode.Bind)
                InteractionMode = DrawingInteractionMode.Standard;
        }

        RefreshSelectedNodeProperties();
        if (value is not null)
        {
            SelectedToOperation ??= value;
            TryAutoSetupSelectedExtractOperation(value);
        }
    }

    partial void OnSelectedFromOperationChanged(ProcessNodeDescriptor? value) => RefreshOutputPorts();

    partial void OnSelectedToOperationChanged(ProcessNodeDescriptor? value) => RefreshInputPorts();

    partial void OnSelectedLoadPathChanged(string value)
    {
        if (SelectedProcessNode?.OperationType == "LoadOperation")
            _loadPathByNodeId[SelectedProcessNode.Id] = value.Trim();
    }

    partial void OnSelectedGaussianKernelSizeChanged(string value)
    {
        if (SelectedProcessNode?.OperationType != "GaussianBlurOperation")
            return;

        if (int.TryParse(value, out var parsed) && parsed > 0)
            _gaussianKernelSizeByNodeId[SelectedProcessNode.Id] = parsed;
    }

    partial void OnSelectedGaussianSigmaChanged(string value)
    {
        if (SelectedProcessNode?.OperationType != "GaussianBlurOperation")
            return;

        if (double.TryParse(value, out var parsed) && parsed > 0)
            _gaussianSigmaByNodeId[SelectedProcessNode.Id] = parsed;
    }

    private void DiscoverAvailableOperations()
    {
        AvailableFlowxelOperations.Clear();

        var operationTypes = Assembly.GetAssembly(typeof(LoadOperation))?
            .GetTypes()
            .Where(type =>
                type.IsClass &&
                !type.IsAbstract &&
                typeof(IExecutableNode).IsAssignableFrom(type) &&
                (type.Namespace ?? string.Empty).Contains("Flowxel.Imaging.Operations", StringComparison.Ordinal))
            .OrderBy(type => type.Name)
            .Select(type => type.Name)
            .ToList() ?? [];

        foreach (var name in operationTypes)
            AvailableFlowxelOperations.Add(name);
    }

    private void AddLoadImageOperation() => AddOperationNode(CreateLoadDescriptor());

    private void AddGaussianBlurOperation() => AddOperationNode(CreateGaussianDescriptor());

    private void AddExtractLineInRegionOperation() => AddOperationNode(CreateExtractLineDescriptor());

    private void AddConstructLineLineIntersectionOperation() => AddOperationNode(CreateConstructLineLineIntersectionDescriptor());

    private void AddOperationNode(ProcessNodeDescriptor descriptor)
    {
        ProcessNodes.Add(descriptor);
        SelectedProcessNode = descriptor;
        SelectedFromOperation = descriptor;
        SelectedToOperation = descriptor;
    }

    private ProcessNodeDescriptor CreateLoadDescriptor()
    {
        var nodeId = NextNodeId("load");
        _loadPathByNodeId[nodeId] = string.Empty;

        return new ProcessNodeDescriptor
        {
            Id = nodeId,
            Name = "loadImage",
            OperationType = "LoadOperation",
            Inputs =
            [
                new ProcessPortDescriptor { Key = "path", Name = "Path", TypeName = "string", Direction = ProcessPortDirection.Input }
            ],
            Outputs =
            [
                new ProcessPortDescriptor { Key = "out", Name = "Image", TypeName = "Mat", Direction = ProcessPortDirection.Output }
            ]
        };
    }

    private ProcessNodeDescriptor CreateGaussianDescriptor()
    {
        var nodeId = NextNodeId("gaussian");
        _gaussianKernelSizeByNodeId[nodeId] = 5;
        _gaussianSigmaByNodeId[nodeId] = 1.0;

        return new ProcessNodeDescriptor
        {
            Id = nodeId,
            Name = "GaussianBlur",
            OperationType = "GaussianBlurOperation",
            Inputs =
            [
                new ProcessPortDescriptor { Key = "in", Name = "Input", TypeName = "Mat", Direction = ProcessPortDirection.Input }
            ],
            Outputs =
            [
                new ProcessPortDescriptor { Key = "out", Name = "Image", TypeName = "Mat", Direction = ProcessPortDirection.Output }
            ]
        };
    }

    private ProcessNodeDescriptor CreateExtractLineDescriptor()
    {
        var nodeId = NextNodeId("extract-line");

        return new ProcessNodeDescriptor
        {
            Id = nodeId,
            Name = "extractLineInRegion",
            OperationType = "ExtractLineInRegionsOperation",
            Inputs =
            [
                new ProcessPortDescriptor { Key = "in", Name = "Input", TypeName = "Mat", Direction = ProcessPortDirection.Input },
                new ProcessPortDescriptor { Key = "region", Name = "Region", TypeName = "Rectangle", Direction = ProcessPortDirection.Input }
            ],
            Outputs =
            [
                new ProcessPortDescriptor { Key = "out", Name = "Line", TypeName = "Line", Direction = ProcessPortDirection.Output }
            ]
        };
    }

    private ProcessNodeDescriptor CreateConstructLineLineIntersectionDescriptor()
    {
        var nodeId = NextNodeId("line-line-intersection");

        return new ProcessNodeDescriptor
        {
            Id = nodeId,
            Name = "constructLineLineIntersection",
            OperationType = "ConstructLineLineIntersectionOperation",
            Inputs =
            [
                new ProcessPortDescriptor { Key = "first", Name = "First", TypeName = "Line", Direction = ProcessPortDirection.Input },
                new ProcessPortDescriptor { Key = "second", Name = "Second", TypeName = "Line", Direction = ProcessPortDirection.Input }
            ],
            Outputs =
            [
                new ProcessPortDescriptor { Key = "out", Name = "Point", TypeName = "Point", Direction = ProcessPortDirection.Output }
            ]
        };
    }

    private string NextNodeId(string prefix)
    {
        _nodeCounter++;
        return $"{prefix}-{_nodeCounter}";
    }

    private void InitializeDefaultPipelinePreset()
    {
        if (ProcessNodes.Count > 0)
            return;

        const string defaultPath = "/home/benou/Downloads/test.tiff";
        const int fallbackWidth = 1920;
        const int fallbackHeight = 1080;

        var loadDescriptor = CreateLoadDescriptor();
        var extractDescriptor = CreateExtractLineDescriptor();

        ProcessNodes.Add(loadDescriptor);
        ProcessNodes.Add(extractDescriptor);

        _loadPathByNodeId[loadDescriptor.Id] = defaultPath;

        ProcessLinks.Add(new ProcessLinkDescriptor
        {
            FromNodeId = loadDescriptor.Id,
            FromPortKey = "out",
            ToNodeId = extractDescriptor.Id,
            ToPortKey = "in"
        });

        var (width, height) = TryReadImageDimensions(defaultPath)
            ? (_lastReadWidth, _lastReadHeight)
            : (fallbackWidth, fallbackHeight);

        var roi = new CenterlineRectangleShape
        {
            Pose = new Pose(new Vector(0, height * 0.5), new Vector(1, 0)),
            Length = width,
            Width = height
        };

        Shapes.Add(roi);
        _extractRoiShapeIdByNodeId[extractDescriptor.Id] = roi.Id;

        SelectedFromOperation = loadDescriptor;
        SelectedToOperation = extractDescriptor;
        SelectedFromPort = loadDescriptor.Outputs.FirstOrDefault(port => port.Key == "out");
        SelectedToPort = extractDescriptor.Inputs.FirstOrDefault(port => port.Key == "in");
        SelectedProcessNode = extractDescriptor;

    }

    private int _lastReadWidth;
    private int _lastReadHeight;

    private bool TryReadImageDimensions(string path)
    {
        try
        {
            if (!File.Exists(path))
                return false;

            using var mat = Cv2.ImRead(path, ImreadModes.Grayscale);
            if (mat.Empty())
                return false;

            _lastReadWidth = mat.Width;
            _lastReadHeight = mat.Height;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void RemoveSelectedOperation()
    {
        if (SelectedProcessNode is null)
            return;

        if (string.Equals(_pendingBindNodeId, SelectedProcessNode.Id, StringComparison.Ordinal))
            CancelBindingSession();

        if (_extractRoiShapeIdByNodeId.TryGetValue(SelectedProcessNode.Id, out var roiShapeId))
        {
            var roiShape = Shapes.FirstOrDefault(shape => shape.Id == roiShapeId);
            if (roiShape is not null)
                Shapes.Remove(roiShape);
        }

        _extractRoiShapeIdByNodeId.Remove(SelectedProcessNode.Id);
        _loadPathByNodeId.Remove(SelectedProcessNode.Id);
        _gaussianKernelSizeByNodeId.Remove(SelectedProcessNode.Id);
        _gaussianSigmaByNodeId.Remove(SelectedProcessNode.Id);

        for (var i = ProcessLinks.Count - 1; i >= 0; i--)
        {
            var link = ProcessLinks[i];
            if (link.FromNodeId == SelectedProcessNode.Id || link.ToNodeId == SelectedProcessNode.Id)
                ProcessLinks.RemoveAt(i);
        }

        ProcessNodes.Remove(SelectedProcessNode);
        SelectedProcessNode = ProcessNodes.FirstOrDefault();
    }

    private void ClearPipeline()
    {
        CancelBindingSession();

        foreach (var roiShapeId in _extractRoiShapeIdByNodeId.Values.ToArray())
        {
            var roiShape = Shapes.FirstOrDefault(shape => shape.Id == roiShapeId);
            if (roiShape is not null)
                Shapes.Remove(roiShape);
        }

        ProcessNodes.Clear();
        ProcessLinks.Clear();
        _extractRoiShapeIdByNodeId.Clear();
        _loadPathByNodeId.Clear();
        _gaussianKernelSizeByNodeId.Clear();
        _gaussianSigmaByNodeId.Clear();
        _pendingRoiNodeId = null;

        SelectedProcessNode = null;
        SelectedFromOperation = null;
        SelectedToOperation = null;
        SelectedFromPort = null;
        SelectedToPort = null;

        RefreshSelectedNodeProperties();
    }

    private async Task RunPipelineAsync()
    {
        if (!await EnsureOpenCvRuntimeAvailableAsync())
            return;

        if (ProcessNodes.Count == 0)
            return;

        ClearComputedResults();
        ClearResources();

        try
        {
            var pool = new ResourcePool();
            var graph = new Graph<IExecutableNode>();
            var nodeInstances = CreateExecutableNodes(pool, graph);

            var setupErrors = ConnectGraph(graph, nodeInstances);

            var executionErrors = await ExecuteGraphWithBranchIsolationAsync(graph, nodeInstances, pool);
            executionErrors.InsertRange(0, setupErrors);
            if (executionErrors.Count > 0)
            {
                var first = executionErrors[0];
                var message = executionErrors.Count == 1
                    ? first
                    : $"{first} (+{executionErrors.Count - 1} other error(s))";
                await _infoBarService.ShowAsync(infoBar =>
                {
                    infoBar.Severity = InfoBarSeverity.Error;
                    infoBar.Title = "Pipeline partially failed";
                    infoBar.Message = message;
                });
            }
        }
        catch (Exception ex)
        {
            await _infoBarService.ShowAsync(infoBar =>
            {
                infoBar.Severity = InfoBarSeverity.Error;
                infoBar.Title = "Pipeline execution failed";
                infoBar.Message = ex.Message;
            });
        }
    }

    private async Task<List<string>> ExecuteGraphWithBranchIsolationAsync(
        Graph<IExecutableNode> graph,
        IReadOnlyDictionary<string, IExecutableNode> nodes,
        ResourcePool pool)
    {
        var errors = new List<string>();
        var descriptorByRuntimeId = nodes.ToDictionary(
            pair => pair.Value.Id,
            pair => ProcessNodes.First(node => node.Id == pair.Key));
        var runtimeNodeById = nodes.Values.ToDictionary(node => node.Id);

        var remainingIncoming = runtimeNodeById.Keys.ToDictionary(
            id => id,
            id => graph.GetIncomingEdges(id).Count());
        var blockedByFailure = runtimeNodeById.Keys.ToDictionary(id => id, _ => false);
        var ready = new Queue<Guid>(remainingIncoming.Where(pair => pair.Value == 0).Select(pair => pair.Key));

        while (ready.Count > 0)
        {
            var batch = new List<Guid>();
            while (ready.Count > 0)
                batch.Add(ready.Dequeue());

            var batchResults = await Task.WhenAll(batch.Select(async runtimeId =>
            {
                if (blockedByFailure[runtimeId])
                    return (RuntimeId: runtimeId, Success: false, Error: (string?)null);

                var node = runtimeNodeById[runtimeId];
                var descriptor = descriptorByRuntimeId[runtimeId];
                if (!TryApplyOperationParameters(descriptor, node, out var parameterError))
                    return (RuntimeId: runtimeId, Success: false, Error: parameterError);

                try
                {
                    await node.Execute();
                    CollectNodeOutput(descriptor, node, pool);
                    return (RuntimeId: runtimeId, Success: true, Error: (string?)null);
                }
                catch (Exception ex)
                {
                    return (RuntimeId: runtimeId, Success: false, Error: $"{descriptor.Name}: {ex.Message}");
                }
            }));

            var failedThisBatch = new HashSet<Guid>();
            foreach (var result in batchResults)
            {
                if (result.Success)
                    continue;

                failedThisBatch.Add(result.RuntimeId);
                if (!string.IsNullOrWhiteSpace(result.Error))
                    errors.Add(result.Error);
            }

            foreach (var runtimeId in batch)
            {
                foreach (var edge in graph.GetOutgoingEdges(runtimeId))
                {
                    if (!remainingIncoming.ContainsKey(edge.ToNodeId))
                        continue;

                    if (failedThisBatch.Contains(runtimeId) || blockedByFailure[runtimeId])
                        blockedByFailure[edge.ToNodeId] = true;

                    remainingIncoming[edge.ToNodeId]--;
                    if (remainingIncoming[edge.ToNodeId] == 0)
                        ready.Enqueue(edge.ToNodeId);
                }
            }
        }

        return errors;
    }

    private Dictionary<string, IExecutableNode> CreateExecutableNodes(ResourcePool pool, Graph<IExecutableNode> graph)
    {
        var map = new Dictionary<string, IExecutableNode>(StringComparer.Ordinal);

        foreach (var descriptor in ProcessNodes)
        {
            IExecutableNode node = descriptor.OperationType switch
            {
                "LoadOperation" => new LoadOperation(pool, graph),
                "GaussianBlurOperation" => new GaussianBlurOperation(pool, graph),
                "ExtractLineInRegionsOperation" => new ExtractLineInRegionsOperation(pool, graph),
                "ConstructLineLineIntersectionOperation" => new ConstructLineLineIntersectionOperation(pool, graph),
                _ => throw new InvalidOperationException($"Unsupported operation type: {descriptor.OperationType}")
            };

            graph.AddNode(node);
            map[descriptor.Id] = node;
        }

        return map;
    }

    private bool TryApplyOperationParameters(ProcessNodeDescriptor descriptor, IExecutableNode node, out string? error)
    {
        error = null;

        switch (descriptor.OperationType)
        {
            case "LoadOperation":
            {
                var path = _loadPathByNodeId.GetValueOrDefault(descriptor.Id)?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(path))
                {
                    error = $"{descriptor.Name}: missing Load.Path.";
                    return false;
                }

                if (!File.Exists(path))
                {
                    error = $"{descriptor.Name}: path does not exist ({path}).";
                    return false;
                }

                node.Parameters["Path"] = path;
                return true;
            }
            case "GaussianBlurOperation":
            {
                var kernelSize = _gaussianKernelSizeByNodeId.GetValueOrDefault(descriptor.Id, 5);
                if (kernelSize <= 0)
                    kernelSize = 5;
                if (kernelSize % 2 == 0)
                    kernelSize++;

                var sigma = _gaussianSigmaByNodeId.GetValueOrDefault(descriptor.Id, 1.0);
                if (sigma <= 0)
                    sigma = 1.0;

                node.Parameters["KernelSize"] = kernelSize;
                node.Parameters["Sigma"] = sigma;
                return true;
            }
            case "ExtractLineInRegionsOperation":
            {
                if (!_extractRoiShapeIdByNodeId.TryGetValue(descriptor.Id, out var roiShapeId))
                {
                    error = $"{descriptor.Name}: missing ROI.";
                    return false;
                }

                var roiShape = Shapes.FirstOrDefault(shape => shape.Id == roiShapeId);
                if (!TryMapRoiShapeToRectangle(roiShape, out var rectangle))
                {
                    error = $"{descriptor.Name}: invalid ROI binding.";
                    return false;
                }

                node.Parameters["Region"] = rectangle;
                return true;
            }
        }

        return true;
    }

    private List<string> ConnectGraph(Graph<IExecutableNode> graph, IReadOnlyDictionary<string, IExecutableNode> nodes)
    {
        var errors = new List<string>();

        foreach (var link in ProcessLinks)
        {
            if (!nodes.TryGetValue(link.FromNodeId, out var from) || !nodes.TryGetValue(link.ToNodeId, out var to))
            {
                errors.Add($"Invalid connection ignored: {link.DisplayLabel}");
                continue;
            }

            try
            {
                graph.Connect(from, to, link.FromPortKey, link.ToPortKey);
            }
            catch (Exception ex)
            {
                errors.Add($"Connection ignored ({link.DisplayLabel}): {ex.Message}");
            }
        }

        return errors;
    }

    private void CollectNodeOutput(ProcessNodeDescriptor descriptor, IExecutableNode node, ResourcePool pool)
    {
        if (node.OutputType == typeof(Mat))
        {
            var mat = pool.Get<Mat>(node.Id);
            var bitmap = CreateBitmapFromMat(mat);
            if (bitmap is not null)
            {
                AddResourceFromNodeOutput(
                    descriptor.Id,
                    $"{descriptor.Name} Image",
                    "Mat",
                    new ImageResourceViewData { Bitmap = bitmap, Path = $"Output of {descriptor.Name}" },
                    ResourceValueKind.Image,
                    $"{mat.Width}x{mat.Height}");
            }

            AddMatOverlayShape(mat, descriptor.Id, "out");
            return;
        }

        if (node.OutputType == typeof(FlowLine))
        {
            var line = pool.Get<FlowLine>(node.Id);
            var lines = line.Length > 0 ? new[] { line } : [];

            AddResourceFromNodeOutput(
                descriptor.Id,
                $"{descriptor.Name} Line",
                "Line",
                new LineCoordinatesResourceViewData
                {
                    Lines = lines.Select(line => new LineCoordinateEntry
                    {
                        StartX = line.StartPoint.Position.X,
                        StartY = line.StartPoint.Position.Y,
                        EndX = line.EndPoint.Position.X,
                        EndY = line.EndPoint.Position.Y,
                        Length = line.Length
                    }).ToArray()
                },
                ResourceValueKind.LineCoordinates,
                $"{lines.Length} line(s)");

            if (line.Length > 0)
            {
                Shapes.Add(line);
                ComputedShapeIds.Add(line.Id);
                RegisterShapeProvenance(line, descriptor.Id, "out", "Line");
            }

            return;
        }

        if (node.OutputType == typeof(FlowPoint))
        {
            var point = pool.Get<FlowPoint>(node.Id);

            AddResourceFromNodeOutput(
                descriptor.Id,
                $"{descriptor.Name} Point",
                "Point",
                new ShapePropertyResourceViewData
                {
                    ShapeType = "Point",
                    Properties =
                    [
                        new KeyValuePair<string, string>("X", point.Pose.Position.X.ToString("0.###")),
                        new KeyValuePair<string, string>("Y", point.Pose.Position.Y.ToString("0.###"))
                    ]
                },
                ResourceValueKind.ShapeProperties,
                $"({point.Pose.Position.X:0.###}, {point.Pose.Position.Y:0.###})");

            Shapes.Add(point);
            ComputedShapeIds.Add(point.Id);
            RegisterShapeProvenance(point, descriptor.Id, "out", "Point");
        }
    }

    private void AddMatOverlayShape(Mat mat, string producerNodeId, string producerPortKey)
    {
        if (mat.Empty())
            return;

        var overlayFolder = Path.Combine(Path.GetTempPath(), "fx-imaging-overlays");
        Directory.CreateDirectory(overlayFolder);

        var overlayPath = Path.Combine(overlayFolder, $"{Guid.NewGuid():N}.png");
        Cv2.ImWrite(overlayPath, mat);

        var imageShape = new ImageShape
        {
            SourcePath = overlayPath,
            Width = mat.Width,
            Height = mat.Height,
            Pose = new Pose(new Vector((mat.Width - 1) * 0.5, (mat.Height - 1) * 0.5), new Vector(1, 0))
        };

        Shapes.Add(imageShape);
        ComputedShapeIds.Add(imageShape.Id);
        RegisterShapeProvenance(imageShape, producerNodeId, producerPortKey, "Mat");
    }

    private void RegisterShapeProvenance(Shape shape, string producerNodeId, string producerPortKey, string typeName)
    {
        _shapeProvenanceByShapeId[shape.Id] = new ShapeOutputProvenance(producerNodeId, producerPortKey, typeName);
    }

    private static Bitmap? CreateBitmapFromMat(Mat mat)
    {
        if (mat.Empty())
            return null;

        Cv2.ImEncode(".png", mat, out var bytes);
        using var stream = new MemoryStream(bytes);
        return new Bitmap(stream);
    }

    private void AddResourceFromNodeOutput(string producerNode, string name, string typeName, ResourceViewData value, ResourceValueKind kind, string preview)
    {
        Resources.Add(new ResourceEntryDescriptor
        {
            Key = Guid.NewGuid().ToString(),
            ProducerNode = producerNode,
            Name = name,
            TypeName = typeName,
            ValueKind = kind,
            Value = value,
            Preview = preview
        });
    }

    private void ConnectPorts()
    {
        if (SelectedFromOperation is null || SelectedToOperation is null || SelectedFromPort is null || SelectedToPort is null)
            return;

        if (SelectedFromOperation.Id == SelectedToOperation.Id)
            return;

        if (SelectedFromPort.Direction != ProcessPortDirection.Output || SelectedToPort.Direction != ProcessPortDirection.Input)
            return;

        if (!string.Equals(SelectedFromPort.TypeName, SelectedToPort.TypeName, StringComparison.Ordinal))
            return;

        var candidate = new ProcessLinkDescriptor
        {
            FromNodeId = SelectedFromOperation.Id,
            FromPortKey = SelectedFromPort.Key,
            ToNodeId = SelectedToOperation.Id,
            ToPortKey = SelectedToPort.Key
        };

        for (var i = ProcessLinks.Count - 1; i >= 0; i--)
        {
            var existing = ProcessLinks[i];
            if (existing.ToNodeId == candidate.ToNodeId && existing.ToPortKey == candidate.ToPortKey)
                ProcessLinks.RemoveAt(i);
        }

        ProcessLinks.Add(candidate);
        OnPropertyChanged(nameof(StatusText));
    }

    private void BeginBindSelectedOperation()
    {
        if (SelectedProcessNode is null)
            return;

        var ports = GetBindableInputPorts(SelectedProcessNode).ToList();
        if (ports.Count == 0)
            return;

        _pendingBindNodeId = SelectedProcessNode.Id;
        _pendingBindPorts.Clear();
        _pendingBindPorts.AddRange(ports);
        _pendingBindPortIndex = 0;
        _bindingSelectedShapeIds.Clear();
        InteractionMode = DrawingInteractionMode.Bind;
        ActiveTool = DrawingTool.Select;
        RefreshBindingCandidates();
        TryAutoBindSingleCandidates();
    }

    private void BindFromCanvasShape(string? shapeId)
    {
        if (_pendingBindNodeId is null || string.IsNullOrWhiteSpace(shapeId))
            return;

        if (_pendingBindPortIndex < 0 || _pendingBindPortIndex >= _pendingBindPorts.Count)
            return;

        if (!_shapeProvenanceByShapeId.TryGetValue(shapeId, out var source))
            return;

        var targetPort = _pendingBindPorts[_pendingBindPortIndex];
        if (!string.Equals(source.TypeName, targetPort.TypeName, StringComparison.Ordinal))
            return;

        if (source.ProducerNodeId == _pendingBindNodeId)
            return;

        if (!_bindingSelectedShapeIds.Add(shapeId))
            return;

        for (var i = ProcessLinks.Count - 1; i >= 0; i--)
        {
            var existing = ProcessLinks[i];
            if (existing.ToNodeId == _pendingBindNodeId && existing.ToPortKey == targetPort.Key)
                ProcessLinks.RemoveAt(i);
        }

        ProcessLinks.Add(new ProcessLinkDescriptor
        {
            FromNodeId = source.ProducerNodeId,
            FromPortKey = source.ProducerPortKey,
            ToNodeId = _pendingBindNodeId,
            ToPortKey = targetPort.Key
        });

        _pendingBindPortIndex++;
        if (_pendingBindPortIndex >= _pendingBindPorts.Count)
        {
            CancelBindingSession();
            return;
        }

        RefreshBindingCandidates();
        TryAutoBindSingleCandidates();
        OnPropertyChanged(nameof(StatusText));
    }

    private void RemoveSelectedConnection()
    {
        if (SelectedProcessLink is null)
            return;

        ProcessLinks.Remove(SelectedProcessLink);
        SelectedProcessLink = null;
        OnPropertyChanged(nameof(StatusText));
    }

    private IEnumerable<ProcessPortDescriptor> GetBindableInputPorts(ProcessNodeDescriptor descriptor)
    {
        return descriptor.OperationType switch
        {
            "GaussianBlurOperation" => descriptor.Inputs.Where(port => port.Key == "in"),
            "ExtractLineInRegionsOperation" => descriptor.Inputs.Where(port => port.Key == "in"),
            "ConstructLineLineIntersectionOperation" => descriptor.Inputs.Where(port => port.Key is "first" or "second"),
            _ => []
        };
    }

    private void RefreshBindingCandidates()
    {
        BindingCandidateShapeIds.Clear();

        if (_pendingBindNodeId is null)
            return;

        if (_pendingBindPortIndex < 0 || _pendingBindPortIndex >= _pendingBindPorts.Count)
            return;

        var targetPort = _pendingBindPorts[_pendingBindPortIndex];
        foreach (var (shapeId, source) in _shapeProvenanceByShapeId)
        {
            if (!string.Equals(source.TypeName, targetPort.TypeName, StringComparison.Ordinal))
                continue;

            if (source.ProducerNodeId == _pendingBindNodeId)
                continue;

            if (_bindingSelectedShapeIds.Contains(shapeId))
                continue;

            BindingCandidateShapeIds.Add(shapeId);
        }
    }

    private void TryAutoBindSingleCandidates()
    {
        while (_pendingBindNodeId is not null &&
               _pendingBindPortIndex >= 0 &&
               _pendingBindPortIndex < _pendingBindPorts.Count &&
               BindingCandidateShapeIds.Count == 1)
        {
            BindFromCanvasShape(BindingCandidateShapeIds[0]);
        }
    }

    private void TryAutoSetupSelectedExtractOperation(ProcessNodeDescriptor descriptor)
    {
        if (descriptor.OperationType != "ExtractLineInRegionsOperation")
            return;

        if (!HasIncomingLink(descriptor.Id, "in"))
            TryAutoBindSingleInputPort(descriptor, "in", "Mat");

        if (!_extractRoiShapeIdByNodeId.ContainsKey(descriptor.Id))
            BeginRoiDrawingForSelectedOperation();
    }

    private bool HasIncomingLink(string toNodeId, string toPortKey)
    {
        return ProcessLinks.Any(link =>
            string.Equals(link.ToNodeId, toNodeId, StringComparison.Ordinal) &&
            string.Equals(link.ToPortKey, toPortKey, StringComparison.Ordinal));
    }

    private void TryAutoBindSingleInputPort(ProcessNodeDescriptor target, string toPortKey, string requiredType)
    {
        var candidateSources = _shapeProvenanceByShapeId.Values
            .Where(source => string.Equals(source.TypeName, requiredType, StringComparison.Ordinal))
            .Distinct()
            .ToArray();

        if (candidateSources.Length != 1)
            return;

        var source = candidateSources[0];
        if (string.Equals(source.ProducerNodeId, target.Id, StringComparison.Ordinal))
            return;

        for (var i = ProcessLinks.Count - 1; i >= 0; i--)
        {
            var existing = ProcessLinks[i];
            if (string.Equals(existing.ToNodeId, target.Id, StringComparison.Ordinal) &&
                string.Equals(existing.ToPortKey, toPortKey, StringComparison.Ordinal))
            {
                ProcessLinks.RemoveAt(i);
            }
        }

        ProcessLinks.Add(new ProcessLinkDescriptor
        {
            FromNodeId = source.ProducerNodeId,
            FromPortKey = source.ProducerPortKey,
            ToNodeId = target.Id,
            ToPortKey = toPortKey
        });
    }

    private void CancelBindingSession()
    {
        _pendingBindNodeId = null;
        _pendingBindPorts.Clear();
        _pendingBindPortIndex = 0;
        _bindingSelectedShapeIds.Clear();
        BindingCandidateShapeIds.Clear();
        InteractionMode = DrawingInteractionMode.Standard;
        OnPropertyChanged(nameof(StatusText));
    }

    private async Task ViewSelectedResourceAsync()
    {
        if (SelectedResource is null)
            return;

        await _dialogService.ShowAsync(dialog =>
        {
            dialog.Title = $"Resource Viewer - {SelectedResource.Name}";
            dialog.Content = new ResourceViewerControl { Resource = SelectedResource };
            dialog.CloseButtonText = "Close";
        });
    }

    private void ClearResources()
    {
        Resources.Clear();
        SelectedResource = null;
        OnPropertyChanged(nameof(StatusText));
    }

    private async Task SaveSceneAsync()
    {
        var path = await PromptScenePathAsync("Save scene", "Save", "scene.json");
        if (string.IsNullOrWhiteSpace(path))
            return;

        try
        {
            var computed = ComputedShapeIds.ToHashSet(StringComparer.Ordinal);
            var scene = SceneDocumentMapper.ToDocument(Shapes, computed);
            var json = _sceneSerializer.Serialize(scene);

            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            await File.WriteAllTextAsync(path, json);

        }
        catch (Exception ex)
        {
            await _infoBarService.ShowAsync(infoBar =>
            {
                infoBar.Severity = InfoBarSeverity.Error;
                infoBar.Title = "Scene save failed";
                infoBar.Message = ex.Message;
            });
        }
    }

    private async Task LoadSceneAsync()
    {
        var path = await PromptScenePathAsync("Load scene", "Load", "scene.json");
        if (string.IsNullOrWhiteSpace(path))
            return;

        try
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Scene file not found.", path);

            var json = await File.ReadAllTextAsync(path);
            var scene = _sceneSerializer.Deserialize(json);
            var loaded = SceneDocumentMapper.FromDocument(scene);

            CancelBindingSession();
            Shapes.Clear();
            ComputedShapeIds.Clear();
            _shapeProvenanceByShapeId.Clear();

            foreach (var shape in loaded.Shapes)
                Shapes.Add(shape);

            foreach (var id in loaded.ComputedShapeIds)
                ComputedShapeIds.Add(id);

        }
        catch (Exception ex)
        {
            await _infoBarService.ShowAsync(infoBar =>
            {
                infoBar.Severity = InfoBarSeverity.Error;
                infoBar.Title = "Scene load failed";
                infoBar.Message = ex.Message;
            });
        }
    }

    private async Task<string?> PromptScenePathAsync(string title, string primaryButtonText, string defaultFileName)
    {
        var pathBox = new TextBox
        {
            Width = 560,
            Watermark = "Absolute path to .json scene file"
        };

        var defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), defaultFileName);
        pathBox.Text = defaultPath;

        var result = await _dialogService.ShowAsync(dialog =>
        {
            dialog.Title = title;
            dialog.Content = new StackPanel
            {
                Spacing = 10,
                Children =
                {
                    new TextBlock { Text = "Scene file path:" },
                    pathBox
                }
            };
            dialog.PrimaryButtonText = primaryButtonText;
            dialog.CloseButtonText = "Cancel";
            dialog.DefaultButton = DefaultButton.Primary;
        });

        if (result != DialogResult.Primary)
            return null;

        var path = pathBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(path))
            return null;

        return path;
    }

    private async Task<bool> EnsureOpenCvRuntimeAvailableAsync()
    {
        try
        {
            _ = Cv2.GetVersionString();
            return true;
        }
        catch (TypeInitializationException ex)
        {
            var details = ex.InnerException?.Message ?? ex.Message;
            await _infoBarService.ShowAsync(infoBar =>
            {
                infoBar.Severity = InfoBarSeverity.Error;
                infoBar.Title = "OpenCV runtime not available";
                infoBar.Message =
                    $"OpenCvSharp native runtime failed to load. {details} " +
                    "On Linux you usually need libOpenCvSharpExtern.so installed/available in runtime search path.";
            });
            return false;
        }
        catch (DllNotFoundException ex)
        {
            await _infoBarService.ShowAsync(infoBar =>
            {
                infoBar.Severity = InfoBarSeverity.Error;
                infoBar.Title = "Missing native OpenCV library";
                infoBar.Message =
                    $"Could not load native dependency: {ex.Message}. " +
                    "Install OpenCvSharp native runtime for your platform or provide libOpenCvSharpExtern.so.";
            });
            return false;
        }
    }

    private void BeginRoiDrawingForSelectedOperation()
    {
        if (SelectedProcessNode?.OperationType != "ExtractLineInRegionsOperation")
            return;

        CancelBindingSession();

        if (_extractRoiShapeIdByNodeId.TryGetValue(SelectedProcessNode.Id, out var existingShapeId))
        {
            var existingShape = Shapes.FirstOrDefault(shape => shape.Id == existingShapeId);
            if (existingShape is not null)
                Shapes.Remove(existingShape);
            _extractRoiShapeIdByNodeId.Remove(SelectedProcessNode.Id);
        }

        _pendingRoiNodeId = SelectedProcessNode.Id;
        ActiveTool = DrawingTool.CenterlineRectangle;
        InteractionMode = DrawingInteractionMode.Draw;
    }

    private void RemoveRoiForSelectedOperation()
    {
        if (SelectedProcessNode?.OperationType != "ExtractLineInRegionsOperation")
            return;

        if (!_extractRoiShapeIdByNodeId.TryGetValue(SelectedProcessNode.Id, out var shapeId))
            return;

        var shape = Shapes.FirstOrDefault(item => item.Id == shapeId);
        if (shape is not null)
            Shapes.Remove(shape);

        _extractRoiShapeIdByNodeId.Remove(SelectedProcessNode.Id);
        RefreshSelectedNodeProperties();
    }

    private async Task BrowseLoadPathForSelectedOperationAsync()
    {
        if (SelectedProcessNode?.OperationType != "LoadOperation")
            return;

        var imageFiles = new FilePickerFileType("Image Files")
        {
            Patterns = ["*.png", "*.jpg", "*.jpeg", "*.bmp", "*.tif", "*.tiff"],
            MimeTypes = ["image/*"]
        };

        var selectedPath = (await _fileDialogService.ShowOpenFileDialogAsync(
            title: "Select image",
            allowMultiple: false,
            fileTypeFilter: [imageFiles, FilePickerFileTypes.All])).FirstOrDefault();

        if (string.IsNullOrWhiteSpace(selectedPath))
            return;

        SelectedLoadPath = selectedPath;
    }

    private async Task OpenResourceInspectorAsync()
    {
        var inspector = new ResourcePoolControl
        {
            DataContext = this
        };
        inspector.Bind(ResourcePoolControl.ResourceItemsProperty, new Binding(nameof(Resources)));
        inspector.Bind(ResourcePoolControl.SelectedResourceProperty, new Binding(nameof(SelectedResource)) { Mode = BindingMode.TwoWay });
        inspector.Bind(ResourcePoolControl.ViewResourceCommandProperty, new Binding(nameof(ViewResourceCommand)));
        inspector.Bind(ResourcePoolControl.ClearResourcesCommandProperty, new Binding(nameof(ClearResourcesCommand)));

        await _dialogService.ShowAsync(dialog =>
        {
            dialog.Title = "Resource Pool";
            dialog.Content = inspector;
            dialog.CloseButtonText = "Close";
        });
    }

    private void ClearComputedResults()
    {
        CancelBindingSession();

        for (var i = Shapes.Count - 1; i >= 0; i--)
        {
            if (ComputedShapeIds.Contains(Shapes[i].Id))
                Shapes.RemoveAt(i);
        }

        ComputedShapeIds.Clear();
        _shapeProvenanceByShapeId.Clear();
    }

    private void ClearCanvas()
    {
        CancelBindingSession();
        Shapes.Clear();
        ComputedShapeIds.Clear();
        Resources.Clear();
        _shapeProvenanceByShapeId.Clear();

        ClearPipeline();

        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(PipelineSummary));
    }

    private void ResetView()
    {
        Zoom = 1d;
        Pan = default;
    }

    private void OnShapesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        TryBindPendingRoi(e);
        RemoveDeletedRoiBindings(e);
        RemoveDeletedShapeProvenance();
        if (InteractionMode == DrawingInteractionMode.Bind)
            RefreshBindingCandidates();
        OnPropertyChanged(nameof(StatusText));
    }

    private void TryBindPendingRoi(NotifyCollectionChangedEventArgs e)
    {
        if (_pendingRoiNodeId is null)
            return;

        if (e.Action is not NotifyCollectionChangedAction.Add || e.NewItems is null)
            return;

        var centerlineRectangle = e.NewItems.OfType<CenterlineRectangleShape>().FirstOrDefault();
        if (centerlineRectangle is null)
            return;

        _extractRoiShapeIdByNodeId[_pendingRoiNodeId] = centerlineRectangle.Id;
        _pendingRoiNodeId = null;
        ActiveTool = DrawingTool.Select;
        InteractionMode = DrawingInteractionMode.Standard;

        RefreshSelectedNodeProperties();
    }

    private static bool TryMapRoiShapeToRectangle(Shape? shape, out FlowRectangle rectangle)
    {
        switch (shape)
        {
            case null:
                rectangle = null!;
                return false;
            case FlowRectangle flowRectangle:
                rectangle = flowRectangle;
                return true;
            case CenterlineRectangleShape centerlineRectangle:
            {
                var orientation = centerlineRectangle.Pose.Orientation.M <= 1e-9
                    ? new Vector(1, 0)
                    : centerlineRectangle.Pose.Orientation.Normalize();
                var center = centerlineRectangle.Pose.Position + orientation.Scale(centerlineRectangle.Length * 0.5);
                rectangle = new FlowRectangle
                {
                    Pose = new Pose(center, orientation),
                    Width = centerlineRectangle.Length,
                    Height = centerlineRectangle.Width
                };
                return rectangle.Width > 0 && rectangle.Height > 0;
            }
            default:
                rectangle = null!;
                return false;
        }
    }

    private void RemoveDeletedRoiBindings(NotifyCollectionChangedEventArgs e)
    {
        if (e.Action is not NotifyCollectionChangedAction.Remove && e.Action is not NotifyCollectionChangedAction.Reset)
            return;

        var existingShapeIds = Shapes.Select(shape => shape.Id).ToHashSet(StringComparer.Ordinal);
        var orphanBindings = _extractRoiShapeIdByNodeId
            .Where(pair => !existingShapeIds.Contains(pair.Value))
            .Select(pair => pair.Key)
            .ToArray();

        foreach (var nodeId in orphanBindings)
            _extractRoiShapeIdByNodeId.Remove(nodeId);

        if (orphanBindings.Length > 0)
            RefreshSelectedNodeProperties();
    }

    private void RemoveDeletedShapeProvenance()
    {
        var existingShapeIds = Shapes.Select(shape => shape.Id).ToHashSet(StringComparer.Ordinal);
        var orphanShapeIds = _shapeProvenanceByShapeId.Keys
            .Where(shapeId => !existingShapeIds.Contains(shapeId))
            .ToArray();

        foreach (var orphanShapeId in orphanShapeIds)
            _shapeProvenanceByShapeId.Remove(orphanShapeId);
    }

    private void OnComputedShapeIdsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => OnPropertyChanged(nameof(StatusText));

    private void OnResourcesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => OnPropertyChanged(nameof(StatusText));

    private void OnBindingCandidateShapeIdsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => OnPropertyChanged(nameof(StatusText));

    private void OnProcessNodesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshNodeSelections();
        RefreshOutputPorts();
        RefreshInputPorts();
        RefreshSelectedNodeProperties();
        if (InteractionMode == DrawingInteractionMode.Bind)
            RefreshBindingCandidates();

        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(PipelineSummary));
    }

    private void RefreshNodeSelections()
    {
        if (SelectedFromOperation is not null && !ProcessNodes.Contains(SelectedFromOperation))
            SelectedFromOperation = null;

        if (SelectedToOperation is not null && !ProcessNodes.Contains(SelectedToOperation))
            SelectedToOperation = null;

        if (SelectedProcessNode is not null && !ProcessNodes.Contains(SelectedProcessNode))
            SelectedProcessNode = null;

        SelectedFromOperation ??= ProcessNodes.FirstOrDefault();
        SelectedToOperation ??= ProcessNodes.FirstOrDefault();
        SelectedProcessNode ??= ProcessNodes.FirstOrDefault();
    }

    private void RefreshSelectedNodeProperties()
    {
        var node = SelectedProcessNode;
        CanRemoveSelectedOperation = node is not null;

        CanEditLoadPath = node?.OperationType == "LoadOperation";
        CanEditGaussian = node?.OperationType == "GaussianBlurOperation";
        CanConfigureExtractLine = node?.OperationType == "ExtractLineInRegionsOperation";

        if (CanEditLoadPath && node is not null)
            SelectedLoadPath = _loadPathByNodeId.GetValueOrDefault(node.Id, string.Empty);
        else
            SelectedLoadPath = string.Empty;

        if (CanEditGaussian && node is not null)
        {
            SelectedGaussianKernelSize = _gaussianKernelSizeByNodeId.GetValueOrDefault(node.Id, 5).ToString();
            SelectedGaussianSigma = _gaussianSigmaByNodeId.GetValueOrDefault(node.Id, 1.0).ToString("0.###");
        }
        else
        {
            SelectedGaussianKernelSize = "5";
            SelectedGaussianSigma = "1.0";
        }

        if (CanConfigureExtractLine && node is not null)
        {
            if (_extractRoiShapeIdByNodeId.TryGetValue(node.Id, out var shapeId))
                SelectedExtractRoiStatus = $"ROI: {shapeId}";
            else
                SelectedExtractRoiStatus = "ROI: not configured";
        }
        else
        {
            SelectedExtractRoiStatus = "ROI: not configured";
        }
    }

    private void RefreshOutputPorts()
    {
        AvailableOutputPorts.Clear();
        if (SelectedFromOperation is null)
        {
            SelectedFromPort = null;
            return;
        }

        foreach (var port in SelectedFromOperation.Outputs)
            AvailableOutputPorts.Add(port);

        SelectedFromPort = AvailableOutputPorts.FirstOrDefault();
    }

    private void RefreshInputPorts()
    {
        AvailableInputPorts.Clear();
        if (SelectedToOperation is null)
        {
            SelectedToPort = null;
            return;
        }

        foreach (var port in SelectedToOperation.Inputs)
            AvailableInputPorts.Add(port);

        SelectedToPort = AvailableInputPorts.FirstOrDefault();
    }

    private readonly record struct ShapeOutputProvenance(
        string ProducerNodeId,
        string ProducerPortKey,
        string TypeName);
}
