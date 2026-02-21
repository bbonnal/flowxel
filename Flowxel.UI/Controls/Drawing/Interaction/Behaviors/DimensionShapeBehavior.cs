using Flowxel.Core.Geometry.Primitives;
using Flowxel.UI.Controls.Drawing.Shapes;

namespace Flowxel.UI.Controls.Drawing;

internal sealed class DimensionShapeBehavior : ShapeBehavior<DimensionShape>
{
    protected override bool IsPerimeterHit(DimensionShape shape, Vector world, double tolerance, double pointRadius)
        => ShapeMath.IsDimensionHit(shape, world, tolerance);

    protected override IReadOnlyList<ShapeHandle> GetHandles(DimensionShape shape)
        =>
        [
            new ShapeHandle(ShapeHandleKind.LineStart, shape.StartPoint),
            new ShapeHandle(ShapeHandleKind.LineEnd, shape.EndPoint),
            new ShapeHandle(ShapeHandleKind.DimensionOffset, shape.OffsetMidpoint),
            new ShapeHandle(ShapeHandleKind.Move, ShapeMath.Midpoint(shape.StartPoint, shape.EndPoint))
        ];

    protected override void ApplyHandleDrag(DimensionShape shape, ShapeHandleKind handle, Vector world, Vector? lastWorld, double minShapeSize)
        => ShapeHandleOps.ApplyDimensionDrag(shape, handle, world, lastWorld, minShapeSize);
}
