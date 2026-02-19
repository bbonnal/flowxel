using Flowxel.Core.Geometry.Primitives;
using Flowxel.Core.Geometry.Shapes;

namespace Flowxel.UI.Drawing.Shapes;

public sealed class TextBoxShape : Shape
{
    public string Text { get; set; } = "Text";

    public double FontSize { get; set; } = 14;

    public double Width { get; set; }

    public double Height { get; set; }

    public Vector TopLeft => Pose.Translate(new Vector(-Width * 0.5, Height * 0.5)).Position;

    public Vector TopRight => Pose.Translate(new Vector(Width * 0.5, Height * 0.5)).Position;

    public Vector BottomLeft => Pose.Translate(new Vector(-Width * 0.5, -Height * 0.5)).Position;

    public Vector BottomRight => Pose.Translate(new Vector(Width * 0.5, -Height * 0.5)).Position;
}
