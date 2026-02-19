using Flowxel.Core.Geometry.Primitives;
using Flowxel.Core.Geometry.Shapes;

namespace Flowxel.UI.Drawing.Shapes;

public sealed class DimensionShape : Shape
{
    public double Length { get; set; }

    public double Offset { get; set; } = 24;

    public string Text { get; set; } = string.Empty;

    public Vector StartPoint => Pose.Position;

    public Vector EndPoint => Pose.Translate(Pose.Orientation.Scale(Length)).Position;

    public Vector Normal => new Vector(-Pose.Orientation.Y, Pose.Orientation.X).Normalize();

    public Vector OffsetStart => StartPoint.Translate(Normal.Scale(Offset));

    public Vector OffsetEnd => EndPoint.Translate(Normal.Scale(Offset));

    public Vector OffsetMidpoint => OffsetStart.Translate((OffsetEnd - OffsetStart).Scale(0.5));
}
