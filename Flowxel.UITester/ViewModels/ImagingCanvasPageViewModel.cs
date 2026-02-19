using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Flowxel.Core.Geometry.Primitives;
using Flowxel.Core.Geometry.Shapes;
using Flowxel.Graph;
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
using FlowRectangle = Flowxel.Core.Geometry.Shapes.Rectangle;

namespace Flowxel.UITester.ViewModels;

public partial class ImagingCanvasPageViewModel : ViewModelBase
{
    private readonly IContentDialogService _dialogService;
    private readonly IInfoBarService _infoBarService;
    private readonly ISceneSerializer _sceneSerializer = new JsonSceneSerializer();

    private readonly Dictionary<string, string> _loadPathByNodeId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _gaussianKernelSizeByNodeId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, double> _gaussianSigmaByNodeId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _extractRoiShapeIdByNodeId = new(StringComparer.Ordinal);

    private int _nodeCounter;
    private string? _pendingRoiNodeId;

    public ImagingCanvasPageViewModel(IContentDialogService dialogService, IInfoBarService infoBarService)
    {
        _dialogService = dialogService;
        _infoBarService = infoBarService;

        SelectToolCommand = new RelayCommand(() => ActiveTool = DrawingTool.Select);
        SelectRectangleToolCommand = new RelayCommand(() => ActiveTool = DrawingTool.Rectangle);

        AddLoadImageOperationCommand = new RelayCommand(AddLoadImageOperation);
        AddGaussianBlurOperationCommand = new RelayCommand(AddGaussianBlurOperation);
        AddExtractLineInRegionOperationCommand = new RelayCommand(AddExtractLineInRegionOperation);
        RemoveSelectedOperationCommand = new RelayCommand(RemoveSelectedOperation);
        ClearPipelineCommand = new RelayCommand(ClearPipeline);

        RunPipelineCommand = new AsyncRelayCommand(RunPipelineAsync);
        ClearComputedResultsCommand = new RelayCommand(ClearComputedResults);

        ConnectPortsCommand = new RelayCommand(ConnectPorts);
        RemoveConnectionCommand = new RelayCommand(RemoveSelectedConnection);
        ViewResourceCommand = new AsyncRelayCommand(ViewSelectedResourceAsync);
        ClearResourcesCommand = new RelayCommand(ClearResources);

        DrawRoiForSelectedOperationCommand = new RelayCommand(BeginRoiDrawingForSelectedOperation);
        RemoveRoiForSelectedOperationCommand = new RelayCommand(RemoveRoiForSelectedOperation);

        SaveSceneCommand = new AsyncRelayCommand(SaveSceneAsync);
        LoadSceneCommand = new AsyncRelayCommand(LoadSceneAsync);

        ClearCanvasCommand = new RelayCommand(ClearCanvas);
        ResetViewCommand = new RelayCommand(ResetView);

        Shapes.CollectionChanged += OnShapesCollectionChanged;
        ComputedShapeIds.CollectionChanged += OnComputedShapeIdsCollectionChanged;
        Resources.CollectionChanged += OnResourcesCollectionChanged;
        ProcessNodes.CollectionChanged += OnProcessNodesCollectionChanged;

        DiscoverAvailableOperations();
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

    public IRelayCommand SelectToolCommand { get; }
    public IRelayCommand SelectRectangleToolCommand { get; }

    public IRelayCommand AddLoadImageOperationCommand { get; }
    public IRelayCommand AddGaussianBlurOperationCommand { get; }
    public IRelayCommand AddExtractLineInRegionOperationCommand { get; }
    public IRelayCommand RemoveSelectedOperationCommand { get; }
    public IRelayCommand ClearPipelineCommand { get; }

    public IAsyncRelayCommand RunPipelineCommand { get; }
    public IRelayCommand ClearComputedResultsCommand { get; }

    public IRelayCommand ConnectPortsCommand { get; }
    public IRelayCommand RemoveConnectionCommand { get; }
    public IAsyncRelayCommand ViewResourceCommand { get; }
    public IRelayCommand ClearResourcesCommand { get; }

    public IRelayCommand DrawRoiForSelectedOperationCommand { get; }
    public IRelayCommand RemoveRoiForSelectedOperationCommand { get; }

    public IAsyncRelayCommand SaveSceneCommand { get; }
    public IAsyncRelayCommand LoadSceneCommand { get; }

    public IRelayCommand ClearCanvasCommand { get; }
    public IRelayCommand ResetViewCommand { get; }

    public string StatusText =>
        $"Tool: {ActiveTool} | Shapes: {Shapes.Count} (Computed: {ComputedShapeIds.Count}) | Ops: {ProcessNodes.Count} | Links: {ProcessLinks.Count} | Resources: {Resources.Count}";

    public string PipelineSummary => ProcessNodes.Count == 0
        ? "No operation in pipeline"
        : string.Join(" -> ", ProcessNodes.Select(p => p.Name));

    partial void OnActiveToolChanged(DrawingTool value) => OnPropertyChanged(nameof(StatusText));
    partial void OnZoomChanged(double value) => OnPropertyChanged(nameof(StatusText));
    partial void OnPanChanged(global::Avalonia.Vector value) => OnPropertyChanged(nameof(StatusText));
    partial void OnCursorCanvasPositionChanged(global::Avalonia.Point value) => OnPropertyChanged(nameof(StatusText));

    partial void OnSelectedProcessNodeChanged(ProcessNodeDescriptor? value)
    {
        RefreshSelectedNodeProperties();
        if (value is not null)
            SelectedToOperation ??= value;
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
                new ProcessPortDescriptor { Key = "out", Name = "Lines", TypeName = "Line[]", Direction = ProcessPortDirection.Output }
            ]
        };
    }

    private string NextNodeId(string prefix)
    {
        _nodeCounter++;
        return $"{prefix}-{_nodeCounter}";
    }

    private void RemoveSelectedOperation()
    {
        if (SelectedProcessNode is null)
            return;

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
        {
            await _infoBarService.ShowAsync(infoBar =>
            {
                infoBar.Severity = InfoBarSeverity.Warning;
                infoBar.Title = "Empty pipeline";
                infoBar.Message = "Add at least one operation in the ribbon.";
            });
            return;
        }

        ClearComputedResults();
        ClearResources();

        try
        {
            var pool = new ResourcePool();
            var graph = new Graph<IExecutableNode>();
            var nodeInstances = CreateExecutableNodes(pool, graph);

            ApplyOperationParameters(nodeInstances);
            ValidateAndConnectGraph(graph, nodeInstances);

            await graph.ExecuteAsync();

            CollectNodeResourcesAndOverlays(nodeInstances, pool);

            await _infoBarService.ShowAsync(infoBar =>
            {
                infoBar.Severity = InfoBarSeverity.Success;
                infoBar.Title = "Pipeline executed";
                infoBar.Message =
                    $"Executed {ProcessNodes.Count} operation(s). Computed overlays: {ComputedShapeIds.Count}. Resources: {Resources.Count}.";
            });
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
                _ => throw new InvalidOperationException($"Unsupported operation type: {descriptor.OperationType}")
            };

            graph.AddNode(node);
            map[descriptor.Id] = node;
        }

        return map;
    }

    private void ApplyOperationParameters(IReadOnlyDictionary<string, IExecutableNode> nodes)
    {
        foreach (var descriptor in ProcessNodes)
        {
            var node = nodes[descriptor.Id];

            switch (descriptor.OperationType)
            {
                case "LoadOperation":
                {
                    var path = _loadPathByNodeId.GetValueOrDefault(descriptor.Id)?.Trim() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(path))
                        throw new InvalidOperationException($"Operation '{descriptor.Name}' requires a path in Load.Path.");
                    if (!File.Exists(path))
                        throw new InvalidOperationException($"Operation '{descriptor.Name}' path does not exist: {path}");

                    node.Parameters["Path"] = path;
                    break;
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
                    break;
                }
                case "ExtractLineInRegionsOperation":
                {
                    if (!_extractRoiShapeIdByNodeId.TryGetValue(descriptor.Id, out var roiShapeId))
                    {
                        throw new InvalidOperationException(
                            $"Operation '{descriptor.Name}' requires an ROI. Select the operation and click 'Draw ROI'.");
                    }

                    var rectangle = Shapes.FirstOrDefault(shape => shape.Id == roiShapeId) as FlowRectangle;
                    if (rectangle is null)
                    {
                        throw new InvalidOperationException(
                            $"Operation '{descriptor.Name}' has an invalid ROI binding. Draw ROI again.");
                    }

                    node.Parameters["Region"] = rectangle;
                    break;
                }
            }
        }
    }

    private void ValidateAndConnectGraph(Graph<IExecutableNode> graph, IReadOnlyDictionary<string, IExecutableNode> nodes)
    {
        var incomingCounts = nodes.Keys.ToDictionary(key => key, _ => 0);

        foreach (var link in ProcessLinks)
        {
            if (!nodes.TryGetValue(link.FromNodeId, out var from) || !nodes.TryGetValue(link.ToNodeId, out var to))
                throw new InvalidOperationException($"Invalid connection: {link.DisplayLabel}");

            if (from.OutputType != to.InputType)
            {
                throw new InvalidOperationException(
                    $"Type mismatch on connection {link.DisplayLabel}: {from.OutputType.Name} -> {to.InputType.Name}");
            }

            graph.Connect(from, to);
            incomingCounts[link.ToNodeId]++;
        }

        foreach (var descriptor in ProcessNodes)
        {
            if (descriptor.OperationType == "LoadOperation")
                continue;

            if (incomingCounts.TryGetValue(descriptor.Id, out var count) && count == 1)
                continue;

            throw new InvalidOperationException(
                $"Operation '{descriptor.Name}' must have exactly one incoming image connection.");
        }
    }

    private void CollectNodeResourcesAndOverlays(IReadOnlyDictionary<string, IExecutableNode> nodes, ResourcePool pool)
    {
        foreach (var descriptor in ProcessNodes)
        {
            if (!nodes.TryGetValue(descriptor.Id, out var node))
                continue;

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

                AddMatOverlayShape(mat);
                continue;
            }

            if (node.OutputType == typeof(FlowLine[]))
            {
                var lines = pool.Get<FlowLine[]>(node.Id);

                AddResourceFromNodeOutput(
                    descriptor.Id,
                    $"{descriptor.Name} Lines",
                    "Line[]",
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

                foreach (var line in lines)
                {
                    Shapes.Add(line);
                    ComputedShapeIds.Add(line.Id);
                }
            }
        }
    }

    private void AddMatOverlayShape(Mat mat)
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

    private void RemoveSelectedConnection()
    {
        if (SelectedProcessLink is null)
            return;

        ProcessLinks.Remove(SelectedProcessLink);
        SelectedProcessLink = null;
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

            await _infoBarService.ShowAsync(infoBar =>
            {
                infoBar.Severity = InfoBarSeverity.Success;
                infoBar.Title = "Scene saved";
                infoBar.Message = path;
            });
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

            Shapes.Clear();
            ComputedShapeIds.Clear();

            foreach (var shape in loaded.Shapes)
                Shapes.Add(shape);

            foreach (var id in loaded.ComputedShapeIds)
                ComputedShapeIds.Add(id);

            await _infoBarService.ShowAsync(infoBar =>
            {
                infoBar.Severity = InfoBarSeverity.Success;
                infoBar.Title = "Scene loaded";
                infoBar.Message = $"{path} ({loaded.Shapes.Count} shape(s))";
            });
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

        _pendingRoiNodeId = SelectedProcessNode.Id;
        ActiveTool = DrawingTool.Rectangle;

        _ = _infoBarService.ShowAsync(infoBar =>
        {
            infoBar.Severity = InfoBarSeverity.Info;
            infoBar.Title = "Draw ROI";
            infoBar.Message = "Draw a rectangle on the canvas to bind it to the selected extractLineInRegion operation.";
        });
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

    private void ClearComputedResults()
    {
        for (var i = Shapes.Count - 1; i >= 0; i--)
        {
            if (ComputedShapeIds.Contains(Shapes[i].Id))
                Shapes.RemoveAt(i);
        }

        ComputedShapeIds.Clear();
    }

    private void ClearCanvas()
    {
        Shapes.Clear();
        ComputedShapeIds.Clear();
        Resources.Clear();

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
        OnPropertyChanged(nameof(StatusText));
    }

    private void TryBindPendingRoi(NotifyCollectionChangedEventArgs e)
    {
        if (_pendingRoiNodeId is null)
            return;

        if (e.Action is not NotifyCollectionChangedAction.Add || e.NewItems is null)
            return;

        var rectangle = e.NewItems.OfType<FlowRectangle>().FirstOrDefault();
        if (rectangle is null)
            return;

        if (_extractRoiShapeIdByNodeId.TryGetValue(_pendingRoiNodeId, out var existingShapeId))
        {
            var existingShape = Shapes.FirstOrDefault(shape => shape.Id == existingShapeId);
            if (existingShape is not null)
                Shapes.Remove(existingShape);
        }

        _extractRoiShapeIdByNodeId[_pendingRoiNodeId] = rectangle.Id;
        _pendingRoiNodeId = null;
        ActiveTool = DrawingTool.Select;

        RefreshSelectedNodeProperties();

        _ = _infoBarService.ShowAsync(infoBar =>
        {
            infoBar.Severity = InfoBarSeverity.Success;
            infoBar.Title = "ROI bound";
            infoBar.Message = $"ROI {rectangle.Id} is now bound to the selected extract operation.";
        });
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

    private void OnComputedShapeIdsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => OnPropertyChanged(nameof(StatusText));

    private void OnResourcesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => OnPropertyChanged(nameof(StatusText));

    private void OnProcessNodesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshNodeSelections();
        RefreshOutputPorts();
        RefreshInputPorts();
        RefreshSelectedNodeProperties();

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
}
