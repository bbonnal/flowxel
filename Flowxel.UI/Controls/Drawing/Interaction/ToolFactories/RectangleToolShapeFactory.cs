using Flowxel.Core.Geometry.Primitives;
using FlowRectangle = Flowxel.Core.Geometry.Shapes.Rectangle;
using Shape = Flowxel.Core.Geometry.Shapes.Shape;

namespace Flowxel.UI.Controls.Drawing;

internal sealed class RectangleToolShapeFactory : IToolShapeFactory
{
    public DrawingTool Tool => DrawingTool.Rectangle;

    public Shape? Build(Vector start, Vector end, double minShapeSize)
    {
        if (!ShapeInteractionEngine.TryBuildAxisAlignedBox(start, end, minShapeSize, out var center, out var width, out var height))
            return null;

        return new FlowRectangle
        {
            Pose = ShapeInteractionEngine.CreatePose(center.X, center.Y),
            Width = width,
            Height = height
        };
    }
}
