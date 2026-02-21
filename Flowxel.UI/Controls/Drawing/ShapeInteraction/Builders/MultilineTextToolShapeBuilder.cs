using Flowxel.Core.Geometry.Primitives;
using Flowxel.UI.Controls.Drawing.Shapes;
using Shape = Flowxel.Core.Geometry.Shapes.Shape;

namespace Flowxel.UI.Controls.Drawing;

internal sealed class MultilineTextToolShapeBuilder : IToolShapeBuilder
{
    public DrawingTool Tool => DrawingTool.MultilineText;

    public Shape? Build(Vector start, Vector end, double minShapeSize)
    {
        if (!ShapeMath.TryBuildAxisAlignedBox(start, end, minShapeSize, out var center, out var width, out _))
            return null;

        return new MultilineTextShape
        {
            Pose = ShapeMath.CreatePose(center.X, center.Y),
            Width = width,
            FontSize = 16,
            Text = "Line 1\\nLine 2"
        };
    }
}
