using Flowxel.Core.Geometry.Primitives;
using FlowPoint = Flowxel.Core.Geometry.Shapes.Point;

namespace Flowxel.UI.Controls.Drawing;

internal sealed class PointShapeBehavior : ShapeBehavior<FlowPoint>
{
    protected override bool IsPerimeterHit(FlowPoint shape, Vector world, double tolerance, double pointRadius)
        => ShapeMath.Distance(shape.Pose.Position, world) <= pointRadius + tolerance;

    protected override IReadOnlyList<ShapeHandle> GetHandles(FlowPoint shape)
        => [new ShapeHandle(ShapeHandleKind.PointPosition, shape.Pose.Position)];

    protected override void ApplyHandleDrag(FlowPoint shape, ShapeHandleKind handle, Vector world, Vector? lastWorld, double minShapeSize)
    {
        if (handle is ShapeHandleKind.PointPosition or ShapeHandleKind.Move)
            shape.Pose = ShapeMath.CreatePose(world.X, world.Y);
    }
}
