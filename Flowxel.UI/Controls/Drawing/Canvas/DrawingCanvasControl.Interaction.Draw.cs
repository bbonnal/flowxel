using Avalonia.Input;
using Flowxel.Core.Geometry.Primitives;
using Flowxel.Core.Geometry.Shapes;

namespace Flowxel.UI.Controls.Drawing;

public partial class DrawingCanvasControl
{
    private sealed class DrawInteractionHandler : ICanvasInteractionHandler
    {
        public void OnPointerPressed(DrawingCanvasControl canvas, PointerPressedEventArgs e, Vector world)
        {
            if (canvas.ActiveTool == DrawingTool.Point)
            {
                canvas.Shapes.Add(new Point { Pose = ShapeMath.CreatePose(world.X, world.Y) });
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

        public void OnPointerMoved(DrawingCanvasControl canvas, PointerEventArgs e, Vector world)
        {
            if (canvas._gestureStartWorld is null)
            {
                canvas.RefreshHover(world, canvas.FindHitShape);
                return;
            }

            canvas._previewShape = ShapeInteractionEngine.BuildShape(
                canvas.ActiveTool,
                canvas._gestureStartWorld.Value,
                world,
                MinShapeSize);
            canvas.UpdateCursor();
            canvas.InvalidateScene();
            e.Handled = true;
        }

        public void OnPointerReleased(DrawingCanvasControl canvas, PointerReleasedEventArgs e, Vector world)
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
