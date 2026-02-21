using Flowxel.Core.Geometry.Primitives;
using Flowxel.UI.Controls.Drawing.Shapes;

namespace Flowxel.UI.Controls.Drawing;

internal sealed class TextShapeBehavior : ShapeBehavior<TextShape>
{
    protected override bool IsPerimeterHit(TextShape shape, Vector world, double tolerance, double pointRadius)
        => ShapeMath.Distance(shape.Pose.Position, world) <= Math.Max(tolerance * 2, shape.FontSize * 0.5);

    protected override IReadOnlyList<ShapeHandle> GetHandles(TextShape shape)
        => [new ShapeHandle(ShapeHandleKind.Move, shape.Pose.Position)];

    protected override void ApplyHandleDrag(TextShape shape, ShapeHandleKind handle, Vector world, Vector? lastWorld, double minShapeSize)
    {
        if ((handle is ShapeHandleKind.Move or ShapeHandleKind.PointPosition) && lastWorld is not null)
            shape.Translate(world - lastWorld.Value);
    }
}
