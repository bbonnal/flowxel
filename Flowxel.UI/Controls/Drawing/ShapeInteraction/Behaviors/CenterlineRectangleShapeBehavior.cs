using Flowxel.Core.Geometry.Primitives;
using Flowxel.UI.Controls.Drawing.Shapes;

namespace Flowxel.UI.Controls.Drawing;

internal sealed class CenterlineRectangleShapeBehavior : ShapeBehavior<CenterlineRectangleShape>
{
    protected override bool IsPerimeterHit(CenterlineRectangleShape shape, Vector world, double tolerance, double pointRadius)
    {
        return ShapeMath.IsRectanglePerimeterHit(
                   shape.TopLeft,
                   shape.TopRight,
                   shape.BottomRight,
                   shape.BottomLeft,
                   world,
                   tolerance) ||
               (shape.Fill && ShapeMath.IsInsideConvexQuad(
                   shape.TopLeft,
                   shape.TopRight,
                   shape.BottomRight,
                   shape.BottomLeft,
                   world));
    }

    protected override IReadOnlyList<ShapeHandle> GetHandles(CenterlineRectangleShape shape)
        =>
        [
            new ShapeHandle(ShapeHandleKind.LineStart, shape.StartPoint),
            new ShapeHandle(ShapeHandleKind.LineEnd, shape.EndPoint),
            new ShapeHandle(ShapeHandleKind.CenterlineWidth, ShapeMath.Midpoint(shape.TopLeft, shape.TopRight)),
            new ShapeHandle(ShapeHandleKind.Move, ShapeMath.Midpoint(shape.StartPoint, shape.EndPoint))
        ];

    protected override void ApplyHandleDrag(CenterlineRectangleShape shape, ShapeHandleKind handle, Vector world, Vector? lastWorld, double minShapeSize)
    {
        switch (handle)
        {
            case ShapeHandleKind.Move when lastWorld is not null:
                shape.Translate(world - lastWorld.Value);
                return;
            case ShapeHandleKind.LineStart:
                ShapeHandleOps.SetCenterlineRectangleFromEndpoints(shape, world, shape.EndPoint, minShapeSize);
                return;
            case ShapeHandleKind.LineEnd:
                ShapeHandleOps.SetCenterlineRectangleFromEndpoints(shape, shape.StartPoint, world, minShapeSize);
                return;
            case ShapeHandleKind.CenterlineWidth:
            {
                var lineMid = ShapeMath.Midpoint(shape.StartPoint, shape.EndPoint);
                var signed = ShapeMath.Dot(world - lineMid, shape.Normal);
                shape.Width = Math.Max(Math.Abs(signed) * 2, minShapeSize);
                return;
            }
        }
    }
}
