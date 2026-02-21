using Flowxel.Core.Geometry.Primitives;
using Flowxel.UI.Controls.Drawing.Shapes;

namespace Flowxel.UI.Controls.Drawing;

internal sealed class TextBoxShapeBehavior : ShapeBehavior<TextBoxShape>
{
    protected override bool IsPerimeterHit(TextBoxShape shape, Vector world, double tolerance, double pointRadius)
    {
        return ShapeInteractionEngine.IsRectanglePerimeterHit(shape.TopLeft, shape.TopRight, shape.BottomRight, shape.BottomLeft, world, tolerance) ||
               (shape.Fill && ShapeInteractionEngine.IsInsideConvexQuad(shape.TopLeft, shape.TopRight, shape.BottomRight, shape.BottomLeft, world));
    }

    protected override IReadOnlyList<ShapeHandle> GetHandles(TextBoxShape shape)
        => ShapeInteractionEngine.GetBoxHandles(shape.TopLeft, shape.TopRight, shape.BottomRight, shape.BottomLeft, shape.Pose.Position);

    protected override void ApplyHandleDrag(TextBoxShape shape, ShapeHandleKind handle, Vector world, Vector? lastWorld, double minShapeSize)
        => ShapeInteractionEngine.ApplyTextBoxHandleDrag(shape, handle, world, lastWorld, minShapeSize);
}
