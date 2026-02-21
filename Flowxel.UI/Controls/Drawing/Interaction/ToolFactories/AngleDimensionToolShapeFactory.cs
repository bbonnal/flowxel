using Flowxel.Core.Geometry.Primitives;
using Flowxel.UI.Controls.Drawing.Shapes;
using Shape = Flowxel.Core.Geometry.Shapes.Shape;

namespace Flowxel.UI.Controls.Drawing;

internal sealed class AngleDimensionToolShapeFactory : IToolShapeFactory
{
    public DrawingTool Tool => DrawingTool.AngleDimension;

    public Shape? Build(Vector start, Vector end, double minShapeSize)
    {
        var delta = end - start;
        var radius = delta.M;
        if (radius <= minShapeSize)
            return null;

        var sweep = new Vector(1, 0).AngleTo(delta.Normalize());
        if (Math.Abs(sweep) <= 0.05)
            sweep = Math.PI / 2;

        return new AngleDimensionShape
        {
            Pose = ShapeMath.CreatePose(start.X, start.Y),
            Radius = radius,
            StartAngleRad = 0,
            SweepAngleRad = sweep,
            Text = $"{Math.Abs(sweep * 180 / Math.PI):0.#}°"
        };
    }
}
