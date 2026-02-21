using Flowxel.Core.Geometry.Primitives;
using Flowxel.Core.Geometry.Shapes;

namespace Flowxel.UI.Controls.Drawing;

internal sealed class CircleShapeBehavior : ShapeBehavior<Circle>
{
    protected override bool IsPerimeterHit(Circle shape, Vector world, double tolerance, double pointRadius)
    {
        var radialDistance = ShapeInteractionEngine.Distance(shape.Pose.Position, world);
        return Math.Abs(radialDistance - shape.Radius) <= tolerance || (shape.Fill && radialDistance <= shape.Radius);
    }

    protected override IReadOnlyList<ShapeHandle> GetHandles(Circle shape)
        =>
        [
            new ShapeHandle(ShapeHandleKind.Move, shape.Pose.Position),
            new ShapeHandle(ShapeHandleKind.CircleRadius, shape.Pose.Position.Translate(new Vector(shape.Radius, 0)))
        ];

    protected override void ApplyHandleDrag(Circle shape, ShapeHandleKind handle, Vector world, Vector? lastWorld, double minShapeSize)
        => ShapeInteractionEngine.ApplyCircleHandleDrag(shape, handle, world, lastWorld, minShapeSize);
}
