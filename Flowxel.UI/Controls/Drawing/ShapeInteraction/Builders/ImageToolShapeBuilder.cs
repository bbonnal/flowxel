using Flowxel.Core.Geometry.Primitives;
using Flowxel.UI.Controls.Drawing.Shapes;
using Shape = Flowxel.Core.Geometry.Shapes.Shape;

namespace Flowxel.UI.Controls.Drawing;

internal sealed class ImageToolShapeBuilder : IToolShapeBuilder
{
    public DrawingTool Tool => DrawingTool.Image;

    public Shape? Build(Vector start, Vector end, double minShapeSize)
    {
        if (!ShapeMath.TryBuildAxisAlignedBox(start, end, minShapeSize, out var center, out var width, out var height))
            return null;

        return new ImageShape
        {
            Pose = ShapeMath.CreatePose(center.X, center.Y),
            Width = width,
            Height = height
        };
    }
}
