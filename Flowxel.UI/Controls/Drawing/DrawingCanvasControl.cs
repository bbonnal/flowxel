using System.Collections.Specialized;
using System.Globalization;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Flowxel.Core.Geometry.Primitives;
using Flowxel.Core.Geometry.Shapes;
using Flowxel.UI.Controls;
using Flowxel.UI.Controls.Editors;
using Flowxel.UI.Services;
using Flowxel.UI.Drawing;
using Flowxel.UI.Drawing.Shapes;
using AvaloniaPoint = global::Avalonia.Point;
using AvaloniaVector = global::Avalonia.Vector;
using FlowPoint = Flowxel.Core.Geometry.Shapes.Point;
using FlowRectangle = Flowxel.Core.Geometry.Shapes.Rectangle;
using FlowVector = Flowxel.Core.Geometry.Primitives.Vector;
using Line = Flowxel.Core.Geometry.Shapes.Line;
using Shape = Flowxel.Core.Geometry.Shapes.Shape;

namespace Flowxel.UI.Controls.Drawing;

public class DrawingCanvasControl : Control
{
    private static readonly Cursor ArrowCursor = new(StandardCursorType.Arrow);
    private static readonly Cursor HandCursor = new(StandardCursorType.Hand);
    private static readonly Cursor GrabCursor = new(StandardCursorType.DragMove);
    private static readonly Cursor DrawCursor = new(StandardCursorType.Cross);

    private const double MinShapeSize = 0.0001;

    private readonly DrawingCanvasContextMenu _contextMenu;
    private readonly Dictionary<string, Bitmap?> _imageCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _invalidImageWarnings = new(StringComparer.OrdinalIgnoreCase);

    private Shape? _previewShape;
    private Shape? _hoveredShape;
    private Shape? _selectedShape;
    private Shape? _contextMenuTargetShape;
    private FlowVector? _gestureStartWorld;
    private FlowVector? _lastDragWorld;
    private AvaloniaPoint? _gestureStartScreen;
    private AvaloniaVector _panAtGestureStart;
    private bool _isMiddlePanning;
    private bool _openContextMenuOnRightRelease;
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

    public void ResetView()
    {
        Zoom = 1d;
        Pan = default;
        InvalidateVisual();
    }

    public void CenterViewOnOrigin()
    {
        Pan = new AvaloniaVector(Bounds.Width * 0.5, Bounds.Height * 0.5);
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        context.FillRectangle(CanvasBackground, new Rect(Bounds.Size));
        DrawCanvasBoundary(context);
        DrawOriginMarker(context);

        foreach (var shape in Shapes)
        {
            if (ReferenceEquals(shape, _selectedShape) || ReferenceEquals(shape, _hoveredShape))
                continue;

            var isComputed = IsComputedShape(shape);
            DrawShape(
                context,
                shape,
                isComputed ? ComputedStroke : ShapeStroke,
                GetShapeStrokeThickness(shape),
                isComputed ? [5, 4] : null);
        }

        if (_hoveredShape is not null && !ReferenceEquals(_hoveredShape, _selectedShape))
            DrawShape(context, _hoveredShape, HoverStroke, GetShapeStrokeThickness(_hoveredShape) + 1.25, null);

        if (_selectedShape is not null)
        {
            var selectedComputed = IsComputedShape(_selectedShape);
            DrawShape(
                context,
                _selectedShape,
                selectedComputed ? ComputedStroke : SelectedStroke,
                GetShapeStrokeThickness(_selectedShape) + 1.5,
                selectedComputed ? [5, 4] : null);
            if (!selectedComputed)
                DrawGrabHandles(context, _selectedShape);
        }

        if (_previewShape is not null)
            DrawShape(context, _previewShape, PreviewStroke, GetShapeStrokeThickness(_previewShape), [6, 4]);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ShapesProperty)
        {
            if (change.OldValue is IList<Shape> oldShapes)
                DetachShapesCollection(oldShapes);

            if (change.NewValue is IList<Shape> newShapes)
                AttachShapesCollection(newShapes);
        }

        if (change.Property == ComputedShapeIdsProperty)
        {
            if (change.OldValue is IList<string> oldComputed)
                DetachComputedShapeIdsCollection(oldComputed);

            if (change.NewValue is IList<string> newComputed)
                AttachComputedShapeIdsCollection(newComputed);

            InvalidateVisual();
        }

