using Flowxel.Core.Geometry.Primitives;
using FlowRectangle = Flowxel.Core.Geometry.Shapes.Rectangle;

namespace Flowxel.UI.Controls.Drawing;

internal sealed class RectangleShapeBehavior : ShapeBehavior<FlowRectangle>
{
    protected override bool IsPerimeterHit(FlowRectangle shape, Vector world, double tolerance, double pointRadius)
    {
        return ShapeMath.IsRectanglePerimeterHit(
                   shape.TopLeft.Position,
                   shape.TopRight.Position,
                   shape.BottomRight.Position,
                   shape.BottomLeft.Position,
                   world,
                   tolerance) ||
               (shape.Fill && ShapeMath.IsInsideConvexQuad(
                   shape.TopLeft.Position,
                   shape.TopRight.Position,
                   shape.BottomRight.Position,
                   shape.BottomLeft.Position,
                   world));
    }

    protected override IReadOnlyList<ShapeHandle> GetHandles(FlowRectangle shape)
        => ShapeMath.GetBoxHandles(
            shape.TopLeft.Position,
            shape.TopRight.Position,
            shape.BottomRight.Position,
            shape.BottomLeft.Position,
            shape.Pose.Position);

    protected override void ApplyHandleDrag(FlowRectangle shape, ShapeHandleKind handle, Vector world, Vector? lastWorld, double minShapeSize)
    {
        if (handle == ShapeHandleKind.Move && lastWorld is not null)
        {
            shape.Translate(world - lastWorld.Value);
            return;
        }

        switch (handle)
        {
            case ShapeHandleKind.RectTopLeft:
                ShapeHandleOps.ResizeRectangle(shape, world, shape.BottomRight.Position, minShapeSize);
                return;
            case ShapeHandleKind.RectTopRight:
                ShapeHandleOps.ResizeRectangle(shape, world, shape.BottomLeft.Position, minShapeSize);
                return;
            case ShapeHandleKind.RectBottomRight:
                ShapeHandleOps.ResizeRectangle(shape, world, shape.TopLeft.Position, minShapeSize);
                return;
            case ShapeHandleKind.RectBottomLeft:
                ShapeHandleOps.ResizeRectangle(shape, world, shape.TopRight.Position, minShapeSize);
                return;
        }
    }
}
