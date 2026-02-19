using Flowxel.Core.Geometry.Primitives;

namespace Flowxel.Core.Geometry.Shapes;

public class Rectangle : Shape
{
    public double Width { get; set; }
    public double Height { get; set; }

    public Pose TopLeft => Pose.Translate(new Vector(-Width / 2, Height / 2));

    public Pose TopRight => Pose.Translate(new Vector(Width / 2, Height / 2));

    public Pose BottomLeft => Pose.Translate(new Vector(-Width / 2, -Height / 2));

    public Pose BottomRight => Pose.Translate(new Vector(Width / 2, -Height / 2));
}