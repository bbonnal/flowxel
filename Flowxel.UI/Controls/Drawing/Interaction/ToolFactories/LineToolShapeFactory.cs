using Flowxel.Core.Geometry.Primitives;
using Line = Flowxel.Core.Geometry.Shapes.Line;
using Shape = Flowxel.Core.Geometry.Shapes.Shape;

namespace Flowxel.UI.Controls.Drawing;

internal sealed class LineToolShapeFactory : IToolShapeFactory
{
    public DrawingTool Tool => DrawingTool.Line;

    public Shape? Build(Vector start, Vector end, double minShapeSize)
    {
        var delta = end - start;
        var length = delta.M;
        if (length <= minShapeSize)
            return null;

        return new Line
        {
            Pose = ShapeInteractionEngine.CreatePose(start.X, start.Y, delta.Normalize()),
            Length = length
        };
    }
}
