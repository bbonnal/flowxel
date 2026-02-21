using Shape = Flowxel.Core.Geometry.Shapes.Shape;

namespace Flowxel.UI.Controls.Drawing;

internal static class ShapeBehaviorRegistry
{
    private static readonly IShapeBehavior[] Behaviors =
    [
        new PointShapeBehavior(),
        new LineShapeBehavior(),
        new RectangleShapeBehavior(),
        new CircleShapeBehavior(),
        new ImageShapeBehavior(),
        new TextBoxShapeBehavior(),
        new ArrowShapeBehavior(),
        new CenterlineRectangleShapeBehavior(),
        new ReferentialShapeBehavior(),
        new DimensionShapeBehavior(),
        new AngleDimensionShapeBehavior(),
        new TextShapeBehavior(),
        new MultilineTextShapeBehavior(),
        new IconShapeBehavior(),
        new ArcShapeBehavior(),
    ];

    public static bool TryGet(Shape shape, out IShapeBehavior behavior)
    {
        foreach (var candidate in Behaviors)
        {
            if (candidate.CanHandle(shape))
            {
                behavior = candidate;
                return true;
            }
        }

        behavior = null!;
        return false;
    }
}
