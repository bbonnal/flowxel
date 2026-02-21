using Avalonia.Media;
using Shape = Flowxel.Core.Geometry.Shapes.Shape;

namespace Flowxel.UI.Controls.Drawing;

internal sealed class DrawingCanvasSceneSnapshot
{
    public static readonly DrawingCanvasSceneSnapshot Empty = new()
    {
        Shapes = [],
    };

    public required IReadOnlyList<DrawingCanvasSceneShape> Shapes { get; init; }
    public DrawingCanvasSceneShape? HoverShape { get; init; }
    public DrawingCanvasSceneShape? SelectedShape { get; init; }
    public Shape? PreviewShape { get; init; }
    public double PreviewThickness { get; init; }
    public bool DrawSelectedHandles { get; init; }
}

internal sealed record DrawingCanvasSceneShape(
    Shape Shape,
    IBrush Stroke,
    double Thickness,
    IReadOnlyList<double>? DashArray,
    bool IsComputed);
