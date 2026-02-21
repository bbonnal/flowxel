using Flowxel.Core.Geometry.Primitives;
using Line = Flowxel.Core.Geometry.Shapes.Line;

namespace Flowxel.UI.Controls.Drawing;

internal sealed class LineShapeBehavior : ShapeBehavior<Line>
{
    protected override bool IsPerimeterHit(Line shape, Vector world, double tolerance, double pointRadius)
        => ShapeMath.DistanceToSegment(world, shape.StartPoint.Position, shape.EndPoint.Position) <= tolerance;

    protected override IReadOnlyList<ShapeHandle> GetHandles(Line shape)
        =>
        [
            new ShapeHandle(ShapeHandleKind.LineStart, shape.StartPoint.Position),
            new ShapeHandle(ShapeHandleKind.LineEnd, shape.EndPoint.Position),
            new ShapeHandle(ShapeHandleKind.Move, ShapeMath.Midpoint(shape.StartPoint.Position, shape.EndPoint.Position))
        ];

    protected override void ApplyHandleDrag(Line shape, ShapeHandleKind handle, Vector world, Vector? lastWorld, double minShapeSize)
    {
        var start = shape.StartPoint.Position;
        var end = shape.EndPoint.Position;

        switch (handle)
        {
            case ShapeHandleKind.Move when lastWorld is not null:
                shape.Translate(world - lastWorld.Value);
                return;
            case ShapeHandleKind.LineStart:
                ShapeHandleOps.SetLineFromEndpoints(shape, world, end, minShapeSize);
                return;
            case ShapeHandleKind.LineEnd:
                ShapeHandleOps.SetLineFromEndpoints(shape, start, world, minShapeSize);
                return;
        }
    }
}
