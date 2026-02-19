using Flowxel.Core.Geometry.Primitives;
using Flowxel.Core.Geometry.Shapes;

namespace Flowxel.UI.Controls.Drawing.Shapes;

public sealed class ReferentialShape : Shape
{
    public double XAxisLength { get; set; } = 80;

    public double YAxisLength { get; set; } = 80;

    public Vector Origin => Pose.Position;

    public Vector XAxisEnd => Pose.Translate(Pose.Orientation.Scale(XAxisLength)).Position;

    public Vector YAxisEnd
    {
        get
        {
            var yDir = new Vector(-Pose.Orientation.Y, Pose.Orientation.X).Normalize();
            return Pose.Translate(yDir.Scale(YAxisLength)).Position;
        }
    }
}
