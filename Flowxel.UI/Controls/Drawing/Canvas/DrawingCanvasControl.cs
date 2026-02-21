using System.Collections.Generic;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Flowxel.Core.Geometry.Shapes;
using Flowxel.UI.Services;
using AvaloniaPoint = global::Avalonia.Point;
using AvaloniaVector = global::Avalonia.Vector;
using FlowVector = Flowxel.Core.Geometry.Primitives.Vector;
using Shape = Flowxel.Core.Geometry.Shapes.Shape;

namespace Flowxel.UI.Controls.Drawing;

public partial class DrawingCanvasControl : Control
{
    private static readonly Cursor ArrowCursor = new(StandardCursorType.Arrow);
    private static readonly Cursor HandCursor = new(StandardCursorType.Hand);
    private static readonly Cursor GrabCursor = new(StandardCursorType.DragMove);
    private static readonly Cursor DrawCursor = new(StandardCursorType.Cross);
    private static readonly double[] BindingDash = [4d, 3d];
    private static readonly double[] ComputedDash = [5d, 4d];
    private static readonly double[] PreviewDash = [6d, 4d];
    private static readonly ICanvasInteractionHandler SelectInteraction = new SelectInteractionHandler();
    private static readonly ICanvasInteractionHandler BindInteraction = new BindInteractionHandler();
    private static readonly ICanvasInteractionHandler DrawInteraction = new DrawInteractionHandler();
    private static readonly Typeface CanvasTextTypeface = new("Segoe UI");
    private const int MaxTextLayoutCacheEntries = 1024;

    private const double MinShapeSize = 0.0001;

    private readonly DrawingCanvasContextMenu _contextMenu;
    private readonly ContextMenuInteractionCoordinator _contextInteraction;
    private IDrawingCanvasRenderer _renderer;
    private IDrawingCanvasRenderBackend _renderBackend;
    private DrawingCanvasRenderStats _lastRenderStats;
    private readonly Dictionary<string, Bitmap?> _imageCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<TextLayoutCacheKey, FormattedText> _textLayoutCache = new();
    private readonly HashSet<string> _invalidImageWarnings = new(StringComparer.OrdinalIgnoreCase);
    private DrawingCanvasSceneSnapshot _sceneSnapshot = DrawingCanvasSceneSnapshot.Empty;
    private bool _isSceneSnapshotDirty = true;

    private Shape? _previewShape;
    private Shape? _hoveredShape;
    private Shape? _selectedShape;
    private FlowVector? _gestureStartWorld;
    private FlowVector? _lastDragWorld;
    private AvaloniaPoint? _gestureStartScreen;
    private AvaloniaVector _panAtGestureStart;
    private bool _isMiddlePanning;
    private bool _isLifecycleAttached;
    private ShapeHandleKind _activeHandle = ShapeHandleKind.None;

