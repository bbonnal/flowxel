using Avalonia.Input;
using Flowxel.Core.Geometry.Primitives;

namespace Flowxel.UI.Controls.Drawing;

public partial class DrawingCanvasControl
{
    private sealed class BindInteractionHandler : ICanvasInteractionHandler
    {
        public void OnPointerPressed(DrawingCanvasControl canvas, PointerPressedEventArgs e, Vector world)
        {
            var hitShape = canvas.FindHitBindingCandidateShape(world);
            if (hitShape is not null)
            {
                canvas._selectedShape = hitShape;
                if (canvas.ShapeInvokedCommand?.CanExecute(hitShape.Id) == true)
                    canvas.ShapeInvokedCommand.Execute(hitShape.Id);
            }

            canvas.ClearShapeDrag();
            canvas.UpdateCursor(world);
            canvas.InvalidateScene();
            e.Handled = true;
        }

        public void OnPointerMoved(DrawingCanvasControl canvas, PointerEventArgs e, Vector world)
            => canvas.RefreshHover(world, canvas.FindHitBindingCandidateShape);

        public void OnPointerReleased(DrawingCanvasControl canvas, PointerReleasedEventArgs e, Vector world)
        {
        }
    }
}
