using Flowxel.Core.Geometry.Primitives;
using Flowxel.Core.Geometry.Shapes;

namespace Flowxel.UI.Drawing.Shapes;

public sealed class ArrowShape : Shape
{
    public double Length { get; set; }

    public double HeadLength { get; set; } = 18;

    public double HeadAngleRad { get; set; } = Math.PI / 7;

    public Vector StartPoint => Pose.Position;

    public Vector EndPoint => Pose.Translate(Pose.Orientation.Scale(Length)).Position;

    public Vector HeadLeftPoint
    {
        get
        {
            var back = Pose.Orientation.Normalize().Scale(-HeadLength).Rotate(HeadAngleRad);
            return EndPoint.Translate(back);
        }
    }

    public Vector HeadRightPoint
    {
        get
        {
            var back = Pose.Orientation.Normalize().Scale(-HeadLength).Rotate(-HeadAngleRad);
            return EndPoint.Translate(back);
        }
    }
}
