using Flowxel.Core.Geometry.Primitives;
using Flowxel.Core.Geometry.Shapes;
using Shape = Flowxel.Core.Geometry.Shapes.Shape;

namespace Flowxel.UI.Controls.Drawing;

internal sealed class CircleToolShapeFactory : IToolShapeFactory
{
    public DrawingTool Tool => DrawingTool.Circle;

    public Shape? Build(Vector start, Vector end, double minShapeSize)
    {
        var delta = end - start;
        var radius = delta.M;
        if (radius <= minShapeSize)
            return null;

        return new Circle
        {
            Pose = ShapeInteractionEngine.CreatePose(start.X, start.Y),
            Radius = radius
        };
    }
}
