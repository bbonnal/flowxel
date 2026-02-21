using Flowxel.Core.Geometry.Primitives;
using Flowxel.UI.Controls.Drawing.Shapes;

namespace Flowxel.UI.Controls.Drawing;

internal sealed class ImageShapeBehavior : ShapeBehavior<ImageShape>
{
    protected override bool IsPerimeterHit(ImageShape shape, Vector world, double tolerance, double pointRadius)
    {
        return ShapeMath.IsRectanglePerimeterHit(shape.TopLeft, shape.TopRight, shape.BottomRight, shape.BottomLeft, world, tolerance) ||
               (shape.Fill && ShapeMath.IsInsideConvexQuad(shape.TopLeft, shape.TopRight, shape.BottomRight, shape.BottomLeft, world));
    }

    protected override IReadOnlyList<ShapeHandle> GetHandles(ImageShape shape)
        => ShapeMath.GetBoxHandles(shape.TopLeft, shape.TopRight, shape.BottomRight, shape.BottomLeft, shape.Pose.Position);

    protected override void ApplyHandleDrag(ImageShape shape, ShapeHandleKind handle, Vector world, Vector? lastWorld, double minShapeSize)
        => ShapeHandleOps.ApplyImageHandleDrag(shape, handle, world, lastWorld, minShapeSize);
}
