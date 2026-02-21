using Flowxel.Core.Geometry.Primitives;
using Flowxel.UI.Controls.Drawing.Shapes;
using Shape = Flowxel.Core.Geometry.Shapes.Shape;

namespace Flowxel.UI.Controls.Drawing;

internal sealed class TextToolShapeFactory : IToolShapeFactory
{
    public DrawingTool Tool => DrawingTool.Text;

    public Shape? Build(Vector start, Vector end, double minShapeSize)
    {
        var delta = end - start;
        return new TextShape
        {
            Pose = ShapeInteractionEngine.CreatePose(start.X, start.Y),
            FontSize = Math.Max(12, delta.M * 0.1),
            Text = "Text"
        };
    }
}
