using Flowxel.Core.Geometry.Primitives;
using Flowxel.UI.Controls.Drawing.Shapes;

namespace Flowxel.UI.Controls.Drawing;

internal sealed class ArcShapeBehavior : ShapeBehavior<ArcShape>
{
    protected override bool IsPerimeterHit(ArcShape shape, Vector world, double tolerance, double pointRadius)
        => ShapeInteractionEngine.IsArcHit(shape, world, tolerance);

    protected override IReadOnlyList<ShapeHandle> GetHandles(ArcShape shape)
        =>
        [
            new ShapeHandle(ShapeHandleKind.Move, shape.Center),
            new ShapeHandle(ShapeHandleKind.AngleDimensionStart, shape.StartPoint),
            new ShapeHandle(ShapeHandleKind.AngleDimensionEnd, shape.EndPoint),
            new ShapeHandle(ShapeHandleKind.AngleDimensionRadius, shape.MidPoint)
        ];

    protected override void ApplyHandleDrag(ArcShape shape, ShapeHandleKind handle, Vector world, Vector? lastWorld, double minShapeSize)
        => ShapeInteractionEngine.ApplyArcDrag(shape, handle, world, lastWorld, minShapeSize);
}
