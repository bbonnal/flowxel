using Flowxel.Core.Geometry.Primitives;
using Flowxel.UI.Controls.Drawing.Shapes;

namespace Flowxel.UI.Controls.Drawing;

internal sealed class ReferentialShapeBehavior : ShapeBehavior<ReferentialShape>
{
    protected override bool IsPerimeterHit(ReferentialShape shape, Vector world, double tolerance, double pointRadius)
        => ShapeInteractionEngine.DistanceToSegment(world, shape.Origin, shape.XAxisEnd) <= tolerance ||
           ShapeInteractionEngine.DistanceToSegment(world, shape.Origin, shape.YAxisEnd) <= tolerance;

    protected override IReadOnlyList<ShapeHandle> GetHandles(ReferentialShape shape)
        =>
        [
            new ShapeHandle(ShapeHandleKind.Move, shape.Origin),
            new ShapeHandle(ShapeHandleKind.ReferentialXAxis, shape.XAxisEnd),
            new ShapeHandle(ShapeHandleKind.ReferentialYAxis, shape.YAxisEnd)
        ];

    protected override void ApplyHandleDrag(ReferentialShape shape, ShapeHandleKind handle, Vector world, Vector? lastWorld, double minShapeSize)
        => ShapeInteractionEngine.ApplyReferentialDrag(shape, handle, world, lastWorld, minShapeSize);
}
