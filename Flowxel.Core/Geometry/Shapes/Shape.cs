using Flowxel.Core.Geometry.Primitives;

namespace Flowxel.Core.Geometry.Shapes;

public abstract class Shape
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public Pose Pose { get; set; }

    public double LineWeight { get; set; } = 1.0;

    public bool Fill { get; set; }

    public string Type => GetType().Name;
    
    public void Translate (Vector v)
        => Pose = Pose.Translate(v);
    
    public void Rotate(double angle)
        => Pose = Pose.Rotate(angle);
    
    public void RotateInPlace(double angle)
        => Pose = Pose.RotateInPlace(angle);

}
