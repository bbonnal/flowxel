using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Flowxel.Core.Geometry.Shapes;
using Flowxel.UI.Services;
using Flowxel.UI.Drawing;

namespace Flowxel.UITester.ViewModels;

public partial class DockingCanvasTestingPageViewModel : ViewModelBase
{
    private int _nextCanvasNumber = 1;

    public DockingCanvasTestingPageViewModel(IContentDialogService dialogService, IInfoBarService infoBarService)
    {
        DialogService = dialogService;
        InfoBarService = infoBarService;

        AddCanvasCommand = new RelayCommand(AddCanvasCommandExecute);
        SelectToolCommand = new RelayCommand(() => ApplyToolToFocused(DrawingTool.Select));
        SelectLineToolCommand = new RelayCommand(() => ApplyToolToFocused(DrawingTool.Line));
        SelectRectangleToolCommand = new RelayCommand(() => ApplyToolToFocused(DrawingTool.Rectangle));
        SelectCircleToolCommand = new RelayCommand(() => ApplyToolToFocused(DrawingTool.Circle));
        SelectArcToolCommand = new RelayCommand(() => ApplyToolToFocused(DrawingTool.Arc));
        SelectTextToolCommand = new RelayCommand(() => ApplyToolToFocused(DrawingTool.Text));
        SelectImageToolCommand = new RelayCommand(() => ApplyToolToFocused(DrawingTool.Image));
        ResetFocusedViewCommand = new RelayCommand(ResetFocusedView);
        ClearFocusedCanvasCommand = new RelayCommand(ClearFocusedCanvas);

        AddCanvas();
    }

    public IContentDialogService DialogService { get; }

    public IInfoBarService InfoBarService { get; }

    public ObservableCollection<CanvasDocumentViewModel> Canvases { get; } = [];

    [ObservableProperty]
    private CanvasDocumentViewModel? focusedCanvas;

    public IRelayCommand AddCanvasCommand { get; }
    public IRelayCommand SelectToolCommand { get; }
    public IRelayCommand SelectLineToolCommand { get; }
    public IRelayCommand SelectRectangleToolCommand { get; }
    public IRelayCommand SelectCircleToolCommand { get; }
    public IRelayCommand SelectArcToolCommand { get; }
    public IRelayCommand SelectTextToolCommand { get; }
    public IRelayCommand SelectImageToolCommand { get; }
    public IRelayCommand ResetFocusedViewCommand { get; }
    public IRelayCommand ClearFocusedCanvasCommand { get; }

    public string StatusText => FocusedCanvas is null
        ? $"Canvases: {Canvases.Count} | Focused: none"
        : $"Canvases: {Canvases.Count} | Focused: {FocusedCanvas.Title} | Tool: {FocusedCanvas.ActiveTool} | Shapes: {FocusedCanvas.Shapes.Count}";

    partial void OnFocusedCanvasChanged(CanvasDocumentViewModel? value)
        => OnPropertyChanged(nameof(StatusText));

    public CanvasDocumentViewModel AddCanvas()
    {
        var canvas = new CanvasDocumentViewModel($"Canvas {_nextCanvasNumber++}");
        canvas.Shapes.CollectionChanged += (_, _) => OnPropertyChanged(nameof(StatusText));
        canvas.PropertyChanged += OnCanvasPropertyChanged;
        Canvases.Add(canvas);
        FocusedCanvas = canvas;
        OnPropertyChanged(nameof(StatusText));
        return canvas;
    }

    private void AddCanvasCommandExecute()
    {
        AddCanvas();
    }

    public void FocusCanvas(Guid canvasId)
    {
        var canvas = Canvases.FirstOrDefault(x => x.Id == canvasId);
        if (canvas is null)
            return;

        FocusedCanvas = canvas;
    }

    public void RemoveCanvas(Guid canvasId)
    {
        var canvas = Canvases.FirstOrDefault(x => x.Id == canvasId);
        if (canvas is null)
            return;

        canvas.PropertyChanged -= OnCanvasPropertyChanged;
        Canvases.Remove(canvas);
        if (ReferenceEquals(FocusedCanvas, canvas))
            FocusedCanvas = Canvases.LastOrDefault();

        OnPropertyChanged(nameof(StatusText));
    }

    private void ApplyToolToFocused(DrawingTool tool)
    {
        if (FocusedCanvas is null)
            return;

        FocusedCanvas.ActiveTool = tool;
        OnPropertyChanged(nameof(StatusText));
    }

    private void ResetFocusedView()
    {
        if (FocusedCanvas is null)
            return;

        FocusedCanvas.Zoom = 1d;
        FocusedCanvas.Pan = default;
    }

    private void ClearFocusedCanvas()
    {
        if (FocusedCanvas is null)
            return;

        FocusedCanvas.Shapes.Clear();
        FocusedCanvas.ComputedShapeIds.Clear();
        OnPropertyChanged(nameof(StatusText));
    }

    private void OnCanvasPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender == FocusedCanvas &&
            (e.PropertyName == nameof(CanvasDocumentViewModel.ActiveTool) ||
             e.PropertyName == nameof(CanvasDocumentViewModel.Title)))
        {
            OnPropertyChanged(nameof(StatusText));
        }
    }
}

public partial class CanvasDocumentViewModel : ObservableObject
{
    public CanvasDocumentViewModel(string title)
    {
        Title = title;
    }

    public Guid Id { get; } = Guid.NewGuid();

    public string Title { get; }

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
    private IBrush canvasBackgroundBrush = new SolidColorBrush(Color.Parse("#101317"));

    [ObservableProperty]
    private bool showCanvasBoundary = true;

    [ObservableProperty]
    private double canvasBoundaryWidth = 1920;

    [ObservableProperty]
    private double canvasBoundaryHeight = 1080;
}
