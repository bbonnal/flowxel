using Flowxel.Core.Geometry.Primitives;
using Flowxel.UI.Controls.Drawing.Shapes;
using Shape = Flowxel.Core.Geometry.Shapes.Shape;

namespace Flowxel.UI.Controls.Drawing;

internal sealed class IconToolShapeFactory : IToolShapeFactory
{
    public DrawingTool Tool => DrawingTool.Icon;

    public Shape? Build(Vector start, Vector end, double minShapeSize)
    {
        var delta = end - start;
        return new IconShape
        {
            Pose = ShapeMath.CreatePose(start.X, start.Y),
            Size = Math.Max(16, delta.M),
            IconKey = "★"
        };
    }
}
