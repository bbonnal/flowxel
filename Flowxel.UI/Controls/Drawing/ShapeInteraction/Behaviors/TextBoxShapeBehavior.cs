using Flowxel.Core.Geometry.Primitives;
using Flowxel.UI.Controls.Drawing.Shapes;

namespace Flowxel.UI.Controls.Drawing;

internal sealed class TextBoxShapeBehavior : ShapeBehavior<TextBoxShape>
{
    protected override bool IsPerimeterHit(TextBoxShape shape, Vector world, double tolerance, double pointRadius)
    {
        return ShapeMath.IsRectanglePerimeterHit(shape.TopLeft, shape.TopRight, shape.BottomRight, shape.BottomLeft, world, tolerance) ||
               (shape.Fill && ShapeMath.IsInsideConvexQuad(shape.TopLeft, shape.TopRight, shape.BottomRight, shape.BottomLeft, world));
    }

    protected override IReadOnlyList<ShapeHandle> GetHandles(TextBoxShape shape)
        => ShapeMath.GetBoxHandles(shape.TopLeft, shape.TopRight, shape.BottomRight, shape.BottomLeft, shape.Pose.Position);

    protected override void ApplyHandleDrag(TextBoxShape shape, ShapeHandleKind handle, Vector world, Vector? lastWorld, double minShapeSize)
        => ShapeHandleOps.ApplyTextBoxHandleDrag(shape, handle, world, lastWorld, minShapeSize);
}
