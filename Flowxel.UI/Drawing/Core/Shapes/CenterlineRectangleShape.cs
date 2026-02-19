using Flowxel.Core.Geometry.Primitives;
using Flowxel.Core.Geometry.Shapes;

namespace Flowxel.UI.Drawing.Shapes;

public sealed class CenterlineRectangleShape : Shape
{
    public double Length { get; set; }

    public double Width { get; set; }

    public Vector StartPoint => Pose.Position;

    public Vector EndPoint => Pose.Translate(Pose.Orientation.Scale(Length)).Position;

    public Vector Normal => new Vector(-Pose.Orientation.Y, Pose.Orientation.X).Normalize();

    public Vector TopLeft => StartPoint.Translate(Normal.Scale(Width * 0.5));

    public Vector TopRight => EndPoint.Translate(Normal.Scale(Width * 0.5));

    public Vector BottomLeft => StartPoint.Translate(Normal.Scale(-Width * 0.5));

    public Vector BottomRight => EndPoint.Translate(Normal.Scale(-Width * 0.5));
}