        if (change.Property == ZoomProperty ||
            change.Property == PanProperty ||
            change.Property == ActiveToolProperty ||
            change.Property == CanvasBackgroundProperty ||
            change.Property == ShowCanvasBoundaryProperty ||
            change.Property == CanvasBoundaryWidthProperty ||
            change.Property == CanvasBoundaryHeightProperty ||
            change.Property == CanvasBoundaryStrokeProperty ||
            change.Property == HoverStrokeProperty ||
            change.Property == ComputedStrokeProperty ||
            change.Property == SelectedStrokeProperty ||
            change.Property == HandleFillProperty ||
            change.Property == HandleStrokeProperty ||
            change.Property == HandleSizeProperty ||
            change.Property == OriginMarkerSizeProperty ||
            change.Property == OriginXAxisBrushProperty ||
            change.Property == OriginYAxisBrushProperty)
        {
            InvalidateVisual();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        AttachLifecycleHandlers();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        DetachLifecycleHandlers();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        Focus();
        var pointer = e.GetCurrentPoint(this);
        var screen = e.GetPosition(this);
        var world = ScreenToWorld(screen);
        UpdateCursorPositions(screen, world);

        if (pointer.Properties.IsRightButtonPressed)
        {
            _contextMenuTargetShape = FindHitShape(world);
            _selectedShape = _contextMenuTargetShape ?? _selectedShape;
            _hoveredShape = _contextMenuTargetShape;
            ConfigureContextMenuTarget(_contextMenuTargetShape);
            _openContextMenuOnRightRelease = true;
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (pointer.Properties.IsMiddleButtonPressed)
        {
            _isMiddlePanning = true;
            _gestureStartScreen = screen;
            _panAtGestureStart = Pan;
            e.Pointer.Capture(this);
            UpdateCursor();
            e.Handled = true;
            return;
        }

        if (!pointer.Properties.IsLeftButtonPressed)
            return;

        if (ActiveTool == DrawingTool.Select)
        {
            HandleSelectToolPointerPressed(e, world);
            return;
        }

        if (ActiveTool == DrawingTool.Point)
        {
            Shapes.Add(new FlowPoint { Pose = CreatePose(world.X, world.Y) });
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        _gestureStartWorld = world;
        _previewShape = ShapeInteractionEngine.BuildShape(ActiveTool, world, world, MinShapeSize);
        e.Pointer.Capture(this);
        UpdateCursor();
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        var screen = e.GetPosition(this);
        var world = ScreenToWorld(screen);
        UpdateCursorPositions(screen, world);

        if (_isMiddlePanning && _gestureStartScreen is not null)
        {
            var delta = screen - _gestureStartScreen.Value;
            Pan = _panAtGestureStart + delta;
            UpdateCursor();
            e.Handled = true;
            return;
        }

        if (ActiveTool == DrawingTool.Select && _selectedShape is not null && !IsComputedShape(_selectedShape) && _activeHandle != ShapeHandleKind.None)
        {
            ShapeInteractionEngine.ApplyHandleDrag(_selectedShape, _activeHandle, world, _lastDragWorld, MinShapeSize);
            _lastDragWorld = world;
            UpdateCursor();
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (_gestureStartWorld is not null)
        {
            _previewShape = ShapeInteractionEngine.BuildShape(ActiveTool, _gestureStartWorld.Value, world, MinShapeSize);
            UpdateCursor();
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        var previousHover = _hoveredShape;
        _hoveredShape = FindHitShape(world);
        UpdateCursor(world);
        if (!ReferenceEquals(previousHover, _hoveredShape))
            InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (e.InitialPressMouseButton == MouseButton.Middle)
        {
            _isMiddlePanning = false;
            _gestureStartScreen = null;
            e.Pointer.Capture(null);
            UpdateCursor();
            e.Handled = true;
            return;
        }

        if (e.InitialPressMouseButton == MouseButton.Right)
        {
            if (_openContextMenuOnRightRelease)
            {
                _openContextMenuOnRightRelease = false;
                Dispatcher.UIThread.Post(() => _contextMenu.Open(this), DispatcherPriority.Background);
            }

            e.Handled = true;
            return;
        }

        if (e.InitialPressMouseButton == MouseButton.Left && _activeHandle != ShapeHandleKind.None)
        {
            _activeHandle = ShapeHandleKind.None;
            _lastDragWorld = null;
            e.Pointer.Capture(null);
            UpdateCursor();
            e.Handled = true;
            return;
        }

        if (e.InitialPressMouseButton != MouseButton.Left || _gestureStartWorld is null)
            return;

        var currentWorld = ScreenToWorld(e.GetPosition(this));
        var finalShape = ShapeInteractionEngine.BuildShape(ActiveTool, _gestureStartWorld.Value, currentWorld, MinShapeSize);
        if (finalShape is not null)
            Shapes.Add(finalShape);

        _gestureStartWorld = null;
        _previewShape = null;
        e.Pointer.Capture(null);
        UpdateCursor();
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        if (_activeHandle == ShapeHandleKind.None && _gestureStartWorld is null && !_isMiddlePanning)
            _hoveredShape = null;

        UpdateCursor();
        InvalidateVisual();
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        var cursor = e.GetPosition(this);
        var worldBeforeZoom = ScreenToWorld(cursor);
        var zoomFactor = Math.Pow(1.12, e.Delta.Y);
        Zoom = Math.Clamp(Zoom * zoomFactor, MinZoom, MaxZoom);
        Pan = new AvaloniaVector(cursor.X - (worldBeforeZoom.X * Zoom), cursor.Y - (worldBeforeZoom.Y * Zoom));

        var worldAfterZoom = ScreenToWorld(cursor);
        UpdateCursorPositions(cursor, worldAfterZoom);
        UpdateCursor(worldAfterZoom);
        InvalidateVisual();
        e.Handled = true;
    }

    private void HandleSelectToolPointerPressed(PointerPressedEventArgs e, FlowVector world)
    {
        if (_selectedShape is not null && !IsComputedShape(_selectedShape))
        {
            var handle = HitTestHandle(_selectedShape, world);
            if (handle != ShapeHandleKind.None)
            {
                _activeHandle = handle;
                _lastDragWorld = world;
                e.Pointer.Capture(this);
                UpdateCursor();
                e.Handled = true;
                return;
            }
        }

        var hitShape = FindHitShape(world);
        _hoveredShape = hitShape;

        if (hitShape is null)
        {
            _selectedShape = null;
            _activeHandle = ShapeHandleKind.None;
            _lastDragWorld = null;
            UpdateCursor();
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (ReferenceEquals(hitShape, _selectedShape) && !IsComputedShape(hitShape) && e.ClickCount >= 2)
        {
            _ = ShowShapePropertiesDialogAsync(hitShape);
            _activeHandle = ShapeHandleKind.None;
            _lastDragWorld = null;
            UpdateCursor();
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (!ReferenceEquals(hitShape, _selectedShape))
        {
            _selectedShape = hitShape;
            UpdateCursor();
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (IsComputedShape(hitShape))
        {
            _activeHandle = ShapeHandleKind.None;
            _lastDragWorld = null;
            UpdateCursor();
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        _activeHandle = ShapeHandleKind.Move;
        _lastDragWorld = world;
        e.Pointer.Capture(this);
        UpdateCursor();
        InvalidateVisual();
        e.Handled = true;
    }

    private void ConfigureContextMenuTarget(Shape? shape)
    {
        if (shape is null)
        {
            _contextMenu.ConfigureForCanvas();
            return;
        }

        if (IsComputedShape(shape))
            _contextMenu.ConfigureForComputedShape();
        else
            _contextMenu.ConfigureForShape();
    }

    private void AttachShapesCollection(IList<Shape> shapes)
    {
        if (!_isLifecycleAttached)
            return;

        if (shapes is INotifyCollectionChanged observableCollection)
            observableCollection.CollectionChanged += OnShapesCollectionChanged;
    }

    private void DetachShapesCollection(IList<Shape> shapes)
    {
        if (shapes is INotifyCollectionChanged observableCollection)
            observableCollection.CollectionChanged -= OnShapesCollectionChanged;
    }

    private void AttachComputedShapeIdsCollection(IList<string> computedShapeIds)
    {
        if (!_isLifecycleAttached)
            return;

        if (computedShapeIds is INotifyCollectionChanged observableCollection)
            observableCollection.CollectionChanged += OnComputedShapeIdsCollectionChanged;
    }

    private void DetachComputedShapeIdsCollection(IList<string> computedShapeIds)
    {
        if (computedShapeIds is INotifyCollectionChanged observableCollection)
            observableCollection.CollectionChanged -= OnComputedShapeIdsCollectionChanged;
    }

    private void OnShapesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_hoveredShape is not null && !Shapes.Contains(_hoveredShape))
            _hoveredShape = null;

        if (_selectedShape is not null && !Shapes.Contains(_selectedShape))
            _selectedShape = null;

        for (var i = ComputedShapeIds.Count - 1; i >= 0; i--)
        {
            var id = ComputedShapeIds[i];
            if (!Shapes.Any(shape => shape.Id == id))
                ComputedShapeIds.RemoveAt(i);
        }

        InvalidateVisual();
    }

    private void OnComputedShapeIdsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => InvalidateVisual();

    private void OnDeleteShapeRequested(object? sender, EventArgs e)
    {
        if (_contextMenuTargetShape is null || IsComputedShape(_contextMenuTargetShape))
            return;

        Shapes.Remove(_contextMenuTargetShape);
        if (ReferenceEquals(_selectedShape, _contextMenuTargetShape))
            _selectedShape = null;

        _hoveredShape = null;
        _contextMenuTargetShape = null;
        _activeHandle = ShapeHandleKind.None;
        _lastDragWorld = null;
        InvalidateVisual();
    }

    private void OnCenterViewRequested(object? sender, EventArgs e)
        => CenterViewOnOrigin();

    private async void OnCanvasSettingsRequested(object? sender, EventArgs e)
        => await ShowCanvasSettingsDialogAsync();

    private async void OnPropertiesRequested(object? sender, EventArgs e)
    {
        if (_contextMenuTargetShape is null || IsComputedShape(_contextMenuTargetShape))
            return;

        await ShowShapePropertiesDialogAsync(_contextMenuTargetShape);
    }

    private bool IsComputedShape(Shape shape)
        => ComputedShapeIds.Contains(shape.Id);

    public void SetShapeComputed(Shape shape, bool isComputed)
    {
        if (isComputed)
        {
            if (!ComputedShapeIds.Contains(shape.Id))
                ComputedShapeIds.Add(shape.Id);
        }
        else
        {
            ComputedShapeIds.Remove(shape.Id);
        }

        InvalidateVisual();
    }

    private async Task ShowShapePropertiesDialogAsync(Shape shape)
    {
        if (DialogService is null)
            return;

        var content = BuildPropertiesEditor(shape, out var applyChanges);

        await DialogService.ShowAsync(dialog =>
        {
            dialog.Title = $"{shape.Type} properties";
            dialog.Content = content;
            dialog.PrimaryButtonText = "Apply";
            dialog.CloseButtonText = "Cancel";
            dialog.DefaultButton = DefaultButton.Primary;
            dialog.PrimaryButtonCommand = new ActionCommand(() =>
            {
                applyChanges();
                ClearImageCache();
                InvalidateVisual();
            });
        });
    }

    private async Task ShowCanvasSettingsDialogAsync()
    {
        if (DialogService is null)
            return;

        var initialColor = (CanvasBackground as ISolidColorBrush)?.Color.ToString() ?? "#00000000";
        var colorEditor = new TextEditor
        {
            Title = "Background color",
            Text = initialColor,
            Watermark = "#RRGGBB or #AARRGGBB",
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        var showBoundaryCheck = new CheckBox
        {
            IsChecked = ShowCanvasBoundary,
            Content = "Show boundary"
        };
        var widthEditor = new DoubleEditor
        {
            Title = "Boundary width",
            Value = CanvasBoundaryWidth,
            Digits = 3,
            Minimum = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        var heightEditor = new DoubleEditor
        {
            Title = "Boundary height",
            Value = CanvasBoundaryHeight,
            Digits = 3,
            Minimum = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var content = new StackPanel
        {
            Spacing = 8,
            MinWidth = 420,
            Children =
            {
                colorEditor,
                showBoundaryCheck,
                widthEditor,
                heightEditor
            }
        };

        await DialogService.ShowAsync(dialog =>
        {
            dialog.Title = "Canvas settings";
            dialog.Content = content;
            dialog.PrimaryButtonText = "Apply";
            dialog.CloseButtonText = "Cancel";
            dialog.DefaultButton = DefaultButton.Primary;
            dialog.PrimaryButtonCommand = new ActionCommand(() =>
            {
                CanvasBackground = TryParseBrushFromText(colorEditor.Text, CanvasBackground);
                ShowCanvasBoundary = showBoundaryCheck.IsChecked == true;
                CanvasBoundaryWidth = GetEditorValueOr(widthEditor, CanvasBoundaryWidth);
                CanvasBoundaryHeight = GetEditorValueOr(heightEditor, CanvasBoundaryHeight);
                InvalidateVisual();
            });
        });
    }

    private Control BuildPropertiesEditor(Shape shape, out Action apply)
    {
        var container = new StackPanel
        {
            Spacing = 8,
            MinWidth = 420
        };

        var xEditor = AddNumberEditor(container, "Position X", shape.Pose.Position.X);
        var yEditor = AddNumberEditor(container, "Position Y", shape.Pose.Position.Y);
        var oxEditor = AddNumberEditor(container, "Orientation X", shape.Pose.Orientation.X);
        var oyEditor = AddNumberEditor(container, "Orientation Y", shape.Pose.Orientation.Y);
        var lineWeightEditor = AddNumberEditor(container, "Line weight", shape.LineWeight, 0);
        var fillEditor = AddCheckBox(container, "Fill", shape.Fill);

        Action specificApply = () => { };

        switch (shape)
        {
            case Line line:
            {
                var lengthEditor = AddNumberEditor(container, "Length", line.Length, MinShapeSize);
                specificApply = () => line.Length = Math.Max(GetEditorValueOr(lengthEditor, line.Length), MinShapeSize);
                break;
            }
            case FlowRectangle rectangle:
            {
                var widthEditor = AddNumberEditor(container, "Width", rectangle.Width, MinShapeSize);
                var heightEditor = AddNumberEditor(container, "Height", rectangle.Height, MinShapeSize);
                specificApply = () =>
                {
                    rectangle.Width = Math.Max(GetEditorValueOr(widthEditor, rectangle.Width), MinShapeSize);
                    rectangle.Height = Math.Max(GetEditorValueOr(heightEditor, rectangle.Height), MinShapeSize);
                };
                break;
            }
            case Circle circle:
            {
                var radiusEditor = AddNumberEditor(container, "Radius", circle.Radius, MinShapeSize);
                specificApply = () => circle.Radius = Math.Max(GetEditorValueOr(radiusEditor, circle.Radius), MinShapeSize);
                break;
            }
            case ImageShape image:
            {
                var widthEditor = AddNumberEditor(container, "Width", image.Width, MinShapeSize);
                var heightEditor = AddNumberEditor(container, "Height", image.Height, MinShapeSize);
                var pathEditor = AddTextEditor(container, "Image path", image.SourcePath);
                specificApply = () =>
                {
                    image.Width = Math.Max(GetEditorValueOr(widthEditor, image.Width), MinShapeSize);
                    image.Height = Math.Max(GetEditorValueOr(heightEditor, image.Height), MinShapeSize);
                    image.SourcePath = pathEditor.Text?.Trim() ?? string.Empty;
                };
                break;
            }
            case TextBoxShape textBox:
            {
                var widthEditor = AddNumberEditor(container, "Width", textBox.Width, MinShapeSize);
                var heightEditor = AddNumberEditor(container, "Height", textBox.Height, MinShapeSize);
                var textValueEditor = AddTextEditor(container, "Text", textBox.Text);
                var fontSizeEditor = AddNumberEditor(container, "Font size", textBox.FontSize, 1);
                specificApply = () =>
                {
                    textBox.Width = Math.Max(GetEditorValueOr(widthEditor, textBox.Width), MinShapeSize);
                    textBox.Height = Math.Max(GetEditorValueOr(heightEditor, textBox.Height), MinShapeSize);
                    textBox.Text = textValueEditor.Text ?? string.Empty;
                    textBox.FontSize = Math.Max(GetEditorValueOr(fontSizeEditor, textBox.FontSize), 1);
                };
                break;
            }
            case ArrowShape arrow:
            {
                var lengthEditor = AddNumberEditor(container, "Length", arrow.Length, MinShapeSize);
                var headLengthEditor = AddNumberEditor(container, "Head length", arrow.HeadLength, MinShapeSize);
                specificApply = () =>
                {
                    arrow.Length = Math.Max(GetEditorValueOr(lengthEditor, arrow.Length), MinShapeSize);
                    arrow.HeadLength = Math.Max(GetEditorValueOr(headLengthEditor, arrow.HeadLength), MinShapeSize);
                };
                break;
            }
            case CenterlineRectangleShape centerlineRectangle:
            {
                var lengthEditor = AddNumberEditor(container, "Length", centerlineRectangle.Length, MinShapeSize);
                var widthEditor = AddNumberEditor(container, "Width", centerlineRectangle.Width, MinShapeSize);
                specificApply = () =>
                {
                    centerlineRectangle.Length = Math.Max(GetEditorValueOr(lengthEditor, centerlineRectangle.Length), MinShapeSize);
                    centerlineRectangle.Width = Math.Max(GetEditorValueOr(widthEditor, centerlineRectangle.Width), MinShapeSize);
                };
                break;
            }
            case ReferentialShape referential:
            {
                var xLengthEditor = AddNumberEditor(container, "X axis length", referential.XAxisLength, MinShapeSize);
                var yLengthEditor = AddNumberEditor(container, "Y axis length", referential.YAxisLength, MinShapeSize);
                specificApply = () =>
                {
                    referential.XAxisLength = Math.Max(GetEditorValueOr(xLengthEditor, referential.XAxisLength), MinShapeSize);
                    referential.YAxisLength = Math.Max(GetEditorValueOr(yLengthEditor, referential.YAxisLength), MinShapeSize);
                };
                break;
            }
            case DimensionShape dimension:
            {
                var lengthEditor = AddNumberEditor(container, "Length", dimension.Length, MinShapeSize);
                var offsetEditor = AddNumberEditor(container, "Offset", dimension.Offset);
                var textEditor = AddTextEditor(container, "Text", dimension.Text);
                specificApply = () =>
                {
                    dimension.Length = Math.Max(GetEditorValueOr(lengthEditor, dimension.Length), MinShapeSize);
                    dimension.Offset = GetEditorValueOr(offsetEditor, dimension.Offset);
                    dimension.Text = textEditor.Text ?? string.Empty;
                };
                break;
            }
            case AngleDimensionShape angleDimension:
            {
                var radiusEditor = AddNumberEditor(container, "Radius", angleDimension.Radius, MinShapeSize);
                var startDegEditor = AddNumberEditor(container, "Start angle (deg)", angleDimension.StartAngleRad * 180 / Math.PI);
                var sweepDegEditor = AddNumberEditor(container, "Sweep angle (deg)", angleDimension.SweepAngleRad * 180 / Math.PI);
                var textEditor = AddTextEditor(container, "Text", angleDimension.Text);
                specificApply = () =>
                {
                    angleDimension.Radius = Math.Max(GetEditorValueOr(radiusEditor, angleDimension.Radius), MinShapeSize);
                    angleDimension.StartAngleRad = GetEditorValueOr(startDegEditor, angleDimension.StartAngleRad * 180 / Math.PI) * Math.PI / 180;
                    angleDimension.SweepAngleRad = GetEditorValueOr(sweepDegEditor, angleDimension.SweepAngleRad * 180 / Math.PI) * Math.PI / 180;
                    angleDimension.Text = textEditor.Text ?? string.Empty;
                };
                break;
            }
            case TextShape text:
            {
                var textValueEditor = AddTextEditor(container, "Text", text.Text);
                var fontSizeEditor = AddNumberEditor(container, "Font size", text.FontSize, 1);
                specificApply = () =>
                {
                    text.Text = textValueEditor.Text ?? string.Empty;
                    text.FontSize = Math.Max(GetEditorValueOr(fontSizeEditor, text.FontSize), 1);
                };
                break;
            }
            case MultilineTextShape multilineText:
            {
                var textValueEditor = AddMultiLineTextEditor(container, "Text", multilineText.Text);
                var fontSizeEditor = AddNumberEditor(container, "Font size", multilineText.FontSize, 1);
                var widthEditor = AddNumberEditor(container, "Width", multilineText.Width, MinShapeSize);
                specificApply = () =>
                {
                    multilineText.Text = textValueEditor.Text ?? string.Empty;
                    multilineText.FontSize = Math.Max(GetEditorValueOr(fontSizeEditor, multilineText.FontSize), 1);
                    multilineText.Width = Math.Max(GetEditorValueOr(widthEditor, multilineText.Width), MinShapeSize);
                };
                break;
            }
            case IconShape icon:
            {
                var iconEditor = AddTextEditor(container, "Icon glyph", icon.IconKey);
                var sizeEditor = AddNumberEditor(container, "Size", icon.Size, MinShapeSize);
                specificApply = () =>
                {
                    icon.IconKey = string.IsNullOrWhiteSpace(iconEditor.Text) ? icon.IconKey : iconEditor.Text;
                    icon.Size = Math.Max(GetEditorValueOr(sizeEditor, icon.Size), MinShapeSize);
                };
                break;
            }
            case ArcShape arc:
            {
                var radiusEditor = AddNumberEditor(container, "Radius", arc.Radius, MinShapeSize);
                var startDegEditor = AddNumberEditor(container, "Start angle (deg)", arc.StartAngleRad * 180 / Math.PI);
                var sweepDegEditor = AddNumberEditor(container, "Sweep angle (deg)", arc.SweepAngleRad * 180 / Math.PI);
                specificApply = () =>
                {
                    arc.Radius = Math.Max(GetEditorValueOr(radiusEditor, arc.Radius), MinShapeSize);
                    arc.StartAngleRad = GetEditorValueOr(startDegEditor, arc.StartAngleRad * 180 / Math.PI) * Math.PI / 180;
                    arc.SweepAngleRad = GetEditorValueOr(sweepDegEditor, arc.SweepAngleRad * 180 / Math.PI) * Math.PI / 180;
                };
                break;
            }
        }

        apply = () =>
        {
            var x = GetEditorValueOr(xEditor, shape.Pose.Position.X);
            var y = GetEditorValueOr(yEditor, shape.Pose.Position.Y);
            var ox = GetEditorValueOr(oxEditor, shape.Pose.Orientation.X);
            var oy = GetEditorValueOr(oyEditor, shape.Pose.Orientation.Y);

            var orientation = new FlowVector(ox, oy);
            if (orientation.M <= 0.000001)
                orientation = shape.Pose.Orientation;

            shape.Pose = CreatePose(x, y, orientation);
            shape.LineWeight = Math.Max(GetEditorValueOr(lineWeightEditor, shape.LineWeight), 0);
            shape.Fill = fillEditor.IsChecked == true;
            specificApply();
        };

        return new ScrollViewer
        {
            Content = container,
            MaxHeight = 520
        };
    }

    private static TextEditor AddTextEditor(Panel parent, string title, string? value, string? watermark = null)
    {
        var editor = new TextEditor
        {
            Title = title,
            Text = value,
            Watermark = watermark,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        parent.Children.Add(editor);
        return editor;
    }

    private static MultiLineTextEditor AddMultiLineTextEditor(Panel parent, string title, string? value)
    {
        var editor = new MultiLineTextEditor
        {
            Title = title,
            Text = value,
            MinHeight = 120,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        parent.Children.Add(editor);
        return editor;
    }

    private static DoubleEditor AddNumberEditor(Panel parent, string title, double value, double? minimum = null, string? unit = null)
    {
        var editor = new DoubleEditor
        {
            Title = title,
            Value = value,
            Digits = 3,
            Minimum = minimum,
            Unit = unit,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        parent.Children.Add(editor);
        return editor;
    }

    private static CheckBox AddCheckBox(Panel parent, string title, bool value)
    {
        var editor = new CheckBox
        {
            Content = title,
            IsChecked = value
        };

        parent.Children.Add(editor);
        return editor;
    }

    private static double GetEditorValueOr(DoubleEditor editor, double fallback)
        => editor.Value ?? fallback;

    private static IBrush TryParseBrushFromText(string? text, IBrush fallback)
    {
        if (string.IsNullOrWhiteSpace(text))
            return fallback;

        if (Color.TryParse(text.Trim(), out var color))
            return new SolidColorBrush(color);

        return fallback;
    }

    private void OnContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        if (!e.TryGetPosition(this, out var position))
            position = CursorAvaloniaPosition;

        var world = ScreenToWorld(position);
        _contextMenuTargetShape = FindHitShape(world);
        if (_contextMenuTargetShape is not null)
            _selectedShape = _contextMenuTargetShape;

        ConfigureContextMenuTarget(_contextMenuTargetShape);
        InvalidateVisual();
    }

    private static Pose CreatePose(double x, double y, FlowVector? orientation = null)
        => new(new FlowVector(x, y), orientation ?? new FlowVector(1, 0));

    private void UpdateCursorPositions(AvaloniaPoint screen, FlowVector world)
    {
        CursorAvaloniaPosition = screen;
        CursorCanvasPosition = new AvaloniaPoint(world.X, world.Y);
    }

    private void UpdateCursor(FlowVector? world = null)
    {
        if (_isMiddlePanning || _activeHandle != ShapeHandleKind.None)
        {
            Cursor = GrabCursor;
            return;
        }

        if (ActiveTool != DrawingTool.Select)
        {
            Cursor = DrawCursor;
            return;
        }

        if (world is null)
        {
            Cursor = HandCursor;
            return;
        }

        if (_selectedShape is not null && !IsComputedShape(_selectedShape) && HitTestHandle(_selectedShape, world.Value) != ShapeHandleKind.None)
        {
            Cursor = HandCursor;
            return;
        }

        Cursor = FindHitShape(world.Value) is null ? ArrowCursor : HandCursor;
    }

    private Shape? FindHitShape(FlowVector world)
    {
        var tolerance = HitTestTolerance / Math.Max(Zoom, MinZoom);
        var pointRadius = PointDisplayRadius / Math.Max(Zoom, MinZoom);

        for (var i = Shapes.Count - 1; i >= 0; i--)
        {
            var shape = Shapes[i];
            if (ShapeInteractionEngine.IsShapePerimeterHit(shape, world, tolerance, pointRadius))
                return shape;
        }

        return null;
    }

    private ShapeHandleKind HitTestHandle(Shape shape, FlowVector world)
    {
        var tolerance = HandleSize / Math.Max(Zoom, MinZoom);
        return ShapeInteractionEngine.HitTestHandle(shape, world, tolerance);
    }

    private void DrawOriginMarker(DrawingContext context)
    {
        var center = WorldToScreen(new FlowVector(0, 0));
        var size = OriginMarkerSize;
        var xPen = new Pen(OriginXAxisBrush, 2);
        var yPen = new Pen(OriginYAxisBrush, 2);

        context.DrawLine(xPen, new AvaloniaPoint(center.X - size, center.Y), new AvaloniaPoint(center.X + size, center.Y));
        context.DrawLine(yPen, new AvaloniaPoint(center.X, center.Y - size), new AvaloniaPoint(center.X, center.Y + size));
    }

    private void DrawCanvasBoundary(DrawingContext context)
    {
        if (!ShowCanvasBoundary || CanvasBoundaryWidth <= 0 || CanvasBoundaryHeight <= 0)
            return;

        var topLeft = WorldToScreen(new FlowVector(0, 0));
        var bottomRight = WorldToScreen(new FlowVector(CanvasBoundaryWidth, CanvasBoundaryHeight));
        var rect = CreateRectFromPoints(topLeft, bottomRight);
        context.DrawRectangle(null, new Pen(CanvasBoundaryStroke, 1.5), rect);
    }

    private void DrawGrabHandles(DrawingContext context, Shape shape)
    {
        var pen = new Pen(HandleStroke, 1.5);
        var half = HandleSize * 0.5;
        foreach (var handle in ShapeInteractionEngine.GetHandles(shape))
        {
            var screen = WorldToScreen(handle.Position);
            var rect = new Rect(screen.X - half, screen.Y - half, HandleSize, HandleSize);
            context.DrawRectangle(HandleFill, pen, rect);
        }
    }

    private void DrawShape(DrawingContext context, Shape shape, IBrush strokeBrush, double thickness, IReadOnlyList<double>? dashArray)
    {
        var fillBrush = shape.Fill ? strokeBrush : null;
        var pen = dashArray is null
            ? new Pen(strokeBrush, thickness)
            : new Pen(strokeBrush, thickness, dashStyle: new DashStyle(dashArray, 0));

        switch (shape)
        {
            case FlowPoint point:
            {
                var p = WorldToScreen(point.Pose.Position);
                context.DrawEllipse(strokeBrush, pen, p, PointDisplayRadius, PointDisplayRadius);
                break;
            }
            case Line line:
            {
                var p1 = WorldToScreen(line.StartPoint.Position);
                var p2 = WorldToScreen(line.EndPoint.Position);
                context.DrawLine(pen, p1, p2);
                break;
            }
            case FlowRectangle rectangle:
                DrawClosedPolygon(context, pen, fillBrush,
                    rectangle.TopLeft.Position,
                    rectangle.TopRight.Position,
                    rectangle.BottomRight.Position,
                    rectangle.BottomLeft.Position,
                    rectangle.TopLeft.Position);
                break;
            case Circle circle:
            {
                var center = WorldToScreen(circle.Pose.Position);
                var radius = circle.Radius * Zoom;
                context.DrawEllipse(fillBrush, pen, center, radius, radius);
                break;
            }
            case ImageShape image:
                DrawImageShape(context, image, pen);
                break;
            case TextBoxShape textBox:
                DrawTextBoxShape(context, textBox, pen, strokeBrush);
                break;
            case ArrowShape arrow:
                DrawArrowShape(context, arrow, pen);
                break;
            case CenterlineRectangleShape centerlineRectangle:
                DrawClosedPolygon(context, pen, fillBrush,
                    centerlineRectangle.TopLeft,
                    centerlineRectangle.TopRight,
                    centerlineRectangle.BottomRight,
                    centerlineRectangle.BottomLeft,
                    centerlineRectangle.TopLeft);
                break;
            case ReferentialShape referential:
                DrawReferentialShape(context, referential, pen);
                break;
            case DimensionShape dimension:
                DrawDimensionShape(context, dimension, pen, strokeBrush);
                break;
            case AngleDimensionShape angleDimension:
                DrawAngleDimensionShape(context, angleDimension, pen, strokeBrush);
                break;
            case TextShape text:
                DrawTextShape(context, text, strokeBrush);
                break;
            case MultilineTextShape multilineText:
                DrawMultilineTextShape(context, multilineText, strokeBrush);
                break;
            case IconShape icon:
                DrawIconShape(context, icon, strokeBrush);
                break;
            case ArcShape arc:
                DrawArcShape(context, arc, pen);
                break;
        }
    }

    private void DrawImageShape(DrawingContext context, ImageShape image, Pen pen)
    {
        var fillBrush = image.Fill ? pen.Brush : null;
        DrawClosedPolygon(context, pen, fillBrush, image.TopLeft, image.TopRight, image.BottomRight, image.BottomLeft, image.TopLeft);

        var bitmap = TryGetBitmap(image.SourcePath);
        if (bitmap is null)
        {
            context.DrawLine(pen, WorldToScreen(image.TopLeft), WorldToScreen(image.BottomRight));
            context.DrawLine(pen, WorldToScreen(image.TopRight), WorldToScreen(image.BottomLeft));
            return;
        }

        var topLeft = WorldToScreen(image.TopLeft);
        var bottomRight = WorldToScreen(image.BottomRight);
        var rect = CreateRectFromPoints(topLeft, bottomRight);
        context.DrawImage(bitmap, new Rect(0, 0, bitmap.PixelSize.Width, bitmap.PixelSize.Height), rect);
        context.DrawRectangle(null, pen, rect);
    }

    private void DrawTextBoxShape(DrawingContext context, TextBoxShape textBox, Pen pen, IBrush strokeBrush)
    {
        var fillBrush = textBox.Fill ? pen.Brush : null;
        DrawClosedPolygon(context, pen, fillBrush, textBox.TopLeft, textBox.TopRight, textBox.BottomRight, textBox.BottomLeft, textBox.TopLeft);

        var topLeft = WorldToScreen(textBox.TopLeft);
        var bottomRight = WorldToScreen(textBox.BottomRight);
        var rect = CreateRectFromPoints(topLeft, bottomRight);

        var fontSize = Math.Max(8, textBox.FontSize * Zoom);
        var formattedText = new FormattedText(
            textBox.Text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            fontSize,
            strokeBrush);

        var textPosition = new AvaloniaPoint(rect.X + 6, rect.Y + 4);
        context.DrawText(formattedText, textPosition);
    }

    private void DrawArrowShape(DrawingContext context, ArrowShape arrow, Pen pen)
    {
        var start = WorldToScreen(arrow.StartPoint);
        var end = WorldToScreen(arrow.EndPoint);
        var headLeft = WorldToScreen(arrow.HeadLeftPoint);
        var headRight = WorldToScreen(arrow.HeadRightPoint);

        context.DrawLine(pen, start, end);
        context.DrawLine(pen, end, headLeft);
        context.DrawLine(pen, end, headRight);
    }

    private void DrawReferentialShape(DrawingContext context, ReferentialShape referential, Pen pen)
    {
        var origin = WorldToScreen(referential.Origin);
        var xEnd = WorldToScreen(referential.XAxisEnd);
        var yEnd = WorldToScreen(referential.YAxisEnd);

        context.DrawLine(pen, origin, xEnd);
        context.DrawLine(pen, origin, yEnd);

        DrawArrowHead(context, pen, referential.XAxisEnd, referential.Origin, 14);
        DrawArrowHead(context, pen, referential.YAxisEnd, referential.Origin, 14);
    }

    private void DrawDimensionShape(DrawingContext context, DimensionShape dimension, Pen pen, IBrush strokeBrush)
    {
        var start = WorldToScreen(dimension.StartPoint);
        var end = WorldToScreen(dimension.EndPoint);
        var offsetStart = WorldToScreen(dimension.OffsetStart);
        var offsetEnd = WorldToScreen(dimension.OffsetEnd);

        context.DrawLine(pen, start, offsetStart);
        context.DrawLine(pen, end, offsetEnd);
        context.DrawLine(pen, offsetStart, offsetEnd);

        DrawArrowHead(context, pen, dimension.OffsetStart, dimension.OffsetEnd, 12);
        DrawArrowHead(context, pen, dimension.OffsetEnd, dimension.OffsetStart, 12);

        var mid = WorldToScreen(dimension.OffsetMidpoint);
        var label = string.IsNullOrWhiteSpace(dimension.Text) ? dimension.Length.ToString("0.##") : dimension.Text;
        var formattedText = new FormattedText(
            label,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            Math.Max(10, 12 * Zoom),
            strokeBrush);

        context.DrawText(formattedText, new AvaloniaPoint(mid.X + 4, mid.Y - formattedText.Height - 2));
    }

    private void DrawAngleDimensionShape(DrawingContext context, AngleDimensionShape angleDimension, Pen pen, IBrush strokeBrush)
    {
        context.DrawLine(pen, WorldToScreen(angleDimension.Center), WorldToScreen(angleDimension.StartPoint));
        context.DrawLine(pen, WorldToScreen(angleDimension.Center), WorldToScreen(angleDimension.EndPoint));

        DrawArc(context, pen, angleDimension);

        var startTanAnchor = angleDimension.PointOnArc(angleDimension.StartAngleRad + (angleDimension.SweepAngleRad >= 0 ? 0.08 : -0.08));
        var endTanAnchor = angleDimension.PointOnArc(angleDimension.EndAngleRad - (angleDimension.SweepAngleRad >= 0 ? 0.08 : -0.08));
        DrawArrowHead(context, pen, angleDimension.StartPoint, startTanAnchor, 12);
        DrawArrowHead(context, pen, angleDimension.EndPoint, endTanAnchor, 12);

        var mid = WorldToScreen(angleDimension.MidPoint);
        var label = string.IsNullOrWhiteSpace(angleDimension.Text)
            ? $"{Math.Abs(angleDimension.SweepAngleRad * 180 / Math.PI):0.#}°"
            : angleDimension.Text;

        var formattedText = new FormattedText(
            label,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            Math.Max(10, 12 * Zoom),
            strokeBrush);

        context.DrawText(formattedText, new AvaloniaPoint(mid.X + 4, mid.Y - formattedText.Height - 2));
    }

    private void DrawTextShape(DrawingContext context, TextShape text, IBrush strokeBrush)
    {
        var formattedText = new FormattedText(
            text.Text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            Math.Max(8, text.FontSize * Zoom),
            strokeBrush);

        var position = WorldToScreen(text.Pose.Position);
        context.DrawText(formattedText, position);
    }

    private void DrawMultilineTextShape(DrawingContext context, MultilineTextShape multilineText, IBrush strokeBrush)
    {
        var formattedText = new FormattedText(
            multilineText.Text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            Math.Max(8, multilineText.FontSize * Zoom),
            strokeBrush)
        {
            MaxTextWidth = Math.Max(8, multilineText.Width * Zoom)
        };

        var position = WorldToScreen(multilineText.Pose.Position);
        context.DrawText(formattedText, position);
    }

    private void DrawIconShape(DrawingContext context, IconShape icon, IBrush strokeBrush)
    {
        var formattedText = new FormattedText(
            icon.IconKey,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            Math.Max(8, icon.Size * Zoom),
            strokeBrush);

        var position = WorldToScreen(icon.Pose.Position);
        context.DrawText(formattedText, position);
    }

    private void DrawArcShape(DrawingContext context, ArcShape arc, Pen pen)
    {
        var span = Math.Abs(arc.SweepAngleRad);
        var segmentCount = Math.Clamp((int)(span * 18), 8, 72);
        var previous = arc.PointOnArc(arc.StartAngleRad);
        for (var i = 1; i <= segmentCount; i++)
        {
            var t = (double)i / segmentCount;
            var angle = arc.StartAngleRad + (arc.SweepAngleRad * t);
            var current = arc.PointOnArc(angle);
            context.DrawLine(pen, WorldToScreen(previous), WorldToScreen(current));
            previous = current;
        }
    }

    private void DrawClosedPolygon(DrawingContext context, Pen pen, IBrush? fill, params FlowVector[] points)
    {
        if (fill is not null && points.Length >= 3)
        {
            var geometry = new StreamGeometry();
            using (var gctx = geometry.Open())
            {
                gctx.BeginFigure(WorldToScreen(points[0]), true);
                for (var i = 1; i < points.Length; i++)
                    gctx.LineTo(WorldToScreen(points[i]));
                gctx.EndFigure(true);
            }

            context.DrawGeometry(fill, null, geometry);
        }

        for (var i = 1; i < points.Length; i++)
            context.DrawLine(pen, WorldToScreen(points[i - 1]), WorldToScreen(points[i]));
    }

    private double GetShapeStrokeThickness(Shape shape)
    {
        var lineWeight = shape.LineWeight;
        return lineWeight > 0 ? lineWeight : StrokeThickness;
    }

    private void DrawArrowHead(DrawingContext context, Pen pen, FlowVector tip, FlowVector tailAnchor, double pixelSize)
    {
        var direction = tip - tailAnchor;
        if (direction.M <= 0.0000001)
            return;

        var dir = direction.Normalize();
        var lengthWorld = pixelSize / Math.Max(Zoom, MinZoom);
        var left = tip.Translate(dir.Scale(-lengthWorld).Rotate(Math.PI / 7));
        var right = tip.Translate(dir.Scale(-lengthWorld).Rotate(-Math.PI / 7));

        context.DrawLine(pen, WorldToScreen(tip), WorldToScreen(left));
        context.DrawLine(pen, WorldToScreen(tip), WorldToScreen(right));
    }

    private void DrawArc(DrawingContext context, Pen pen, AngleDimensionShape angleDimension)
    {
        var span = Math.Abs(angleDimension.SweepAngleRad);
        var segmentCount = Math.Clamp((int)(span * 18), 8, 72);
        var previous = angleDimension.PointOnArc(angleDimension.StartAngleRad);
        for (var i = 1; i <= segmentCount; i++)
        {
            var t = (double)i / segmentCount;
            var angle = angleDimension.StartAngleRad + (angleDimension.SweepAngleRad * t);
            var current = angleDimension.PointOnArc(angle);
            context.DrawLine(pen, WorldToScreen(previous), WorldToScreen(current));
            previous = current;
        }
    }

    private Bitmap? TryGetBitmap(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        if (_imageCache.TryGetValue(path, out var cached))
            return cached;

        try
        {
            var bitmap = new Bitmap(path);
            _imageCache[path] = bitmap;
            _invalidImageWarnings.Remove(path);
            return bitmap;
        }
        catch (Exception ex)
        {
            _imageCache[path] = null;

            if (_invalidImageWarnings.Add(path))
                Dispatcher.UIThread.Post(() => _ = ShowImageLoadWarningAsync(path, ex.Message), DispatcherPriority.Background);

            return null;
        }
    }

    private void AttachLifecycleHandlers()
    {
        if (_isLifecycleAttached)
            return;

        _isLifecycleAttached = true;
        _contextMenu.CanvasSettingsRequested += OnCanvasSettingsRequested;
        _contextMenu.DeleteShapeRequested += OnDeleteShapeRequested;
        _contextMenu.CenterViewRequested += OnCenterViewRequested;
        _contextMenu.PropertiesRequested += OnPropertiesRequested;
        ContextRequested += OnContextRequested;
        AttachShapesCollection(Shapes);
        AttachComputedShapeIdsCollection(ComputedShapeIds);
    }

    private void DetachLifecycleHandlers()
    {
        if (!_isLifecycleAttached)
            return;

        _contextMenu.CanvasSettingsRequested -= OnCanvasSettingsRequested;
        _contextMenu.DeleteShapeRequested -= OnDeleteShapeRequested;
        _contextMenu.CenterViewRequested -= OnCenterViewRequested;
        _contextMenu.PropertiesRequested -= OnPropertiesRequested;
        ContextRequested -= OnContextRequested;
        DetachShapesCollection(Shapes);
        DetachComputedShapeIdsCollection(ComputedShapeIds);
        _contextMenu.Close();
        _openContextMenuOnRightRelease = false;
        _isMiddlePanning = false;
        _gestureStartWorld = null;
        _gestureStartScreen = null;
        _activeHandle = ShapeHandleKind.None;
        _lastDragWorld = null;
        _previewShape = null;
        _hoveredShape = null;
        _contextMenuTargetShape = null;
        _selectedShape = null;
        ClearImageCache();
        _isLifecycleAttached = false;
    }

    private void ClearImageCache()
    {
        foreach (var cachedBitmap in _imageCache.Values)
            cachedBitmap?.Dispose();

        _imageCache.Clear();
    }

    private Rect CreateRectFromPoints(AvaloniaPoint p1, AvaloniaPoint p2)
        => new(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y), Math.Abs(p2.X - p1.X), Math.Abs(p2.Y - p1.Y));

    private async Task ShowImageLoadWarningAsync(string path, string details)
    {
        if (InfoBarService is null)
            return;

        await InfoBarService.ShowAsync(infoBar =>
        {
            infoBar.Severity = InfoBarSeverity.Warning;
            infoBar.Title = "Image load failed";
            infoBar.Message = $"Could not load image at '{path}'. {details}";
        });
    }

    private AvaloniaPoint WorldToScreen(FlowVector world)
        => new((world.X * Zoom) + Pan.X, (world.Y * Zoom) + Pan.Y);

    private FlowVector ScreenToWorld(AvaloniaPoint screen)
        => new((screen.X - Pan.X) / Zoom, (screen.Y - Pan.Y) / Zoom);

    private sealed class ActionCommand(Action execute) : ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => execute();
    }
}
