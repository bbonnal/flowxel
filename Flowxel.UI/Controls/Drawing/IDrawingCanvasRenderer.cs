using Avalonia.Media;

namespace Flowxel.UI.Controls.Drawing;

internal interface IDrawingCanvasRenderer
{
    void Render(DrawingCanvasControl canvas, DrawingContext context);
}
