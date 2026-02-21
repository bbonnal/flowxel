using Avalonia.Media;

namespace Flowxel.UI.Controls.Drawing;

internal interface IDrawingCanvasRenderBackend
{
    DrawingCanvasRenderStats Render(DrawingCanvasControl canvas, DrawingContext context, DrawingCanvasSceneSnapshot scene);
}
