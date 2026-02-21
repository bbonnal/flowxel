using Flowxel.Core.Geometry.Primitives;
using Flowxel.UI.Controls.Drawing.Shapes;

namespace Flowxel.UI.Controls.Drawing;

internal sealed class MultilineTextShapeBehavior : ShapeBehavior<MultilineTextShape>
{
    protected override bool IsPerimeterHit(MultilineTextShape shape, Vector world, double tolerance, double pointRadius)
        => ShapeMath.Distance(shape.Pose.Position, world) <= Math.Max(tolerance * 2, shape.FontSize);

    protected override IReadOnlyList<ShapeHandle> GetHandles(MultilineTextShape shape)
        => [new ShapeHandle(ShapeHandleKind.Move, shape.Pose.Position)];

    protected override void ApplyHandleDrag(MultilineTextShape shape, ShapeHandleKind handle, Vector world, Vector? lastWorld, double minShapeSize)
    {
        if ((handle is ShapeHandleKind.Move or ShapeHandleKind.PointPosition) && lastWorld is not null)
            shape.Translate(world - lastWorld.Value);
    }
}
