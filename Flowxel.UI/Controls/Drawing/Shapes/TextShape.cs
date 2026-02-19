using Flowxel.Core.Geometry.Shapes;

namespace Flowxel.UI.Controls.Drawing.Shapes;

public sealed class TextShape : Shape
{
    public string Text { get; set; } = "Text";

    public double FontSize { get; set; } = 20;
}
