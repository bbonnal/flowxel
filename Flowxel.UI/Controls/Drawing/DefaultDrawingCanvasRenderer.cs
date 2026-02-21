using Avalonia.Media;

namespace Flowxel.UI.Controls.Drawing;

internal sealed class DefaultDrawingCanvasRenderer : IDrawingCanvasRenderer
{
    public void Render(DrawingCanvasControl canvas, DrawingContext context)
        => canvas.RenderCore(context);
}
