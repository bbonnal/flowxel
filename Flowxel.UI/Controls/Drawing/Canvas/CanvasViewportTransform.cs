using Avalonia;
using AvaloniaPoint = global::Avalonia.Point;
using AvaloniaVector = global::Avalonia.Vector;
using FlowVector = Flowxel.Core.Geometry.Primitives.Vector;

namespace Flowxel.UI.Controls.Drawing;

internal readonly record struct CanvasViewportTransform(double Zoom, AvaloniaVector Pan)
{
    public AvaloniaPoint WorldToScreen(FlowVector world)
        => new((world.X * Zoom) + Pan.X, (world.Y * Zoom) + Pan.Y);

    public FlowVector ScreenToWorld(AvaloniaPoint screen)
        => new((screen.X - Pan.X) / Zoom, (screen.Y - Pan.Y) / Zoom);

    public CanvasViewportTransform ZoomAroundScreen(AvaloniaPoint screenPoint, double zoomFactor, double minZoom, double maxZoom)
    {
        var worldBefore = ScreenToWorld(screenPoint);
        var nextZoom = Math.Clamp(Zoom * zoomFactor, minZoom, maxZoom);
        var nextPan = new AvaloniaVector(
            screenPoint.X - (worldBefore.X * nextZoom),
            screenPoint.Y - (worldBefore.Y * nextZoom));

        return new CanvasViewportTransform(nextZoom, nextPan);
    }
}
