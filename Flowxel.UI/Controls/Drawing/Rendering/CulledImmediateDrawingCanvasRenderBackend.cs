using Avalonia;
using Flowxel.Core.Geometry.Shapes;
using Flowxel.UI.Controls.Drawing.Shapes;
using FlowPoint = Flowxel.Core.Geometry.Shapes.Point;
using FlowRectangle = Flowxel.Core.Geometry.Shapes.Rectangle;
using FlowVector = Flowxel.Core.Geometry.Primitives.Vector;
using Line = Flowxel.Core.Geometry.Shapes.Line;
using Shape = Flowxel.Core.Geometry.Shapes.Shape;

namespace Flowxel.UI.Controls.Drawing;

internal sealed class CulledImmediateDrawingCanvasRenderBackend : IDrawingCanvasRenderBackend
{
    public DrawingCanvasRenderStats Render(DrawingCanvasControl canvas, Avalonia.Media.DrawingContext context, DrawingCanvasSceneSnapshot scene)
    {
        if (scene.Shapes.Count == 0)
        {
            canvas.RenderSceneImmediate(context, scene);
            return new DrawingCanvasRenderStats(0, 0);
        }

        var viewportWorld = GetViewportWorldRect(canvas);
        var visibleShapes = new List<DrawingCanvasSceneShape>(scene.Shapes.Count);
        foreach (var sceneShape in scene.Shapes)
        {
            if (IsPotentiallyVisible(sceneShape.Shape, viewportWorld))
                visibleShapes.Add(sceneShape);
        }

        var culledScene = new DrawingCanvasSceneSnapshot
        {
            Shapes = visibleShapes,
            HoverShape = scene.HoverShape,
            SelectedShape = scene.SelectedShape,
            PreviewShape = scene.PreviewShape,
            PreviewThickness = scene.PreviewThickness,
            DrawSelectedHandles = scene.DrawSelectedHandles,
        };

        canvas.RenderSceneImmediate(context, culledScene);
        return new DrawingCanvasRenderStats(scene.Shapes.Count, visibleShapes.Count);
    }

    private static Rect GetViewportWorldRect(DrawingCanvasControl canvas)
    {
        var zoom = Math.Max(canvas.Zoom, canvas.MinZoom);
        var pad = Math.Max(1d, 8d / zoom);

        var minX = (0d - canvas.Pan.X) / zoom - pad;
        var minY = (0d - canvas.Pan.Y) / zoom - pad;
        var maxX = (canvas.Bounds.Width - canvas.Pan.X) / zoom + pad;
        var maxY = (canvas.Bounds.Height - canvas.Pan.Y) / zoom + pad;

        return new Rect(minX, minY, Math.Max(0d, maxX - minX), Math.Max(0d, maxY - minY));
    }

    private static bool IsPotentiallyVisible(Shape shape, Rect viewport)
        => shape switch
        {
            FlowPoint point => ContainsPoint(viewport, point.Pose.Position.X, point.Pose.Position.Y),
            Line line => IntersectsAabb(line.StartPoint.Position, line.EndPoint.Position, viewport),
            FlowRectangle rectangle => IntersectsAabb(
                rectangle.TopLeft.Position,
                rectangle.TopRight.Position,
                rectangle.BottomRight.Position,
                rectangle.BottomLeft.Position,
                viewport),
            Circle circle => IntersectsAabb(
                circle.Pose.Position.X - circle.Radius,
                circle.Pose.Position.Y - circle.Radius,
                circle.Pose.Position.X + circle.Radius,
                circle.Pose.Position.Y + circle.Radius,
                viewport),
            CenterlineRectangleShape rectangle => IntersectsAabb(
                rectangle.TopLeft,
                rectangle.TopRight,
                rectangle.BottomRight,
                rectangle.BottomLeft,
                viewport),
            ImageShape image => IntersectsAabb(image.TopLeft, image.TopRight, image.BottomRight, image.BottomLeft, viewport),
            TextBoxShape textBox => IntersectsAabb(textBox.TopLeft, textBox.TopRight, textBox.BottomRight, textBox.BottomLeft, viewport),
            ArrowShape arrow => IntersectsAabb(arrow.StartPoint, arrow.EndPoint, arrow.HeadLeftPoint, arrow.HeadRightPoint, viewport),
            ReferentialShape referential => IntersectsAabb(referential.Origin, referential.XAxisEnd, referential.YAxisEnd, viewport),
            DimensionShape dimension => IntersectsAabb(dimension.StartPoint, dimension.EndPoint, dimension.OffsetStart, dimension.OffsetEnd, viewport),
            AngleDimensionShape angleDimension => IntersectsAabb(
                angleDimension.Center.X - angleDimension.Radius,
                angleDimension.Center.Y - angleDimension.Radius,
                angleDimension.Center.X + angleDimension.Radius,
                angleDimension.Center.Y + angleDimension.Radius,
                viewport),
            ArcShape arc => IntersectsAabb(
                arc.Pose.Position.X - arc.Radius,
                arc.Pose.Position.Y - arc.Radius,
                arc.Pose.Position.X + arc.Radius,
                arc.Pose.Position.Y + arc.Radius,
                viewport),
            TextShape text => ContainsPoint(viewport, text.Pose.Position.X, text.Pose.Position.Y),
            MultilineTextShape text => ContainsPoint(viewport, text.Pose.Position.X, text.Pose.Position.Y),
            IconShape icon => ContainsPoint(viewport, icon.Pose.Position.X, icon.Pose.Position.Y),
            _ => true,
        };

    private static bool ContainsPoint(Rect rect, double x, double y)
        => rect.Contains(new Avalonia.Point(x, y));

    private static bool IntersectsAabb(FlowVector start, FlowVector end, Rect viewport)
        => IntersectsAabb(
            Math.Min(start.X, end.X),
            Math.Min(start.Y, end.Y),
            Math.Max(start.X, end.X),
            Math.Max(start.Y, end.Y),
            viewport);

    private static bool IntersectsAabb(FlowVector p1, FlowVector p2, FlowVector p3, Rect viewport)
        => IntersectsAabb(p1, p2, p3, p1, viewport);

    private static bool IntersectsAabb(FlowVector p1, FlowVector p2, FlowVector p3, FlowVector p4, Rect viewport)
    {
        var minX = Math.Min(Math.Min(p1.X, p2.X), Math.Min(p3.X, p4.X));
        var minY = Math.Min(Math.Min(p1.Y, p2.Y), Math.Min(p3.Y, p4.Y));
        var maxX = Math.Max(Math.Max(p1.X, p2.X), Math.Max(p3.X, p4.X));
        var maxY = Math.Max(Math.Max(p1.Y, p2.Y), Math.Max(p3.Y, p4.Y));
        return IntersectsAabb(minX, minY, maxX, maxY, viewport);
    }

    private static bool IntersectsAabb(double minX, double minY, double maxX, double maxY, Rect viewport)
    {
        var viewportMaxX = viewport.X + viewport.Width;
        var viewportMaxY = viewport.Y + viewport.Height;
        return !(maxX < viewport.X || maxY < viewport.Y || minX > viewportMaxX || minY > viewportMaxY);
    }
}
