using Flowxel.Core.Geometry.Primitives;
using Flowxel.Core.Geometry.Shapes;

namespace Flowxel.UI.Controls.Drawing;

public static class ShapeInteractionEngine
{
    public static Shape? BuildShape(DrawingTool tool, Vector start, Vector end, double minShapeSize)
    {
        if (!ToolShapeBuilderRegistry.TryGet(tool, out var factory))
            return null;

        return factory.Build(start, end, minShapeSize);
    }

    public static bool IsShapePerimeterHit(Shape shape, Vector world, double tolerance, double pointRadius)
    {
        if (!ShapeBehaviorRegistry.TryGet(shape, out var behavior))
            return false;

        return behavior.IsPerimeterHit(shape, world, tolerance, pointRadius);
    }

    public static IReadOnlyList<ShapeHandle> GetHandles(Shape shape)
    {
        if (!ShapeBehaviorRegistry.TryGet(shape, out var behavior))
            return [];

        return behavior.GetHandles(shape);
    }

    public static ShapeHandleKind HitTestHandle(Shape shape, Vector world, double tolerance)
    {
        foreach (var handle in GetHandles(shape))
        {
            if (ShapeMath.Distance(handle.Position, world) <= tolerance)
                return handle.Kind;
        }

        return ShapeHandleKind.None;
    }

    public static void ApplyHandleDrag(Shape shape, ShapeHandleKind handle, Vector world, Vector? lastWorld, double minShapeSize)
    {
        if (!ShapeBehaviorRegistry.TryGet(shape, out var behavior))
            return;

        behavior.ApplyHandleDrag(shape, handle, world, lastWorld, minShapeSize);
    }
}
