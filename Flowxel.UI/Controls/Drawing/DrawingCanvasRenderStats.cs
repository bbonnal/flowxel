namespace Flowxel.UI.Controls.Drawing;

internal readonly record struct DrawingCanvasRenderStats(int TotalShapes, int DrawnShapes)
{
    public int CulledShapes => Math.Max(0, TotalShapes - DrawnShapes);
}
