using Flowxel.Core.Geometry.Primitives;
using Flowxel.UI.Controls.Drawing.Shapes;

namespace Flowxel.UI.Controls.Drawing;

internal sealed class AngleDimensionShapeBehavior : ShapeBehavior<AngleDimensionShape>
{
    protected override bool IsPerimeterHit(AngleDimensionShape shape, Vector world, double tolerance, double pointRadius)
        => ShapeInteractionEngine.IsAngleDimensionHit(shape, world, tolerance);

    protected override IReadOnlyList<ShapeHandle> GetHandles(AngleDimensionShape shape)
        =>
        [
            new ShapeHandle(ShapeHandleKind.Move, shape.Center),
            new ShapeHandle(ShapeHandleKind.AngleDimensionStart, shape.StartPoint),
            new ShapeHandle(ShapeHandleKind.AngleDimensionEnd, shape.EndPoint),
            new ShapeHandle(ShapeHandleKind.AngleDimensionRadius, shape.MidPoint)
        ];

    protected override void ApplyHandleDrag(AngleDimensionShape shape, ShapeHandleKind handle, Vector world, Vector? lastWorld, double minShapeSize)
        => ShapeInteractionEngine.ApplyAngleDimensionDrag(shape, handle, world, lastWorld, minShapeSize);
}
