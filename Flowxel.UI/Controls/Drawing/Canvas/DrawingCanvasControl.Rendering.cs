using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Flowxel.Core.Geometry.Shapes;
using Flowxel.UI.Controls;
using Flowxel.UI.Controls.Drawing.Shapes;
using AvaloniaPoint = global::Avalonia.Point;
using AvaloniaVector = global::Avalonia.Vector;
using FlowVector = Flowxel.Core.Geometry.Primitives.Vector;
using FlowRectangle = Flowxel.Core.Geometry.Shapes.Rectangle;
using Line = Flowxel.Core.Geometry.Shapes.Line;
using Point = Flowxel.Core.Geometry.Shapes.Point;
using Shape = Flowxel.Core.Geometry.Shapes.Shape;

namespace Flowxel.UI.Controls.Drawing;

public partial class DrawingCanvasControl
{
    public void ResetView()
    {
        Zoom = 1d;
        Pan = default;
        InvalidateScene();
    }

    public void CenterViewOnOrigin()
    {
        Pan = new AvaloniaVector(Bounds.Width * 0.5, Bounds.Height * 0.5);
        InvalidateScene();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        _renderer.Render(this, context);
    }

    internal void RenderCore(DrawingContext context)
    {
        context.FillRectangle(CanvasBackground, new Rect(Bounds.Size));
        DrawCanvasBoundary(context);
        DrawOriginMarker(context);

        var scene = GetSceneSnapshot();
        _lastRenderStats = _renderBackend.Render(this, context, scene);
    }

    internal void RenderSceneImmediate(DrawingContext context, DrawingCanvasSceneSnapshot scene)
    {
        foreach (var shape in scene.Shapes)
            DrawShape(context, shape.Shape, shape.Stroke, shape.Thickness, shape.DashArray);

        if (scene.HoverShape is not null)
            DrawShape(context, scene.HoverShape.Shape, scene.HoverShape.Stroke, scene.HoverShape.Thickness, scene.HoverShape.DashArray);

        if (scene.SelectedShape is not null)
            DrawShape(context, scene.SelectedShape.Shape, scene.SelectedShape.Stroke, scene.SelectedShape.Thickness, scene.SelectedShape.DashArray);

        if (scene.DrawSelectedHandles && scene.SelectedShape is not null)
            DrawGrabHandles(context, scene.SelectedShape.Shape);

        if (scene.PreviewShape is not null)
            DrawShape(context, scene.PreviewShape, PreviewStroke, scene.PreviewThickness, PreviewDash);
    }

    private void DrawOriginMarker(DrawingContext context)
    {
        var center = WorldToScreen(new FlowVector(0, 0));
        var size = OriginMarkerSize;
        var xPen = new Pen(OriginXAxisBrush, 2);
        var yPen = new Pen(OriginYAxisBrush, 2);

        context.DrawLine(xPen, new AvaloniaPoint(center.X - size, center.Y), new AvaloniaPoint(center.X + size, center.Y));
        context.DrawLine(yPen, new AvaloniaPoint(center.X, center.Y - size), new AvaloniaPoint(center.X, center.Y + size));
    }

    private void DrawCanvasBoundary(DrawingContext context)
    {
        if (!ShowCanvasBoundary || CanvasBoundaryWidth <= 0 || CanvasBoundaryHeight <= 0)
            return;

        var topLeft = WorldToScreen(new FlowVector(0, 0));
        var bottomRight = WorldToScreen(new FlowVector(CanvasBoundaryWidth, CanvasBoundaryHeight));
        var rect = CreateRectFromPoints(topLeft, bottomRight);
        context.DrawRectangle(null, new Pen(CanvasBoundaryStroke, 1.5), rect);
    }

    private void DrawGrabHandles(DrawingContext context, Shape shape)
    {
        var pen = new Pen(HandleStroke, 1.5);
        var half = HandleSize * 0.5;
        foreach (var handle in ShapeInteractionEngine.GetHandles(shape))
        {
            var screen = WorldToScreen(handle.Position);
            var rect = new Rect(screen.X - half, screen.Y - half, HandleSize, HandleSize);
            context.DrawRectangle(HandleFill, pen, rect);
        }
    }

