using Avalonia.Input;
using Flowxel.Core.Geometry.Primitives;

namespace Flowxel.UI.Controls.Drawing;

public partial class DrawingCanvasControl
{
    private sealed class SelectInteractionHandler : ICanvasInteractionHandler
    {
        public void OnPointerPressed(DrawingCanvasControl canvas, PointerPressedEventArgs e, Vector world)
        {
            var selectedHandle = ShapeHandleKind.None;
            if (canvas._selectedShape is not null && !canvas.IsComputedShape(canvas._selectedShape))
                selectedHandle = canvas.HitTestHandle(canvas._selectedShape, world);

            if (canvas._selectedShape is not null &&
                !canvas.IsComputedShape(canvas._selectedShape) &&
                selectedHandle != ShapeHandleKind.None)
            {
                canvas.StartShapeDrag(selectedHandle, world, e.Pointer);
                e.Handled = true;
                return;
            }

            var hitShape = canvas.FindHitShape(world);
            canvas._hoveredShape = hitShape;

            if (hitShape is null)
            {
                canvas.ClearSelectionAndDrag();
                e.Handled = true;
                return;
            }

            if (ReferenceEquals(hitShape, canvas._selectedShape) &&
                !canvas.IsComputedShape(hitShape) &&
                e.ClickCount >= 2)
            {
                _ = canvas.ShowShapePropertiesDialogAsync(hitShape);
                canvas.ClearShapeDrag();
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
                canvas.ClearShapeDrag();
                canvas.UpdateCursor();
                canvas.InvalidateScene();
                e.Handled = true;
                return;
            }

            canvas.StartShapeDrag(ShapeHandleKind.Move, world, e.Pointer);
            e.Handled = true;
        }

        public void OnPointerMoved(DrawingCanvasControl canvas, PointerEventArgs e, Vector world)
        {
            if (canvas._selectedShape is not null &&
                !canvas.IsComputedShape(canvas._selectedShape) &&
                canvas._activeHandle != ShapeHandleKind.None)
            {
                ShapeInteractionEngine.ApplyHandleDrag(
                    canvas._selectedShape,
                    canvas._activeHandle,
                    world,
                    canvas._lastDragWorld,
                    MinShapeSize);

                canvas._lastDragWorld = world;
                canvas.UpdateCursor();
                canvas.InvalidateScene();
                e.Handled = true;
                return;
            }

            canvas.RefreshHover(world, canvas.FindHitShape);
        }

        public void OnPointerReleased(DrawingCanvasControl canvas, PointerReleasedEventArgs e, Vector world)
        {
            if (canvas._activeHandle == ShapeHandleKind.None)
                return;

            canvas.ClearShapeDrag(e.Pointer);
            canvas.UpdateCursor();
            e.Handled = true;
        }
    }
}
