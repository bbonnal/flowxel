using System.Collections.Generic;
using Flowxel.Core.Geometry.Primitives;
using Shape = Flowxel.Core.Geometry.Shapes.Shape;

namespace Flowxel.UI.Controls.Drawing;

internal interface IShapeBehavior
{
    bool CanHandle(Shape shape);
    bool IsPerimeterHit(Shape shape, Vector world, double tolerance, double pointRadius);
    IReadOnlyList<ShapeHandle> GetHandles(Shape shape);
    void ApplyHandleDrag(Shape shape, ShapeHandleKind handle, Vector world, Vector? lastWorld, double minShapeSize);
}

internal abstract class ShapeBehavior<TShape> : IShapeBehavior where TShape : Shape
{
    public bool CanHandle(Shape shape) => shape is TShape;

    public bool IsPerimeterHit(Shape shape, Vector world, double tolerance, double pointRadius)
        => shape is TShape typed && IsPerimeterHit(typed, world, tolerance, pointRadius);

    public IReadOnlyList<ShapeHandle> GetHandles(Shape shape)
        => shape is TShape typed ? GetHandles(typed) : [];

    public void ApplyHandleDrag(Shape shape, ShapeHandleKind handle, Vector world, Vector? lastWorld, double minShapeSize)
    {
        if (shape is TShape typed)
            ApplyHandleDrag(typed, handle, world, lastWorld, minShapeSize);
    }

    protected abstract bool IsPerimeterHit(TShape shape, Vector world, double tolerance, double pointRadius);
    protected abstract IReadOnlyList<ShapeHandle> GetHandles(TShape shape);
    protected abstract void ApplyHandleDrag(TShape shape, ShapeHandleKind handle, Vector world, Vector? lastWorld, double minShapeSize);
}
