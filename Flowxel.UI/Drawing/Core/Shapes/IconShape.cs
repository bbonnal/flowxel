using Flowxel.Core.Geometry.Shapes;

namespace Flowxel.UI.Drawing.Shapes;

public sealed class IconShape : Shape
{
    public string IconKey { get; set; } = "★";

    public double Size { get; set; } = 32;
}
