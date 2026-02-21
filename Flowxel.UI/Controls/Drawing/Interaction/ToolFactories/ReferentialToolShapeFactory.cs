using Flowxel.Core.Geometry.Primitives;
using Flowxel.UI.Controls.Drawing.Shapes;
using Shape = Flowxel.Core.Geometry.Shapes.Shape;

namespace Flowxel.UI.Controls.Drawing;

internal sealed class ReferentialToolShapeFactory : IToolShapeFactory
{
    public DrawingTool Tool => DrawingTool.Referential;

    public Shape? Build(Vector start, Vector end, double minShapeSize)
    {
        var delta = end - start;
        var axisLength = delta.M;
        if (axisLength <= minShapeSize)
            return null;

        return new ReferentialShape
        {
            Pose = ShapeInteractionEngine.CreatePose(start.X, start.Y, delta.Normalize()),
            XAxisLength = axisLength,
            YAxisLength = axisLength
        };
    }
}
