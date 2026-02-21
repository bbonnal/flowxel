using Avalonia.Media;

namespace Flowxel.UI.Controls.Drawing;

internal sealed class ImmediateDrawingCanvasRenderBackend : IDrawingCanvasRenderBackend
{
    public DrawingCanvasRenderStats Render(DrawingCanvasControl canvas, DrawingContext context, DrawingCanvasSceneSnapshot scene)
    {
        canvas.RenderSceneImmediate(context, scene);
        return new DrawingCanvasRenderStats(scene.Shapes.Count, scene.Shapes.Count);
    }
}
