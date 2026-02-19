using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Flowxel.Core.Geometry.Shapes;
using Flowxel.UI.Controls;
using Flowxel.UI.Services;
using Flowxel.UI.Services.Shortcuts;
using Flowxel.UI.Drawing;
using Flowxel.UI.Drawing.Scene;

namespace Flowxel.UITester.ViewModels;

public partial class RibbonCanvasTestingPageViewModel : ViewModelBase, IShortcutBindingProvider
{
    private readonly ISceneSerializer _sceneSerializer = new JsonSceneSerializer();
    private readonly ISvgSceneExporter _svgExporter = new SvgSceneExporter();

    public RibbonCanvasTestingPageViewModel(IContentDialogService dialogService, IInfoBarService infoBarService)
    {
        DialogService = dialogService;
        InfoBarService = infoBarService;

        SelectToolCommand = new RelayCommand(() => ActiveTool = DrawingTool.Select);
        SelectPointToolCommand = new RelayCommand(() => ActiveTool = DrawingTool.Point);
        SelectLineToolCommand = new RelayCommand(() => ActiveTool = DrawingTool.Line);
        SelectRectangleToolCommand = new RelayCommand(() => ActiveTool = DrawingTool.Rectangle);
        SelectCircleToolCommand = new RelayCommand(() => ActiveTool = DrawingTool.Circle);
        SelectImageToolCommand = new RelayCommand(() => ActiveTool = DrawingTool.Image);
        SelectTextBoxToolCommand = new RelayCommand(() => ActiveTool = DrawingTool.TextBox);
        SelectTextToolCommand = new RelayCommand(() => ActiveTool = DrawingTool.Text);
        SelectMultilineTextToolCommand = new RelayCommand(() => ActiveTool = DrawingTool.MultilineText);
        SelectIconToolCommand = new RelayCommand(() => ActiveTool = DrawingTool.Icon);
        SelectArcToolCommand = new RelayCommand(() => ActiveTool = DrawingTool.Arc);
        SelectArrowToolCommand = new RelayCommand(() => ActiveTool = DrawingTool.Arrow);
        SelectCenterlineRectangleToolCommand = new RelayCommand(() => ActiveTool = DrawingTool.CenterlineRectangle);
        SelectReferentialToolCommand = new RelayCommand(() => ActiveTool = DrawingTool.Referential);
        SelectDimensionToolCommand = new RelayCommand(() => ActiveTool = DrawingTool.Dimension);
        SelectAngleDimensionToolCommand = new RelayCommand(() => ActiveTool = DrawingTool.AngleDimension);

        SaveSceneCommand = new AsyncRelayCommand(SaveSceneAsync);
        LoadSceneCommand = new AsyncRelayCommand(LoadSceneAsync);
        ExportSvgCommand = new AsyncRelayCommand(ExportSvgAsync);
        ClearCanvasCommand = new RelayCommand(ClearCanvas);
        ResetViewCommand = new RelayCommand(ResetView);

        Shapes.CollectionChanged += OnShapesCollectionChanged;
        ComputedShapeIds.CollectionChanged += OnComputedShapeIdsCollectionChanged;

        ApplyCanvasColor();
    }

    public IContentDialogService DialogService { get; }

    public IInfoBarService InfoBarService { get; }

    public ObservableCollection<Shape> Shapes { get; } = [];

    public ObservableCollection<string> ComputedShapeIds { get; } = [];

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
    private string canvasBackgroundHex = "#101317";

    [ObservableProperty]
    private IBrush canvasBackgroundBrush = new SolidColorBrush(Color.Parse("#101317"));

    [ObservableProperty]
    private bool showCanvasBoundary = true;

    [ObservableProperty]
    private double canvasBoundaryWidth = 1920;

    [ObservableProperty]
    private double canvasBoundaryHeight = 1080;

