using Flowxel.Core.Geometry.Primitives;

namespace Flowxel.Core.Geometry.Shapes;

public class Line : Shape
{
    
    public double Length { get; set; }


    public Pose StartPoint => Pose;

    public Pose EndPoint => Pose.Translate(Pose.Orientation.Scale(Length));
}