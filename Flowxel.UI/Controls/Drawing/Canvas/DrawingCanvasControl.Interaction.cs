using System;
using Avalonia;
using Avalonia.Input;
using Avalonia.Threading;
using Flowxel.Core.Geometry.Primitives;
using Shape = Flowxel.Core.Geometry.Shapes.Shape;
using Vector = Flowxel.Core.Geometry.Primitives.Vector;

namespace Flowxel.UI.Controls.Drawing;

public partial class DrawingCanvasControl
{
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
            InvalidateScene();
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

        GetActiveInteractionHandler().OnPointerPressed(this, e, world);
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

        GetActiveInteractionHandler().OnPointerMoved(this, e, world);
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

        if (e.InitialPressMouseButton != MouseButton.Left)
            return;

        var world = ScreenToWorld(e.GetPosition(this));
        GetActiveInteractionHandler().OnPointerReleased(this, e, world);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        if (_activeHandle == ShapeHandleKind.None && _gestureStartWorld is null && !_isMiddlePanning)
            _hoveredShape = null;

        UpdateCursor();
        InvalidateScene();
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        var cursor = e.GetPosition(this);
        var zoomFactor = Math.Pow(1.12, e.Delta.Y);
        var nextViewport = Viewport.ZoomAroundScreen(cursor, zoomFactor, MinZoom, MaxZoom);
        SetViewport(nextViewport);

        var worldAfterZoom = ScreenToWorld(cursor);
        UpdateCursorPositions(cursor, worldAfterZoom);
        UpdateCursor(worldAfterZoom);
        InvalidateScene();
        e.Handled = true;
    }

    private ICanvasInteractionHandler GetActiveInteractionHandler()
    {
        if (ActiveTool == DrawingTool.Select)
            return InteractionMode == DrawingInteractionMode.Bind ? BindInteraction : SelectInteraction;

        return DrawInteraction;
    }

    private void UpdateCursor(Vector? world = null)
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

        if (InteractionMode == DrawingInteractionMode.Bind)
        {
            if (world is null)
            {
                Cursor = ArrowCursor;
                return;
            }

            var hit = FindHitBindingCandidateShape(world.Value);
            Cursor = hit is not null ? HandCursor : ArrowCursor;
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

    private Shape? FindHitShape(Vector world)
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

    private Shape? FindHitBindingCandidateShape(Vector world)
    {
        if (InteractionMode != DrawingInteractionMode.Bind)
            return null;

        var tolerance = HitTestTolerance / Math.Max(Zoom, MinZoom);
        var pointRadius = PointDisplayRadius / Math.Max(Zoom, MinZoom);

        for (var i = Shapes.Count - 1; i >= 0; i--)
        {
            var shape = Shapes[i];
            if (!IsBindingCandidate(shape))
                continue;

            if (ShapeInteractionEngine.IsShapePerimeterHit(shape, world, tolerance, pointRadius))
                return shape;
        }

        return null;
    }

    private ShapeHandleKind HitTestHandle(Shape shape, Vector world)
    {
        var tolerance = HandleSize / Math.Max(Zoom, MinZoom);
        return ShapeInteractionEngine.HitTestHandle(shape, world, tolerance);
    }

    private void RefreshHover(Vector world, Func<Vector, Shape?> hitTest)
    {
        var previousHover = _hoveredShape;
        _hoveredShape = hitTest(world);
        UpdateCursor(world);
        if (!ReferenceEquals(previousHover, _hoveredShape))
            InvalidateScene();
    }

    private void StartShapeDrag(ShapeHandleKind handle, Vector world, IPointer pointer)
    {
        _activeHandle = handle;
        _lastDragWorld = world;
        pointer.Capture(this);
        UpdateCursor();
        InvalidateScene();
    }

    private void ClearShapeDrag(IPointer? pointer = null)
    {
        _activeHandle = ShapeHandleKind.None;
        _lastDragWorld = null;
        pointer?.Capture(null);
    }

    private void ClearSelectionAndDrag()
    {
        _selectedShape = null;
        ClearShapeDrag();
        UpdateCursor();
        InvalidateScene();
    }

    private interface ICanvasInteractionHandler
    {
        void OnPointerPressed(DrawingCanvasControl canvas, PointerPressedEventArgs e, Vector world);
        void OnPointerMoved(DrawingCanvasControl canvas, PointerEventArgs e, Vector world);
        void OnPointerReleased(DrawingCanvasControl canvas, PointerReleasedEventArgs e, Vector world);
    }
}