    public IRelayCommand SelectToolCommand { get; }
    public IRelayCommand SelectPointToolCommand { get; }
    public IRelayCommand SelectLineToolCommand { get; }
    public IRelayCommand SelectRectangleToolCommand { get; }
    public IRelayCommand SelectCircleToolCommand { get; }
    public IRelayCommand SelectImageToolCommand { get; }
    public IRelayCommand SelectTextBoxToolCommand { get; }
    public IRelayCommand SelectTextToolCommand { get; }
    public IRelayCommand SelectMultilineTextToolCommand { get; }
    public IRelayCommand SelectIconToolCommand { get; }
    public IRelayCommand SelectArcToolCommand { get; }
    public IRelayCommand SelectArrowToolCommand { get; }
    public IRelayCommand SelectCenterlineRectangleToolCommand { get; }
    public IRelayCommand SelectReferentialToolCommand { get; }
    public IRelayCommand SelectDimensionToolCommand { get; }
    public IRelayCommand SelectAngleDimensionToolCommand { get; }
    public IAsyncRelayCommand SaveSceneCommand { get; }
    public IAsyncRelayCommand LoadSceneCommand { get; }
    public IAsyncRelayCommand ExportSvgCommand { get; }
    public IRelayCommand ClearCanvasCommand { get; }
    public IRelayCommand ResetViewCommand { get; }

    public IEnumerable<ShortcutDefinition> GetShortcutDefinitions()
    {
        return
        [
            new ShortcutDefinition("Ctrl+S", SaveSceneCommand, Description: "Save scene"),
            new ShortcutDefinition("Ctrl+O", LoadSceneCommand, Description: "Load scene"),
            new ShortcutDefinition("Ctrl+Shift+E", ExportSvgCommand, Description: "Export SVG"),
            new ShortcutDefinition("Ctrl+R", ResetViewCommand, Description: "Reset view"),
            new ShortcutDefinition("Ctrl+Shift+Delete", ClearCanvasCommand, Description: "Clear canvas"),

            new ShortcutDefinition("Escape", SelectToolCommand, Description: "Select tool"),
            new ShortcutDefinition("P", SelectPointToolCommand, Description: "Point tool"),
            new ShortcutDefinition("L", SelectLineToolCommand, Description: "Line tool"),
            new ShortcutDefinition("R", SelectRectangleToolCommand, Description: "Rectangle tool"),
            new ShortcutDefinition("C", SelectCircleToolCommand, Description: "Circle tool"),
            new ShortcutDefinition("A", SelectArcToolCommand, Description: "Arc tool"),
            new ShortcutDefinition("T", SelectTextToolCommand, Description: "Text tool"),
            new ShortcutDefinition("M", SelectMultilineTextToolCommand, Description: "Multiline text tool"),
            new ShortcutDefinition("I", SelectImageToolCommand, Description: "Image tool")
        ];
    }

    public string StatusText =>
        $"Tool: {ActiveTool} | Shapes: {Shapes.Count} (Computed: {ComputedShapeIds.Count}) | Boundary: {(ShowCanvasBoundary ? $"{CanvasBoundaryWidth:0.#}x{CanvasBoundaryHeight:0.#}" : "off")} | Cursor(Canvas): ({CursorCanvasPosition.X:0.000}, {CursorCanvasPosition.Y:0.000})";

    partial void OnActiveToolChanged(DrawingTool value) => OnPropertyChanged(nameof(StatusText));
    partial void OnCursorCanvasPositionChanged(global::Avalonia.Point value) => OnPropertyChanged(nameof(StatusText));
    partial void OnShowCanvasBoundaryChanged(bool value) => OnPropertyChanged(nameof(StatusText));
    partial void OnCanvasBoundaryWidthChanged(double value) => OnPropertyChanged(nameof(StatusText));
    partial void OnCanvasBoundaryHeightChanged(double value) => OnPropertyChanged(nameof(StatusText));
    partial void OnCanvasBackgroundBrushChanged(IBrush value) => CanvasBackgroundHex = ToHexColor(value);
    partial void OnCanvasBackgroundHexChanged(string value) => ApplyCanvasColor();

    private void ApplyCanvasColor()
    {
        try
        {
            CanvasBackgroundBrush = new SolidColorBrush(Color.Parse(CanvasBackgroundHex));
        }
        catch
        {
            // Keep previous brush when color parsing fails.
        }
    }

    private async Task SaveSceneAsync()
    {
        var path = await PromptPathAsync("Save scene", "Save", "ribbon-canvas-scene.json", "Absolute path to .json scene file");
        if (string.IsNullOrWhiteSpace(path))
            return;

        try
        {
            var computed = ComputedShapeIds.ToHashSet(System.StringComparer.Ordinal);
            var baseScene = SceneDocumentMapper.ToDocument(Shapes, computed);
            var scene = new SceneDocument
            {
                Version = baseScene.Version,
                Shapes = baseScene.Shapes,
                CanvasBackgroundColor = CanvasBackgroundHex,
                ShowCanvasBoundary = ShowCanvasBoundary,
                CanvasBoundaryWidth = CanvasBoundaryWidth,
                CanvasBoundaryHeight = CanvasBoundaryHeight
            };

            var json = _sceneSerializer.Serialize(scene);
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            await File.WriteAllTextAsync(path, json);

            await InfoBarService.ShowAsync(infoBar =>
            {
                infoBar.Severity = InfoBarSeverity.Success;
                infoBar.Title = "Scene saved";
                infoBar.Message = path;
            });
        }
        catch (System.Exception ex)
        {
            await InfoBarService.ShowAsync(infoBar =>
            {
                infoBar.Severity = InfoBarSeverity.Error;
                infoBar.Title = "Scene save failed";
                infoBar.Message = ex.Message;
            });
        }
    }

    private async Task LoadSceneAsync()
    {
        var path = await PromptPathAsync("Load scene", "Load", "ribbon-canvas-scene.json", "Absolute path to .json scene file");
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

            CanvasBackgroundHex = scene.CanvasBackgroundColor;
            ShowCanvasBoundary = scene.ShowCanvasBoundary;
            CanvasBoundaryWidth = scene.CanvasBoundaryWidth;
            CanvasBoundaryHeight = scene.CanvasBoundaryHeight;

            await InfoBarService.ShowAsync(infoBar =>
            {
                infoBar.Severity = InfoBarSeverity.Success;
                infoBar.Title = "Scene loaded";
                infoBar.Message = $"{path} ({loaded.Shapes.Count} shape(s))";
            });
        }
        catch (System.Exception ex)
        {
            await InfoBarService.ShowAsync(infoBar =>
            {
                infoBar.Severity = InfoBarSeverity.Error;
                infoBar.Title = "Scene load failed";
                infoBar.Message = ex.Message;
            });
        }
    }

