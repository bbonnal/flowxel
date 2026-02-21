using Flowxel.Core.Geometry.Primitives;
using Flowxel.UI.Controls.Drawing.Shapes;
using Shape = Flowxel.Core.Geometry.Shapes.Shape;

namespace Flowxel.UI.Controls.Drawing;

internal sealed class DimensionToolShapeFactory : IToolShapeFactory
{
    public DrawingTool Tool => DrawingTool.Dimension;

    public Shape? Build(Vector start, Vector end, double minShapeSize)
    {
        var delta = end - start;
        var length = delta.M;
        if (length <= minShapeSize)
            return null;

        return new DimensionShape
        {
            Pose = ShapeMath.CreatePose(start.X, start.Y, delta.Normalize()),
            Length = length,
            Offset = Math.Max(24, length * 0.2),
            Text = length.ToString("0.##")
        };
    }
}
