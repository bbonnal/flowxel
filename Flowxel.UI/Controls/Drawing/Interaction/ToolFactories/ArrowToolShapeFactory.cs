using Flowxel.Core.Geometry.Primitives;
using Flowxel.UI.Controls.Drawing.Shapes;
using Shape = Flowxel.Core.Geometry.Shapes.Shape;

namespace Flowxel.UI.Controls.Drawing;

internal sealed class ArrowToolShapeFactory : IToolShapeFactory
{
    public DrawingTool Tool => DrawingTool.Arrow;

    public Shape? Build(Vector start, Vector end, double minShapeSize)
    {
        var delta = end - start;
        var length = delta.M;
        if (length <= minShapeSize)
            return null;

        return new ArrowShape
        {
            Pose = ShapeInteractionEngine.CreatePose(start.X, start.Y, delta.Normalize()),
            Length = length,
            HeadLength = Math.Max(12, length * 0.15)
        };
    }
}