    private async Task ExportSvgAsync()
    {
        var path = await PromptPathAsync("Export SVG", "Export", "ribbon-canvas-export.svg", "Absolute path to .svg file");
        if (string.IsNullOrWhiteSpace(path))
            return;

        try
        {
            var computed = ComputedShapeIds.ToHashSet(System.StringComparer.Ordinal);
            var baseScene = SceneDocumentMapper.ToDocument(Shapes, computed);
            var scene = new SceneDocument
            {
                Version = baseScene.Version,
                Shapes = baseScene.Shapes,
                CanvasBackgroundColor = CanvasBackgroundHex,
                ShowCanvasBoundary = ShowCanvasBoundary,
                CanvasBoundaryWidth = CanvasBoundaryWidth,
                CanvasBoundaryHeight = CanvasBoundaryHeight
            };

            var svg = _svgExporter.Export(scene);
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            await File.WriteAllTextAsync(path, svg);

            await InfoBarService.ShowAsync(infoBar =>
            {
                infoBar.Severity = InfoBarSeverity.Success;
                infoBar.Title = "SVG exported";
                infoBar.Message = path;
            });
        }
        catch (System.Exception ex)
        {
            await InfoBarService.ShowAsync(infoBar =>
            {
                infoBar.Severity = InfoBarSeverity.Error;
                infoBar.Title = "SVG export failed";
                infoBar.Message = ex.Message;
            });
        }
    }

    private async Task<string?> PromptPathAsync(string title, string primaryButtonText, string defaultFileName, string watermark)
    {
        var pathBox = new TextBox
        {
            Width = 560,
            Watermark = watermark
        };

        var defaultPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), defaultFileName);
        pathBox.Text = defaultPath;

        var result = await DialogService.ShowAsync(dialog =>
        {
            dialog.Title = title;
            dialog.Content = new StackPanel
            {
                Spacing = 10,
                Children =
                {
                    new TextBlock { Text = "File path:" },
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
        return string.IsNullOrWhiteSpace(path) ? null : path;
    }

    private void ClearCanvas()
    {
        Shapes.Clear();
        ComputedShapeIds.Clear();
        OnPropertyChanged(nameof(StatusText));
    }

    private void ResetView()
    {
        Zoom = 1d;
        Pan = default;
    }

    private void OnShapesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => OnPropertyChanged(nameof(StatusText));

    private void OnComputedShapeIdsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => OnPropertyChanged(nameof(StatusText));

    private static string ToHexColor(IBrush brush)
    {
        if (brush is ISolidColorBrush solid)
            return solid.Color.ToString();

        return "#00000000";
    }
}
