using Flowxel.Core.Geometry.Primitives;
using Flowxel.UI.Controls.Drawing.Shapes;
using Shape = Flowxel.Core.Geometry.Shapes.Shape;

namespace Flowxel.UI.Controls.Drawing;

internal sealed class ImageToolShapeFactory : IToolShapeFactory
{
    public DrawingTool Tool => DrawingTool.Image;

    public Shape? Build(Vector start, Vector end, double minShapeSize)
    {
        if (!ShapeInteractionEngine.TryBuildAxisAlignedBox(start, end, minShapeSize, out var center, out var width, out var height))
            return null;

        return new ImageShape
        {
            Pose = ShapeInteractionEngine.CreatePose(center.X, center.Y),
            Width = width,
            Height = height
        };
    }
}
