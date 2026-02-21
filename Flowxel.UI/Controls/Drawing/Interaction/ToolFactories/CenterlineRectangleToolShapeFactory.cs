using Flowxel.Core.Geometry.Primitives;
using Flowxel.UI.Controls.Drawing.Shapes;
using Shape = Flowxel.Core.Geometry.Shapes.Shape;

namespace Flowxel.UI.Controls.Drawing;

internal sealed class CenterlineRectangleToolShapeFactory : IToolShapeFactory
{
    public DrawingTool Tool => DrawingTool.CenterlineRectangle;

    public Shape? Build(Vector start, Vector end, double minShapeSize)
    {
        var delta = end - start;
        var length = delta.M;
        if (length <= minShapeSize)
            return null;

        return new CenterlineRectangleShape
        {
            Pose = ShapeMath.CreatePose(start.X, start.Y, delta.Normalize()),
            Length = length,
            Width = Math.Max(24, length * 0.2)
        };
    }
}
