using Flowxel.Core.Geometry.Primitives;
using Flowxel.UI.Controls.Drawing.Shapes;
using Shape = Flowxel.Core.Geometry.Shapes.Shape;

namespace Flowxel.UI.Controls.Drawing;

internal sealed class ArcToolShapeBuilder : IToolShapeBuilder
{
    public DrawingTool Tool => DrawingTool.Arc;

    public Shape? Build(Vector start, Vector end, double minShapeSize)
    {
        var delta = end - start;
        var radius = delta.M;
        if (radius <= minShapeSize)
            return null;

        var sweep = new Vector(1, 0).AngleTo(delta.Normalize());
        if (Math.Abs(sweep) <= 0.05)
            sweep = Math.PI / 2;

        return new ArcShape
        {
            Pose = ShapeMath.CreatePose(start.X, start.Y),
            Radius = radius,
            StartAngleRad = 0,
            SweepAngleRad = sweep
        };
    }
}
