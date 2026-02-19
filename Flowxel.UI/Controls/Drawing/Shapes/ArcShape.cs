using Flowxel.Core.Geometry.Primitives;
using Flowxel.Core.Geometry.Shapes;

namespace Flowxel.UI.Controls.Drawing.Shapes;

public sealed class ArcShape : Shape
{
    public double Radius { get; set; } = 40;

    public double StartAngleRad { get; set; }

    public double SweepAngleRad { get; set; } = Math.PI / 2;

    public Vector Center => Pose.Position;

    public double EndAngleRad => StartAngleRad + SweepAngleRad;

    public Vector StartPoint => PointOnArc(StartAngleRad);

    public Vector EndPoint => PointOnArc(EndAngleRad);

    public Vector MidPoint => PointOnArc(StartAngleRad + (SweepAngleRad * 0.5));

    public Vector PointOnArc(double localAngleRad)
    {
        var radial = Pose.Orientation.Normalize().Rotate(localAngleRad);
        return Center.Translate(radial.Scale(Radius));
    }
}