    private void DrawShape(DrawingContext context, Shape shape, IBrush strokeBrush, double thickness, IReadOnlyList<double>? dashArray)
    {
        var fillBrush = shape.Fill ? strokeBrush : null;
        var pen = dashArray is null
            ? new Pen(strokeBrush, thickness)
            : new Pen(strokeBrush, thickness, dashStyle: new DashStyle(dashArray, 0));

        switch (shape)
        {
            case Point point:
            {
                var p = WorldToScreen(point.Pose.Position);
                context.DrawEllipse(strokeBrush, pen, p, PointDisplayRadius, PointDisplayRadius);
                break;
            }
            case Line line:
            {
                var p1 = WorldToScreen(line.StartPoint.Position);
                var p2 = WorldToScreen(line.EndPoint.Position);
                context.DrawLine(pen, p1, p2);
                break;
            }
            case FlowRectangle rectangle:
                DrawClosedPolygon(context, pen, fillBrush,
                    rectangle.TopLeft.Position,
                    rectangle.TopRight.Position,
                    rectangle.BottomRight.Position,
                    rectangle.BottomLeft.Position,
                    rectangle.TopLeft.Position);
                break;
            case Circle circle:
            {
                var center = WorldToScreen(circle.Pose.Position);
                var radius = circle.Radius * Zoom;
                context.DrawEllipse(fillBrush, pen, center, radius, radius);
                break;
            }
            case ImageShape image:
                DrawImageShape(context, image, pen);
                break;
            case TextBoxShape textBox:
                DrawTextBoxShape(context, textBox, pen, strokeBrush);
                break;
            case ArrowShape arrow:
                DrawArrowShape(context, arrow, pen);
                break;
            case CenterlineRectangleShape centerlineRectangle:
                DrawClosedPolygon(context, pen, fillBrush,
                    centerlineRectangle.TopLeft,
                    centerlineRectangle.TopRight,
                    centerlineRectangle.BottomRight,
                    centerlineRectangle.BottomLeft,
                    centerlineRectangle.TopLeft);
                break;
            case ReferentialShape referential:
                DrawReferentialShape(context, referential, pen);
                break;
            case DimensionShape dimension:
                DrawDimensionShape(context, dimension, pen, strokeBrush);
                break;
            case AngleDimensionShape angleDimension:
                DrawAngleDimensionShape(context, angleDimension, pen, strokeBrush);
                break;
            case TextShape text:
                DrawTextShape(context, text, strokeBrush);
                break;
            case MultilineTextShape multilineText:
                DrawMultilineTextShape(context, multilineText, strokeBrush);
                break;
            case IconShape icon:
                DrawIconShape(context, icon, strokeBrush);
                break;
            case ArcShape arc:
                DrawArcShape(context, arc, pen);
                break;
        }
    }

    private void DrawImageShape(DrawingContext context, ImageShape image, Pen pen)
    {
        var fillBrush = image.Fill ? pen.Brush : null;
        DrawClosedPolygon(context, pen, fillBrush, image.TopLeft, image.TopRight, image.BottomRight, image.BottomLeft, image.TopLeft);

        var bitmap = TryGetBitmap(image.SourcePath);
        if (bitmap is null)
        {
            context.DrawLine(pen, WorldToScreen(image.TopLeft), WorldToScreen(image.BottomRight));
            context.DrawLine(pen, WorldToScreen(image.TopRight), WorldToScreen(image.BottomLeft));
            return;
        }

        var topLeft = WorldToScreen(image.TopLeft);
        var bottomRight = WorldToScreen(image.BottomRight);
        var rect = CreateRectFromPoints(topLeft, bottomRight);
        context.DrawImage(bitmap, new Rect(0, 0, bitmap.PixelSize.Width, bitmap.PixelSize.Height), rect);
        context.DrawRectangle(null, pen, rect);
    }

