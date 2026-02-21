using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Media;

namespace Flowxel.UI.Controls.Drawing;

internal sealed class DebugOverlayDrawingCanvasRenderer(IDrawingCanvasRenderer inner) : IDrawingCanvasRenderer
{
    private static readonly IBrush OverlayTextBrush = Brushes.White;
    private static readonly IBrush OverlayPanelBrush = new SolidColorBrush(Color.FromArgb(185, 12, 18, 26));
    private static readonly IBrush OverlayBorderBrush = Brushes.DodgerBlue;
    private static readonly Typeface OverlayTypeface = new("Consolas");

    public void Render(DrawingCanvasControl canvas, DrawingContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        inner.Render(canvas, context);
        stopwatch.Stop();

        var stats = canvas.LastRenderStats;
        var text = $"render {stopwatch.Elapsed.TotalMilliseconds:0.00} ms | backend {canvas.RenderBackendKind} | drawn {stats.DrawnShapes}/{stats.TotalShapes} (culled {stats.CulledShapes}) | textCache size {canvas.TextLayoutCacheCount} | zoom {canvas.Zoom:0.###} | pan ({canvas.Pan.X:0.#}, {canvas.Pan.Y:0.#})";
        var formatted = new FormattedText(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            OverlayTypeface,
            12,
            OverlayTextBrush);

        var panel = new Rect(8, 8, formatted.Width + 16, formatted.Height + 10);
        context.DrawRectangle(OverlayPanelBrush, new Pen(OverlayBorderBrush, 1), panel);
        context.DrawText(formatted, new Point(panel.X + 8, panel.Y + 5));

        context.DrawRectangle(null, new Pen(OverlayBorderBrush, 1), new Rect(canvas.Bounds.Size));
    }
}