    public static readonly StyledProperty<IList<Shape>> ShapesProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, IList<Shape>>(
            nameof(Shapes),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<DrawingTool> ActiveToolProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, DrawingTool>(
            nameof(ActiveTool),
            DrawingTool.Select,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> ZoomProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, double>(
            nameof(Zoom),
            1d,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<AvaloniaVector> PanProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, AvaloniaVector>(
            nameof(Pan),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<IList<string>> ComputedShapeIdsProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, IList<string>>(
            nameof(ComputedShapeIds),
            defaultValue: new List<string>(),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<DrawingInteractionMode> InteractionModeProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, DrawingInteractionMode>(
            nameof(InteractionMode),
            DrawingInteractionMode.Standard,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<IList<string>> BindingCandidateShapeIdsProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, IList<string>>(
            nameof(BindingCandidateShapeIds),
            defaultValue: new List<string>());

    public static readonly StyledProperty<ICommand?> ShapeInvokedCommandProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, ICommand?>(nameof(ShapeInvokedCommand));

    public static readonly StyledProperty<IContentDialogService?> DialogServiceProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, IContentDialogService?>(nameof(DialogService));

    public static readonly StyledProperty<IInfoBarService?> InfoBarServiceProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, IInfoBarService?>(nameof(InfoBarService));

    public static readonly StyledProperty<double> MinZoomProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, double>(nameof(MinZoom), 0.1d);

    public static readonly StyledProperty<double> MaxZoomProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, double>(nameof(MaxZoom), 16d);

    public static readonly StyledProperty<IBrush> CanvasBackgroundProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, IBrush>(
            nameof(CanvasBackground),
            Brushes.Transparent);

    public static readonly StyledProperty<bool> ShowCanvasBoundaryProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, bool>(nameof(ShowCanvasBoundary), false);

    public static readonly StyledProperty<double> CanvasBoundaryWidthProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, double>(nameof(CanvasBoundaryWidth), 0d);

    public static readonly StyledProperty<double> CanvasBoundaryHeightProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, double>(nameof(CanvasBoundaryHeight), 0d);

    public static readonly StyledProperty<IBrush> CanvasBoundaryStrokeProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, IBrush>(
            nameof(CanvasBoundaryStroke),
            Brushes.DimGray);

    public static readonly StyledProperty<IBrush> ShapeStrokeProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, IBrush>(
            nameof(ShapeStroke),
            Brushes.DeepSkyBlue);

    public static readonly StyledProperty<IBrush> SelectedStrokeProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, IBrush>(
            nameof(SelectedStroke),
            Brushes.DeepSkyBlue);

    public static readonly StyledProperty<IBrush> PreviewStrokeProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, IBrush>(
            nameof(PreviewStroke),
            Brushes.Orange);

    public static readonly StyledProperty<IBrush> HoverStrokeProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, IBrush>(
            nameof(HoverStroke),
            Brushes.Gold);

    public static readonly StyledProperty<IBrush> ComputedStrokeProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, IBrush>(
            nameof(ComputedStroke),
            Brushes.MediumSeaGreen);

    public static readonly StyledProperty<bool> UseDashedComputedStrokeProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, bool>(nameof(UseDashedComputedStroke), true);

    public static readonly StyledProperty<IBrush> HandleFillProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, IBrush>(
            nameof(HandleFill),
            Brushes.White);

    public static readonly StyledProperty<IBrush> HandleStrokeProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, IBrush>(
            nameof(HandleStroke),
            Brushes.DodgerBlue);

    public static readonly StyledProperty<double> HandleSizeProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, double>(nameof(HandleSize), 9d);

    public static readonly StyledProperty<IBrush> OriginXAxisBrushProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, IBrush>(
            nameof(OriginXAxisBrush),
            Brushes.Red);

    public static readonly StyledProperty<IBrush> OriginYAxisBrushProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, IBrush>(
            nameof(OriginYAxisBrush),
            Brushes.LimeGreen);

    public static readonly StyledProperty<double> OriginMarkerSizeProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, double>(nameof(OriginMarkerSize), 12d);

    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, double>(nameof(StrokeThickness), 2d);

    public static readonly StyledProperty<double> PointDisplayRadiusProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, double>(nameof(PointDisplayRadius), 4d);

    public static readonly StyledProperty<double> HitTestToleranceProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, double>(nameof(HitTestTolerance), 8d);

    public static readonly StyledProperty<bool> UseDebugOverlayRendererProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, bool>(nameof(UseDebugOverlayRenderer), false);

    public static readonly StyledProperty<DrawingCanvasRenderBackendKind> RenderBackendKindProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, DrawingCanvasRenderBackendKind>(
            nameof(RenderBackendKind),
            DrawingCanvasRenderBackendKind.Immediate);

    public static readonly StyledProperty<AvaloniaPoint> CursorAvaloniaPositionProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, AvaloniaPoint>(
            nameof(CursorAvaloniaPosition),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<AvaloniaPoint> CursorCanvasPositionProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, AvaloniaPoint>(
            nameof(CursorCanvasPosition),
            defaultBindingMode: BindingMode.TwoWay);

    public DrawingCanvasControl()
    {
        Focusable = true;
        ClipToBounds = true;
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.None);
        Shapes = [];
        ComputedShapeIds = [];

        _contextMenu = new DrawingCanvasContextMenu();
        _contextInteraction = new ContextMenuInteractionCoordinator(this, _contextMenu);
        _renderBackend = CreateRenderBackend();
        _renderer = CreateRenderer();
        ContextMenu = _contextMenu;
    }

    public IList<Shape> Shapes
    {
        get => GetValue(ShapesProperty);
        set => SetValue(ShapesProperty, value);
    }

    public DrawingTool ActiveTool
    {
        get => GetValue(ActiveToolProperty);
        set => SetValue(ActiveToolProperty, value);
    }

    public double Zoom
    {
        get => GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, Math.Clamp(value, MinZoom, MaxZoom));
    }

    public AvaloniaVector Pan
    {
        get => GetValue(PanProperty);
        set => SetValue(PanProperty, value);
    }

    public IList<string> ComputedShapeIds
    {
        get => GetValue(ComputedShapeIdsProperty);
        set => SetValue(ComputedShapeIdsProperty, value);
    }

    public DrawingInteractionMode InteractionMode
    {
        get => GetValue(InteractionModeProperty);
        set => SetValue(InteractionModeProperty, value);
    }

    public IList<string> BindingCandidateShapeIds
    {
        get => GetValue(BindingCandidateShapeIdsProperty);
        set => SetValue(BindingCandidateShapeIdsProperty, value);
    }

    public ICommand? ShapeInvokedCommand
    {
        get => GetValue(ShapeInvokedCommandProperty);
        set => SetValue(ShapeInvokedCommandProperty, value);
    }

    public IContentDialogService? DialogService
    {
        get => GetValue(DialogServiceProperty);
        set => SetValue(DialogServiceProperty, value);
    }

    public IInfoBarService? InfoBarService
    {
        get => GetValue(InfoBarServiceProperty);
        set => SetValue(InfoBarServiceProperty, value);
    }

    public double MinZoom
    {
        get => GetValue(MinZoomProperty);
        set => SetValue(MinZoomProperty, value);
    }

    public double MaxZoom
    {
        get => GetValue(MaxZoomProperty);
        set => SetValue(MaxZoomProperty, value);
    }

    public IBrush CanvasBackground
    {
        get => GetValue(CanvasBackgroundProperty);
        set => SetValue(CanvasBackgroundProperty, value);
    }

    public bool ShowCanvasBoundary
    {
        get => GetValue(ShowCanvasBoundaryProperty);
        set => SetValue(ShowCanvasBoundaryProperty, value);
    }

    public double CanvasBoundaryWidth
    {
        get => GetValue(CanvasBoundaryWidthProperty);
        set => SetValue(CanvasBoundaryWidthProperty, Math.Max(0, value));
    }

    public double CanvasBoundaryHeight
    {
        get => GetValue(CanvasBoundaryHeightProperty);
        set => SetValue(CanvasBoundaryHeightProperty, Math.Max(0, value));
    }

    public IBrush CanvasBoundaryStroke
    {
        get => GetValue(CanvasBoundaryStrokeProperty);
        set => SetValue(CanvasBoundaryStrokeProperty, value);
    }

    public IBrush ShapeStroke
    {
        get => GetValue(ShapeStrokeProperty);
        set => SetValue(ShapeStrokeProperty, value);
    }

    public IBrush SelectedStroke
    {
        get => GetValue(SelectedStrokeProperty);
        set => SetValue(SelectedStrokeProperty, value);
    }

    public IBrush PreviewStroke
    {
        get => GetValue(PreviewStrokeProperty);
        set => SetValue(PreviewStrokeProperty, value);
    }

    public IBrush HoverStroke
    {
        get => GetValue(HoverStrokeProperty);
        set => SetValue(HoverStrokeProperty, value);
    }

    public IBrush ComputedStroke
    {
        get => GetValue(ComputedStrokeProperty);
        set => SetValue(ComputedStrokeProperty, value);
    }

    public bool UseDashedComputedStroke
    {
        get => GetValue(UseDashedComputedStrokeProperty);
        set => SetValue(UseDashedComputedStrokeProperty, value);
    }

    public IBrush HandleFill
    {
        get => GetValue(HandleFillProperty);
        set => SetValue(HandleFillProperty, value);
    }

    public IBrush HandleStroke
    {
        get => GetValue(HandleStrokeProperty);
        set => SetValue(HandleStrokeProperty, value);
    }

    public double HandleSize
    {
        get => GetValue(HandleSizeProperty);
        set => SetValue(HandleSizeProperty, value);
    }

    public IBrush OriginXAxisBrush
    {
        get => GetValue(OriginXAxisBrushProperty);
        set => SetValue(OriginXAxisBrushProperty, value);
    }

    public IBrush OriginYAxisBrush
    {
        get => GetValue(OriginYAxisBrushProperty);
        set => SetValue(OriginYAxisBrushProperty, value);
    }

    public double OriginMarkerSize
    {
        get => GetValue(OriginMarkerSizeProperty);
        set => SetValue(OriginMarkerSizeProperty, value);
    }

    public double StrokeThickness
    {
        get => GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public double PointDisplayRadius
    {
        get => GetValue(PointDisplayRadiusProperty);
        set => SetValue(PointDisplayRadiusProperty, value);
    }

    public double HitTestTolerance
    {
        get => GetValue(HitTestToleranceProperty);
        set => SetValue(HitTestToleranceProperty, value);
    }

    public bool UseDebugOverlayRenderer
    {
        get => GetValue(UseDebugOverlayRendererProperty);
        set => SetValue(UseDebugOverlayRendererProperty, value);
    }

    public DrawingCanvasRenderBackendKind RenderBackendKind
    {
        get => GetValue(RenderBackendKindProperty);
        set => SetValue(RenderBackendKindProperty, value);
    }

    internal DrawingCanvasRenderStats LastRenderStats => _lastRenderStats;
    internal int TextLayoutCacheCount => _textLayoutCache.Count;

    public AvaloniaPoint CursorAvaloniaPosition
    {
        get => GetValue(CursorAvaloniaPositionProperty);
        set => SetValue(CursorAvaloniaPositionProperty, value);
    }

    public AvaloniaPoint CursorCanvasPosition
    {
        get => GetValue(CursorCanvasPositionProperty);
        set => SetValue(CursorCanvasPositionProperty, value);
    }
}