    private void DrawTextBoxShape(DrawingContext context, TextBoxShape textBox, Pen pen, IBrush strokeBrush)
    {
        var fillBrush = textBox.Fill ? pen.Brush : null;
        DrawClosedPolygon(context, pen, fillBrush, textBox.TopLeft, textBox.TopRight, textBox.BottomRight, textBox.BottomLeft, textBox.TopLeft);

        var topLeft = WorldToScreen(textBox.TopLeft);
        var bottomRight = WorldToScreen(textBox.BottomRight);
        var rect = CreateRectFromPoints(topLeft, bottomRight);

        var textZoom = GetTextZoomBucket();
        var fontSize = Math.Max(8, textBox.FontSize * textZoom);
        var formattedText = GetCachedFormattedText(textBox.Text, fontSize, strokeBrush);

        var textPosition = new AvaloniaPoint(rect.X + 6, rect.Y + 4);
        context.DrawText(formattedText, textPosition);
    }

    private void DrawArrowShape(DrawingContext context, ArrowShape arrow, Pen pen)
    {
        var start = WorldToScreen(arrow.StartPoint);
        var end = WorldToScreen(arrow.EndPoint);
        var headLeft = WorldToScreen(arrow.HeadLeftPoint);
        var headRight = WorldToScreen(arrow.HeadRightPoint);

        context.DrawLine(pen, start, end);
        context.DrawLine(pen, end, headLeft);
        context.DrawLine(pen, end, headRight);
    }

    private void DrawReferentialShape(DrawingContext context, ReferentialShape referential, Pen pen)
    {
        var origin = WorldToScreen(referential.Origin);
        var xEnd = WorldToScreen(referential.XAxisEnd);
        var yEnd = WorldToScreen(referential.YAxisEnd);

        context.DrawLine(pen, origin, xEnd);
        context.DrawLine(pen, origin, yEnd);

        DrawArrowHead(context, pen, referential.XAxisEnd, referential.Origin, 14);
        DrawArrowHead(context, pen, referential.YAxisEnd, referential.Origin, 14);
    }

    private void DrawDimensionShape(DrawingContext context, DimensionShape dimension, Pen pen, IBrush strokeBrush)
    {
        var start = WorldToScreen(dimension.StartPoint);
        var end = WorldToScreen(dimension.EndPoint);
        var offsetStart = WorldToScreen(dimension.OffsetStart);
        var offsetEnd = WorldToScreen(dimension.OffsetEnd);

        context.DrawLine(pen, start, offsetStart);
        context.DrawLine(pen, end, offsetEnd);
        context.DrawLine(pen, offsetStart, offsetEnd);

        DrawArrowHead(context, pen, dimension.OffsetStart, dimension.OffsetEnd, 12);
        DrawArrowHead(context, pen, dimension.OffsetEnd, dimension.OffsetStart, 12);

        var mid = WorldToScreen(dimension.OffsetMidpoint);
        var label = string.IsNullOrWhiteSpace(dimension.Text) ? dimension.Length.ToString("0.##") : dimension.Text;
        var formattedText = GetCachedFormattedText(label, Math.Max(10, 12 * GetTextZoomBucket()), strokeBrush);

        context.DrawText(formattedText, new AvaloniaPoint(mid.X + 4, mid.Y - formattedText.Height - 2));
    }

    private void DrawAngleDimensionShape(DrawingContext context, AngleDimensionShape angleDimension, Pen pen, IBrush strokeBrush)
    {
        context.DrawLine(pen, WorldToScreen(angleDimension.Center), WorldToScreen(angleDimension.StartPoint));
        context.DrawLine(pen, WorldToScreen(angleDimension.Center), WorldToScreen(angleDimension.EndPoint));

        DrawArc(context, pen, angleDimension);

        var startTanAnchor = angleDimension.PointOnArc(angleDimension.StartAngleRad + (angleDimension.SweepAngleRad >= 0 ? 0.08 : -0.08));
        var endTanAnchor = angleDimension.PointOnArc(angleDimension.EndAngleRad - (angleDimension.SweepAngleRad >= 0 ? 0.08 : -0.08));
        DrawArrowHead(context, pen, angleDimension.StartPoint, startTanAnchor, 12);
        DrawArrowHead(context, pen, angleDimension.EndPoint, endTanAnchor, 12);

        var mid = WorldToScreen(angleDimension.MidPoint);
        var label = string.IsNullOrWhiteSpace(angleDimension.Text)
            ? $"{Math.Abs(angleDimension.SweepAngleRad * 180 / Math.PI):0.#}°"
            : angleDimension.Text;

        var formattedText = GetCachedFormattedText(label, Math.Max(10, 12 * GetTextZoomBucket()), strokeBrush);

        context.DrawText(formattedText, new AvaloniaPoint(mid.X + 4, mid.Y - formattedText.Height - 2));
    }

