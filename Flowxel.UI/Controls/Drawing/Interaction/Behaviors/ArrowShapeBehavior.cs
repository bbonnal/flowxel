using Flowxel.Core.Geometry.Primitives;
using Flowxel.UI.Controls.Drawing.Shapes;

namespace Flowxel.UI.Controls.Drawing;

internal sealed class ArrowShapeBehavior : ShapeBehavior<ArrowShape>
{
    protected override bool IsPerimeterHit(ArrowShape shape, Vector world, double tolerance, double pointRadius)
        => ShapeInteractionEngine.IsArrowHit(shape, world, tolerance);

    protected override IReadOnlyList<ShapeHandle> GetHandles(ArrowShape shape)
        =>
        [
            new ShapeHandle(ShapeHandleKind.LineStart, shape.StartPoint),
            new ShapeHandle(ShapeHandleKind.LineEnd, shape.EndPoint),
            new ShapeHandle(ShapeHandleKind.Move, ShapeInteractionEngine.Midpoint(shape.StartPoint, shape.EndPoint))
        ];

    protected override void ApplyHandleDrag(ArrowShape shape, ShapeHandleKind handle, Vector world, Vector? lastWorld, double minShapeSize)
        => ShapeInteractionEngine.ApplyArrowHandleDrag(shape, handle, world, lastWorld, minShapeSize);
}
