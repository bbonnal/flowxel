using System;
using Avalonia;
using Avalonia.Input;
using Avalonia.Threading;
using Flowxel.UI.Controls.Drawing.Shapes;
using FlowPoint = Flowxel.Core.Geometry.Shapes.Point;
using FlowVector = Flowxel.Core.Geometry.Primitives.Vector;
using Shape = Flowxel.Core.Geometry.Shapes.Shape;

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

    private Shape? FindHitBindingCandidateShape(FlowVector world)
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

    private ShapeHandleKind HitTestHandle(Shape shape, FlowVector world)
    {
        var tolerance = HandleSize / Math.Max(Zoom, MinZoom);
        return ShapeInteractionEngine.HitTestHandle(shape, world, tolerance);
    }

    private interface ICanvasInteractionHandler
    {
        void OnPointerPressed(DrawingCanvasControl canvas, PointerPressedEventArgs e, FlowVector world);
        void OnPointerMoved(DrawingCanvasControl canvas, PointerEventArgs e, FlowVector world);
        void OnPointerReleased(DrawingCanvasControl canvas, PointerReleasedEventArgs e, FlowVector world);
    }

    private sealed class BindInteractionHandler : ICanvasInteractionHandler
    {
        public void OnPointerPressed(DrawingCanvasControl canvas, PointerPressedEventArgs e, FlowVector world)
        {
            var bindHitShape = canvas.FindHitBindingCandidateShape(world);
            if (bindHitShape is not null)
            {
                canvas._selectedShape = bindHitShape;
                if (canvas.ShapeInvokedCommand?.CanExecute(bindHitShape.Id) == true)
                    canvas.ShapeInvokedCommand.Execute(bindHitShape.Id);
            }

            canvas._activeHandle = ShapeHandleKind.None;
            canvas._lastDragWorld = null;
            canvas.UpdateCursor(world);
            canvas.InvalidateScene();
            e.Handled = true;
        }

        public void OnPointerMoved(DrawingCanvasControl canvas, PointerEventArgs e, FlowVector world)
        {
            var previousHover = canvas._hoveredShape;
            canvas._hoveredShape = canvas.FindHitBindingCandidateShape(world);
            canvas.UpdateCursor(world);
            if (!ReferenceEquals(previousHover, canvas._hoveredShape))
                canvas.InvalidateScene();
        }

        public void OnPointerReleased(DrawingCanvasControl canvas, PointerReleasedEventArgs e, FlowVector world)
        {
        }
    }

    private sealed class SelectInteractionHandler : ICanvasInteractionHandler
    {
        public void OnPointerPressed(DrawingCanvasControl canvas, PointerPressedEventArgs e, FlowVector world)
        {
            if (canvas._selectedShape is not null && !canvas.IsComputedShape(canvas._selectedShape))
            {
                var handle = canvas.HitTestHandle(canvas._selectedShape, world);
                if (handle != ShapeHandleKind.None)
                {
                    canvas._activeHandle = handle;
                    canvas._lastDragWorld = world;
                    e.Pointer.Capture(canvas);
                    canvas.UpdateCursor();
                    e.Handled = true;
                    return;
                }
            }

            var hitShape = canvas.FindHitShape(world);
            canvas._hoveredShape = hitShape;

            if (hitShape is null)
            {
                canvas._selectedShape = null;
                canvas._activeHandle = ShapeHandleKind.None;
                canvas._lastDragWorld = null;
                canvas.UpdateCursor();
                canvas.InvalidateScene();
                e.Handled = true;
                return;
            }

            if (ReferenceEquals(hitShape, canvas._selectedShape) && !canvas.IsComputedShape(hitShape) && e.ClickCount >= 2)
            {
                _ = canvas.ShowShapePropertiesDialogAsync(hitShape);
                canvas._activeHandle = ShapeHandleKind.None;
                canvas._lastDragWorld = null;
                canvas.UpdateCursor();
                canvas.InvalidateScene();
                e.Handled = true;
                return;
            }

            if (!ReferenceEquals(hitShape, canvas._selectedShape))
            {
                canvas._selectedShape = hitShape;
                canvas.UpdateCursor();
                canvas.InvalidateScene();
                e.Handled = true;
                return;
            }

            if (canvas.IsComputedShape(hitShape))
            {
                canvas._activeHandle = ShapeHandleKind.None;
                canvas._lastDragWorld = null;
                canvas.UpdateCursor();
                canvas.InvalidateScene();
                e.Handled = true;
                return;
            }

            canvas._activeHandle = ShapeHandleKind.Move;
            canvas._lastDragWorld = world;
            e.Pointer.Capture(canvas);
            canvas.UpdateCursor();
            canvas.InvalidateScene();
            e.Handled = true;
        }

        public void OnPointerMoved(DrawingCanvasControl canvas, PointerEventArgs e, FlowVector world)
        {
            if (canvas._selectedShape is not null && !canvas.IsComputedShape(canvas._selectedShape) && canvas._activeHandle != ShapeHandleKind.None)
            {
                ShapeInteractionEngine.ApplyHandleDrag(canvas._selectedShape, canvas._activeHandle, world, canvas._lastDragWorld, MinShapeSize);
                canvas._lastDragWorld = world;
                canvas.UpdateCursor();
                canvas.InvalidateScene();
                e.Handled = true;
                return;
            }

            var previousHover = canvas._hoveredShape;
            canvas._hoveredShape = canvas.FindHitShape(world);
            canvas.UpdateCursor(world);
            if (!ReferenceEquals(previousHover, canvas._hoveredShape))
                canvas.InvalidateScene();
        }

        public void OnPointerReleased(DrawingCanvasControl canvas, PointerReleasedEventArgs e, FlowVector world)
        {
            if (canvas._activeHandle == ShapeHandleKind.None)
                return;

            canvas._activeHandle = ShapeHandleKind.None;
            canvas._lastDragWorld = null;
            e.Pointer.Capture(null);
            canvas.UpdateCursor();
            e.Handled = true;
        }
    }

    private sealed class DrawInteractionHandler : ICanvasInteractionHandler
    {
        public void OnPointerPressed(DrawingCanvasControl canvas, PointerPressedEventArgs e, FlowVector world)
        {
            if (canvas.ActiveTool == DrawingTool.Point)
            {
                canvas.Shapes.Add(new FlowPoint { Pose = DrawingCanvasControl.CreatePose(world.X, world.Y) });
                canvas.InvalidateScene();
                e.Handled = true;
                return;
            }

            canvas._gestureStartWorld = world;
            canvas._previewShape = ShapeInteractionEngine.BuildShape(canvas.ActiveTool, world, world, MinShapeSize);
            e.Pointer.Capture(canvas);
            canvas.UpdateCursor();
            canvas.InvalidateScene();
            e.Handled = true;
        }

        public void OnPointerMoved(DrawingCanvasControl canvas, PointerEventArgs e, FlowVector world)
        {
            if (canvas._gestureStartWorld is null)
            {
                var previousHover = canvas._hoveredShape;
                canvas._hoveredShape = canvas.FindHitShape(world);
                canvas.UpdateCursor(world);
                if (!ReferenceEquals(previousHover, canvas._hoveredShape))
                    canvas.InvalidateScene();
                return;
            }

            canvas._previewShape = ShapeInteractionEngine.BuildShape(canvas.ActiveTool, canvas._gestureStartWorld.Value, world, MinShapeSize);
            canvas.UpdateCursor();
            canvas.InvalidateScene();
            e.Handled = true;
        }

        public void OnPointerReleased(DrawingCanvasControl canvas, PointerReleasedEventArgs e, FlowVector world)
        {
            if (canvas._gestureStartWorld is null)
                return;

            var finalShape = ShapeInteractionEngine.BuildShape(canvas.ActiveTool, canvas._gestureStartWorld.Value, world, MinShapeSize);
            if (finalShape is not null)
                canvas.Shapes.Add(finalShape);

            canvas._gestureStartWorld = null;
            canvas._previewShape = null;
            e.Pointer.Capture(null);
            canvas.UpdateCursor();
            canvas.InvalidateScene();
            e.Handled = true;
        }
    }
}
