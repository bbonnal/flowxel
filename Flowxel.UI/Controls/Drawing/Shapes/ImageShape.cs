using Flowxel.Core.Geometry.Primitives;
using Flowxel.Core.Geometry.Shapes;

namespace Flowxel.UI.Controls.Drawing.Shapes;

public sealed class ImageShape : Shape
{
    public string SourcePath { get; set; } = string.Empty;

    public double Width { get; set; }

    public double Height { get; set; }

    public Vector TopLeft => Pose.Translate(new Vector(-Width * 0.5, Height * 0.5)).Position;

    public Vector TopRight => Pose.Translate(new Vector(Width * 0.5, Height * 0.5)).Position;

    public Vector BottomLeft => Pose.Translate(new Vector(-Width * 0.5, -Height * 0.5)).Position;

    public Vector BottomRight => Pose.Translate(new Vector(Width * 0.5, -Height * 0.5)).Position;
}
