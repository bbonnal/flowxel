using Flowxel.Core.Geometry.Primitives;
using Flowxel.UI.Controls.Drawing.Shapes;

namespace Flowxel.UI.Controls.Drawing;

internal sealed class IconShapeBehavior : ShapeBehavior<IconShape>
{
    protected override bool IsPerimeterHit(IconShape shape, Vector world, double tolerance, double pointRadius)
        => ShapeMath.Distance(shape.Pose.Position, world) <= Math.Max(tolerance * 2, shape.Size * 0.6);

    protected override IReadOnlyList<ShapeHandle> GetHandles(IconShape shape)
        =>
        [
            new ShapeHandle(ShapeHandleKind.Move, shape.Pose.Position),
            new ShapeHandle(ShapeHandleKind.CircleRadius, shape.Pose.Position.Translate(new Vector(shape.Size * 0.5, 0)))
        ];

    protected override void ApplyHandleDrag(IconShape shape, ShapeHandleKind handle, Vector world, Vector? lastWorld, double minShapeSize)
        => ShapeHandleOps.ApplyIconDrag(shape, handle, world, lastWorld, minShapeSize);
}