    private void DrawTextShape(DrawingContext context, TextShape text, IBrush strokeBrush)
    {
        var formattedText = GetCachedFormattedText(text.Text, Math.Max(8, text.FontSize * GetTextZoomBucket()), strokeBrush);

        var position = WorldToScreen(text.Pose.Position);
        context.DrawText(formattedText, position);
    }

    private void DrawMultilineTextShape(DrawingContext context, MultilineTextShape multilineText, IBrush strokeBrush)
    {
        var textZoom = GetTextZoomBucket();
        var formattedText = GetCachedFormattedText(
            multilineText.Text,
            Math.Max(8, multilineText.FontSize * textZoom),
            strokeBrush,
            Math.Max(8, multilineText.Width * textZoom));

        var position = WorldToScreen(multilineText.Pose.Position);
        context.DrawText(formattedText, position);
    }

    private void DrawIconShape(DrawingContext context, IconShape icon, IBrush strokeBrush)
    {
        var formattedText = GetCachedFormattedText(icon.IconKey, Math.Max(8, icon.Size * GetTextZoomBucket()), strokeBrush);

        var position = WorldToScreen(icon.Pose.Position);
        context.DrawText(formattedText, position);
    }

    private void DrawArcShape(DrawingContext context, ArcShape arc, Pen pen)
    {
        var span = Math.Abs(arc.SweepAngleRad);
        var segmentCount = Math.Clamp((int)(span * 18), 8, 72);
        var previous = arc.PointOnArc(arc.StartAngleRad);
        for (var i = 1; i <= segmentCount; i++)
        {
            var t = (double)i / segmentCount;
            var angle = arc.StartAngleRad + (arc.SweepAngleRad * t);
            var current = arc.PointOnArc(angle);
            context.DrawLine(pen, WorldToScreen(previous), WorldToScreen(current));
            previous = current;
        }
    }

    private void DrawClosedPolygon(DrawingContext context, Pen pen, IBrush? fill, params FlowVector[] points)
    {
        if (fill is not null && points.Length >= 3)
        {
            var geometry = new StreamGeometry();
            using (var gctx = geometry.Open())
            {
                gctx.BeginFigure(WorldToScreen(points[0]), true);
                for (var i = 1; i < points.Length; i++)
                    gctx.LineTo(WorldToScreen(points[i]));
                gctx.EndFigure(true);
            }

            context.DrawGeometry(fill, null, geometry);
        }

        for (var i = 1; i < points.Length; i++)
            context.DrawLine(pen, WorldToScreen(points[i - 1]), WorldToScreen(points[i]));
    }

    private double GetShapeStrokeThickness(Shape shape)
    {
        var lineWeight = shape.LineWeight;
        return lineWeight > 0 ? lineWeight : StrokeThickness;
    }

    private void DrawArrowHead(DrawingContext context, Pen pen, FlowVector tip, FlowVector tailAnchor, double pixelSize)
    {
        var direction = tip - tailAnchor;
        if (direction.M <= 0.0000001)
            return;

        var dir = direction.Normalize();
        var lengthWorld = pixelSize / Math.Max(Zoom, MinZoom);
        var left = tip.Translate(dir.Scale(-lengthWorld).Rotate(Math.PI / 7));
        var right = tip.Translate(dir.Scale(-lengthWorld).Rotate(-Math.PI / 7));

        context.DrawLine(pen, WorldToScreen(tip), WorldToScreen(left));
        context.DrawLine(pen, WorldToScreen(tip), WorldToScreen(right));
    }

    private void DrawArc(DrawingContext context, Pen pen, AngleDimensionShape angleDimension)
    {
        var span = Math.Abs(angleDimension.SweepAngleRad);
        var segmentCount = Math.Clamp((int)(span * 18), 8, 72);
        var previous = angleDimension.PointOnArc(angleDimension.StartAngleRad);
        for (var i = 1; i <= segmentCount; i++)
        {
            var t = (double)i / segmentCount;
            var angle = angleDimension.StartAngleRad + (angleDimension.SweepAngleRad * t);
            var current = angleDimension.PointOnArc(angle);
            context.DrawLine(pen, WorldToScreen(previous), WorldToScreen(current));
            previous = current;
        }
    }

    private Bitmap? TryGetBitmap(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        if (_imageCache.TryGetValue(path, out var cached))
            return cached;

        try
        {
            var bitmap = new Bitmap(path);
            _imageCache[path] = bitmap;
            _invalidImageWarnings.Remove(path);
            return bitmap;
        }
        catch (Exception ex)
        {
            _imageCache[path] = null;

            if (_invalidImageWarnings.Add(path))
                Dispatcher.UIThread.Post(() => _ = ShowImageLoadWarningAsync(path, ex.Message), DispatcherPriority.Background);

            return null;
        }
    }

    private void ClearImageCache()
    {
        foreach (var cachedBitmap in _imageCache.Values)
            cachedBitmap?.Dispose();

        _imageCache.Clear();
    }

    private void ClearTextLayoutCache()
    {
        _textLayoutCache.Clear();
    }

    private Rect CreateRectFromPoints(AvaloniaPoint p1, AvaloniaPoint p2)
        => new(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y), Math.Abs(p2.X - p1.X), Math.Abs(p2.Y - p1.Y));

    private double GetTextZoomBucket()
    {
        var bucketed = Math.Round(Zoom * 8d, MidpointRounding.AwayFromZero) / 8d;
        return Math.Clamp(bucketed, MinZoom, MaxZoom);
    }

    private FormattedText GetCachedFormattedText(string text, double fontSize, IBrush brush, double? maxTextWidth = null)
    {
        var roundedFontSize = Math.Round(fontSize, 2, MidpointRounding.AwayFromZero);
        var roundedMaxWidth = maxTextWidth.HasValue
            ? Math.Round(maxTextWidth.Value, 2, MidpointRounding.AwayFromZero)
            : 0d;
        var key = new TextLayoutCacheKey(
            text,
            roundedFontSize,
            roundedMaxWidth,
            RuntimeHelpers.GetHashCode(brush));

        if (_textLayoutCache.TryGetValue(key, out var cached))
            return cached;

        var formattedText = new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            CanvasTextTypeface,
            roundedFontSize,
            brush);
        if (roundedMaxWidth > 0)
            formattedText.MaxTextWidth = Math.Max(8, roundedMaxWidth);

        if (_textLayoutCache.Count >= MaxTextLayoutCacheEntries)
            _textLayoutCache.Clear();

        _textLayoutCache[key] = formattedText;
        return formattedText;
    }

    private async Task ShowImageLoadWarningAsync(string path, string details)
    {
        if (InfoBarService is null)
            return;

        await InfoBarService.ShowAsync(infoBar =>
        {
            infoBar.Severity = InfoBarSeverity.Warning;
            infoBar.Title = "Image load failed";
            infoBar.Message = $"Could not load image at '{path}'. {details}";
        });
    }

    private CanvasViewportTransform Viewport => new(Zoom, Pan);

    private void SetViewport(CanvasViewportTransform viewport)
    {
        Zoom = viewport.Zoom;
        Pan = viewport.Pan;
    }

    private AvaloniaPoint WorldToScreen(FlowVector world)
        => Viewport.WorldToScreen(world);

    private FlowVector ScreenToWorld(AvaloniaPoint screen)
        => Viewport.ScreenToWorld(screen);
    private readonly record struct TextLayoutCacheKey(
        string Text,
        double FontSize,
        double MaxTextWidth,
        int BrushId);
}
