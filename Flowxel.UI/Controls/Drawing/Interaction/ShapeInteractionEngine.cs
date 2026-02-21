using Flowxel.Core.Geometry.Primitives;
using Flowxel.Core.Geometry.Shapes;
using Flowxel.UI.Controls.Drawing.Shapes;
using FlowPoint = Flowxel.Core.Geometry.Shapes.Point;
using FlowRectangle = Flowxel.Core.Geometry.Shapes.Rectangle;
using Line = Flowxel.Core.Geometry.Shapes.Line;

namespace Flowxel.UI.Controls.Drawing;

public static class ShapeInteractionEngine
{
    private const double TwoPi = Math.PI * 2;

    public static Shape? BuildShape(DrawingTool tool, Vector start, Vector end, double minShapeSize)
    {
        var delta = end - start;

        switch (tool)
        {
            case DrawingTool.Line:
            {
                var length = delta.M;
                if (length <= minShapeSize)
                    return null;

                return new Line
                {
                    Pose = CreatePose(start.X, start.Y, delta.Normalize()),
                    Length = length
                };
            }
            case DrawingTool.Rectangle:
            {
                if (!TryBuildAxisAlignedBox(start, end, minShapeSize, out var center, out var width, out var height))
                    return null;

                return new FlowRectangle
                {
                    Pose = CreatePose(center.X, center.Y),
                    Width = width,
                    Height = height
                };
            }
            case DrawingTool.Circle:
            {
                var radius = delta.M;
                if (radius <= minShapeSize)
                    return null;

                return new Circle
                {
                    Pose = CreatePose(start.X, start.Y),
                    Radius = radius
                };
            }
            case DrawingTool.Image:
            {
                if (!TryBuildAxisAlignedBox(start, end, minShapeSize, out var center, out var width, out var height))
                    return null;

                return new ImageShape
                {
                    Pose = CreatePose(center.X, center.Y),
                    Width = width,
                    Height = height
                };
            }
            case DrawingTool.TextBox:
            {
                if (!TryBuildAxisAlignedBox(start, end, minShapeSize, out var center, out var width, out var height))
                    return null;

                return new TextBoxShape
                {
                    Pose = CreatePose(center.X, center.Y),
                    Width = width,
                    Height = height,
                    Text = "Text"
                };
            }
            case DrawingTool.Arrow:
            {
                var length = delta.M;
                if (length <= minShapeSize)
                    return null;

                return new ArrowShape
                {
                    Pose = CreatePose(start.X, start.Y, delta.Normalize()),
                    Length = length,
                    HeadLength = Math.Max(12, length * 0.15)
                };
            }
            case DrawingTool.CenterlineRectangle:
            {
                var length = delta.M;
                if (length <= minShapeSize)
                    return null;

                return new CenterlineRectangleShape
                {
                    Pose = CreatePose(start.X, start.Y, delta.Normalize()),
                    Length = length,
                    Width = Math.Max(24, length * 0.2)
                };
            }
            case DrawingTool.Referential:
            {
                var axisLength = delta.M;
                if (axisLength <= minShapeSize)
                    return null;

                var orientation = delta.Normalize();
                return new ReferentialShape
                {
                    Pose = CreatePose(start.X, start.Y, orientation),
                    XAxisLength = axisLength,
                    YAxisLength = axisLength
                };
            }
            case DrawingTool.Dimension:
            {
                var length = delta.M;
                if (length <= minShapeSize)
                    return null;

                return new DimensionShape
                {
                    Pose = CreatePose(start.X, start.Y, delta.Normalize()),
                    Length = length,
                    Offset = Math.Max(24, length * 0.2),
                    Text = length.ToString("0.##")
                };
            }
            case DrawingTool.AngleDimension:
            {
                var radius = delta.M;
                if (radius <= minShapeSize)
                    return null;

                var sweep = new Vector(1, 0).AngleTo(delta.Normalize());
                if (Math.Abs(sweep) <= 0.05)
                    sweep = Math.PI / 2;

                return new AngleDimensionShape
                {
                    Pose = CreatePose(start.X, start.Y),
                    Radius = radius,
                    StartAngleRad = 0,
                    SweepAngleRad = sweep,
                    Text = $"{Math.Abs(sweep * 180 / Math.PI):0.#}°"
                };
            }
            case DrawingTool.Text:
                return new TextShape
                {
                    Pose = CreatePose(start.X, start.Y),
                    FontSize = Math.Max(12, delta.M * 0.1),
                    Text = "Text"
                };
            case DrawingTool.MultilineText:
            {
                if (!TryBuildAxisAlignedBox(start, end, minShapeSize, out var center, out var width, out _))
                    return null;

                return new MultilineTextShape
                {
                    Pose = CreatePose(center.X, center.Y),
                    Width = width,
                    FontSize = 16,
                    Text = "Line 1\nLine 2"
                };
            }
            case DrawingTool.Icon:
                return new IconShape
                {
                    Pose = CreatePose(start.X, start.Y),
                    Size = Math.Max(16, delta.M),
                    IconKey = "★"
                };
            case DrawingTool.Arc:
            {
                var radius = delta.M;
                if (radius <= minShapeSize)
                    return null;

                var sweep = new Vector(1, 0).AngleTo(delta.Normalize());
                if (Math.Abs(sweep) <= 0.05)
                    sweep = Math.PI / 2;

                return new ArcShape
                {
                    Pose = CreatePose(start.X, start.Y),
                    Radius = radius,
                    StartAngleRad = 0,
                    SweepAngleRad = sweep
                };
            }
            default:
                return null;
        }
    }

    public static bool IsShapePerimeterHit(Shape shape, Vector world, double tolerance, double pointRadius)
    {
        switch (shape)
        {
            case FlowPoint point:
                return Distance(point.Pose.Position, world) <= pointRadius + tolerance;
            case Line line:
                return DistanceToSegment(world, line.StartPoint.Position, line.EndPoint.Position) <= tolerance;
            case FlowRectangle rectangle:
                return IsRectanglePerimeterHit(rectangle.TopLeft.Position, rectangle.TopRight.Position, rectangle.BottomRight.Position, rectangle.BottomLeft.Position, world, tolerance) ||
                       (rectangle.Fill && IsInsideConvexQuad(rectangle.TopLeft.Position, rectangle.TopRight.Position, rectangle.BottomRight.Position, rectangle.BottomLeft.Position, world));
            case Circle circle:
            {
                var radialDistance = Distance(circle.Pose.Position, world);
                return Math.Abs(radialDistance - circle.Radius) <= tolerance || (circle.Fill && radialDistance <= circle.Radius);
            }
            case ImageShape image:
                return IsRectanglePerimeterHit(image.TopLeft, image.TopRight, image.BottomRight, image.BottomLeft, world, tolerance) ||
                       (image.Fill && IsInsideConvexQuad(image.TopLeft, image.TopRight, image.BottomRight, image.BottomLeft, world));
            case TextBoxShape textBox:
                return IsRectanglePerimeterHit(textBox.TopLeft, textBox.TopRight, textBox.BottomRight, textBox.BottomLeft, world, tolerance) ||
                       (textBox.Fill && IsInsideConvexQuad(textBox.TopLeft, textBox.TopRight, textBox.BottomRight, textBox.BottomLeft, world));
            case ArrowShape arrow:
                return IsArrowHit(arrow, world, tolerance);
            case CenterlineRectangleShape centerlineRectangle:
                return IsRectanglePerimeterHit(
                           centerlineRectangle.TopLeft,
                           centerlineRectangle.TopRight,
                           centerlineRectangle.BottomRight,
                           centerlineRectangle.BottomLeft,
                           world,
                           tolerance) ||
                       (centerlineRectangle.Fill && IsInsideConvexQuad(
                           centerlineRectangle.TopLeft,
                           centerlineRectangle.TopRight,
                           centerlineRectangle.BottomRight,
                           centerlineRectangle.BottomLeft,
                           world));
            case ReferentialShape referential:
                return DistanceToSegment(world, referential.Origin, referential.XAxisEnd) <= tolerance ||
                       DistanceToSegment(world, referential.Origin, referential.YAxisEnd) <= tolerance;
            case DimensionShape dimension:
                return IsDimensionHit(dimension, world, tolerance);
            case AngleDimensionShape angleDimension:
                return IsAngleDimensionHit(angleDimension, world, tolerance);
            case TextShape text:
                return Distance(text.Pose.Position, world) <= Math.Max(tolerance * 2, text.FontSize * 0.5);
            case MultilineTextShape multilineText:
                return Distance(multilineText.Pose.Position, world) <= Math.Max(tolerance * 2, multilineText.FontSize);
            case IconShape icon:
                return Distance(icon.Pose.Position, world) <= Math.Max(tolerance * 2, icon.Size * 0.6);
            case ArcShape arc:
                return IsArcHit(arc, world, tolerance);
            default:
                return false;
        }
    }

    public static IReadOnlyList<ShapeHandle> GetHandles(Shape shape)
    {
        switch (shape)
        {
            case FlowPoint point:
                return
                [
                    new ShapeHandle(ShapeHandleKind.PointPosition, point.Pose.Position)
                ];
            case Line line:
                return
                [
                    new ShapeHandle(ShapeHandleKind.LineStart, line.StartPoint.Position),
                    new ShapeHandle(ShapeHandleKind.LineEnd, line.EndPoint.Position),
                    new ShapeHandle(ShapeHandleKind.Move, Midpoint(line.StartPoint.Position, line.EndPoint.Position))
                ];
            case FlowRectangle rectangle:
                return GetBoxHandles(rectangle.TopLeft.Position, rectangle.TopRight.Position, rectangle.BottomRight.Position, rectangle.BottomLeft.Position, rectangle.Pose.Position);
            case Circle circle:
                return
                [
                    new ShapeHandle(ShapeHandleKind.Move, circle.Pose.Position),
                    new ShapeHandle(ShapeHandleKind.CircleRadius, circle.Pose.Position.Translate(new Vector(circle.Radius, 0)))
                ];
            case ImageShape image:
                return GetBoxHandles(image.TopLeft, image.TopRight, image.BottomRight, image.BottomLeft, image.Pose.Position);
            case TextBoxShape textBox:
                return GetBoxHandles(textBox.TopLeft, textBox.TopRight, textBox.BottomRight, textBox.BottomLeft, textBox.Pose.Position);
            case ArrowShape arrow:
                return
                [
                    new ShapeHandle(ShapeHandleKind.LineStart, arrow.StartPoint),
                    new ShapeHandle(ShapeHandleKind.LineEnd, arrow.EndPoint),
                    new ShapeHandle(ShapeHandleKind.Move, Midpoint(arrow.StartPoint, arrow.EndPoint))
                ];
            case CenterlineRectangleShape centerlineRectangle:
                return
                [
                    new ShapeHandle(ShapeHandleKind.LineStart, centerlineRectangle.StartPoint),
                    new ShapeHandle(ShapeHandleKind.LineEnd, centerlineRectangle.EndPoint),
                    new ShapeHandle(ShapeHandleKind.CenterlineWidth, Midpoint(centerlineRectangle.TopLeft, centerlineRectangle.TopRight)),
                    new ShapeHandle(ShapeHandleKind.Move, Midpoint(centerlineRectangle.StartPoint, centerlineRectangle.EndPoint))
                ];
            case ReferentialShape referential:
                return
                [
                    new ShapeHandle(ShapeHandleKind.Move, referential.Origin),
                    new ShapeHandle(ShapeHandleKind.ReferentialXAxis, referential.XAxisEnd),
                    new ShapeHandle(ShapeHandleKind.ReferentialYAxis, referential.YAxisEnd)
                ];
            case DimensionShape dimension:
                return
                [
                    new ShapeHandle(ShapeHandleKind.LineStart, dimension.StartPoint),
                    new ShapeHandle(ShapeHandleKind.LineEnd, dimension.EndPoint),
                    new ShapeHandle(ShapeHandleKind.DimensionOffset, dimension.OffsetMidpoint),
                    new ShapeHandle(ShapeHandleKind.Move, Midpoint(dimension.StartPoint, dimension.EndPoint))
                ];
            case AngleDimensionShape angleDimension:
                return
                [
                    new ShapeHandle(ShapeHandleKind.Move, angleDimension.Center),
                    new ShapeHandle(ShapeHandleKind.AngleDimensionStart, angleDimension.StartPoint),
                    new ShapeHandle(ShapeHandleKind.AngleDimensionEnd, angleDimension.EndPoint),
                    new ShapeHandle(ShapeHandleKind.AngleDimensionRadius, angleDimension.MidPoint)
                ];
            case TextShape text:
                return [new ShapeHandle(ShapeHandleKind.Move, text.Pose.Position)];
            case MultilineTextShape multilineText:
                return [new ShapeHandle(ShapeHandleKind.Move, multilineText.Pose.Position)];
            case IconShape icon:
                return
                [
                    new ShapeHandle(ShapeHandleKind.Move, icon.Pose.Position),
                    new ShapeHandle(ShapeHandleKind.CircleRadius, icon.Pose.Position.Translate(new Vector(icon.Size * 0.5, 0)))
                ];
            case ArcShape arc:
                return
                [
                    new ShapeHandle(ShapeHandleKind.Move, arc.Center),
                    new ShapeHandle(ShapeHandleKind.AngleDimensionStart, arc.StartPoint),
                    new ShapeHandle(ShapeHandleKind.AngleDimensionEnd, arc.EndPoint),
                    new ShapeHandle(ShapeHandleKind.AngleDimensionRadius, arc.MidPoint)
                ];
            default:
                return [];
        }
    }

    public static ShapeHandleKind HitTestHandle(Shape shape, Vector world, double tolerance)
    {
        foreach (var handle in GetHandles(shape))
        {
            if (Distance(handle.Position, world) <= tolerance)
                return handle.Kind;
        }

        return ShapeHandleKind.None;
    }

    public static void ApplyHandleDrag(Shape shape, ShapeHandleKind handle, Vector world, Vector? lastWorld, double minShapeSize)
    {
        switch (shape)
        {
            case FlowPoint point:
                if (handle is ShapeHandleKind.PointPosition or ShapeHandleKind.Move)
                    point.Pose = CreatePose(world.X, world.Y);
                break;
            case Line line:
                ApplyLineHandleDrag(line, handle, world, lastWorld, minShapeSize);
                break;
            case FlowRectangle rectangle:
                ApplyRectangleHandleDrag(rectangle, handle, world, lastWorld, minShapeSize);
                break;
            case Circle circle:
                ApplyCircleHandleDrag(circle, handle, world, lastWorld, minShapeSize);
                break;
            case ImageShape image:
                ApplyImageHandleDrag(image, handle, world, lastWorld, minShapeSize);
                break;
            case TextBoxShape textBox:
                ApplyTextBoxHandleDrag(textBox, handle, world, lastWorld, minShapeSize);
                break;
            case ArrowShape arrow:
                ApplyArrowHandleDrag(arrow, handle, world, lastWorld, minShapeSize);
                break;
            case CenterlineRectangleShape centerlineRectangle:
                ApplyCenterlineRectangleDrag(centerlineRectangle, handle, world, lastWorld, minShapeSize);
                break;
            case ReferentialShape referential:
                ApplyReferentialDrag(referential, handle, world, lastWorld, minShapeSize);
                break;
            case DimensionShape dimension:
                ApplyDimensionDrag(dimension, handle, world, lastWorld, minShapeSize);
                break;
            case AngleDimensionShape angleDimension:
                ApplyAngleDimensionDrag(angleDimension, handle, world, lastWorld, minShapeSize);
                break;
            case TextShape text:
                if ((handle is ShapeHandleKind.Move or ShapeHandleKind.PointPosition) && lastWorld is not null)
                    text.Translate(world - lastWorld.Value);
                break;
            case MultilineTextShape multilineText:
                if ((handle is ShapeHandleKind.Move or ShapeHandleKind.PointPosition) && lastWorld is not null)
                    multilineText.Translate(world - lastWorld.Value);
                break;
            case IconShape icon:
                ApplyIconDrag(icon, handle, world, lastWorld, minShapeSize);
                break;
            case ArcShape arc:
                ApplyArcDrag(arc, handle, world, lastWorld, minShapeSize);
                break;
        }
    }

    private static void ApplyLineHandleDrag(Line line, ShapeHandleKind handle, Vector world, Vector? lastWorld, double minShapeSize)
    {
        var start = line.StartPoint.Position;
        var end = line.EndPoint.Position;

        switch (handle)
        {
            case ShapeHandleKind.Move when lastWorld is not null:
                line.Translate(world - lastWorld.Value);
                return;
            case ShapeHandleKind.LineStart:
                SetLineFromEndpoints(line, world, end, minShapeSize);
                return;
            case ShapeHandleKind.LineEnd:
                SetLineFromEndpoints(line, start, world, minShapeSize);
                return;
        }
    }

    private static void ApplyRectangleHandleDrag(
        FlowRectangle rectangle,
        ShapeHandleKind handle,
        Vector world,
        Vector? lastWorld,
        double minShapeSize)
    {
        if (handle == ShapeHandleKind.Move && lastWorld is not null)
        {
            rectangle.Translate(world - lastWorld.Value);
            return;
        }

        switch (handle)
        {
            case ShapeHandleKind.RectTopLeft:
                ResizeRectangle(rectangle, world, rectangle.BottomRight.Position, minShapeSize);
                return;
            case ShapeHandleKind.RectTopRight:
                ResizeRectangle(rectangle, world, rectangle.BottomLeft.Position, minShapeSize);
                return;
            case ShapeHandleKind.RectBottomRight:
                ResizeRectangle(rectangle, world, rectangle.TopLeft.Position, minShapeSize);
                return;
            case ShapeHandleKind.RectBottomLeft:
                ResizeRectangle(rectangle, world, rectangle.TopRight.Position, minShapeSize);
                return;
        }
    }

    private static void ApplyCircleHandleDrag(Circle circle, ShapeHandleKind handle, Vector world, Vector? lastWorld, double minShapeSize)
    {
        switch (handle)
        {
            case ShapeHandleKind.Move when lastWorld is not null:
                circle.Translate(world - lastWorld.Value);
                return;
            case ShapeHandleKind.CircleRadius:
                circle.Radius = Math.Max(Distance(circle.Pose.Position, world), minShapeSize);
                return;
        }
    }

    private static void ApplyImageHandleDrag(ImageShape image, ShapeHandleKind handle, Vector world, Vector? lastWorld, double minShapeSize)
    {
        if (handle == ShapeHandleKind.Move && lastWorld is not null)
        {
            image.Translate(world - lastWorld.Value);
            return;
        }

        switch (handle)
        {
            case ShapeHandleKind.RectTopLeft:
                ResizeAxisAlignedBox(world, image.BottomRight, minShapeSize, (center, width, height) =>
                {
                    image.Pose = CreatePose(center.X, center.Y);
                    image.Width = width;
                    image.Height = height;
                });
                return;
            case ShapeHandleKind.RectTopRight:
                ResizeAxisAlignedBox(world, image.BottomLeft, minShapeSize, (center, width, height) =>
                {
                    image.Pose = CreatePose(center.X, center.Y);
                    image.Width = width;
                    image.Height = height;
                });
                return;
            case ShapeHandleKind.RectBottomRight:
                ResizeAxisAlignedBox(world, image.TopLeft, minShapeSize, (center, width, height) =>
                {
                    image.Pose = CreatePose(center.X, center.Y);
                    image.Width = width;
                    image.Height = height;
                });
                return;
            case ShapeHandleKind.RectBottomLeft:
                ResizeAxisAlignedBox(world, image.TopRight, minShapeSize, (center, width, height) =>
                {
                    image.Pose = CreatePose(center.X, center.Y);
                    image.Width = width;
                    image.Height = height;
                });
                return;
        }
    }

    private static void ApplyTextBoxHandleDrag(TextBoxShape textBox, ShapeHandleKind handle, Vector world, Vector? lastWorld, double minShapeSize)
    {
        if (handle == ShapeHandleKind.Move && lastWorld is not null)
        {
            textBox.Translate(world - lastWorld.Value);
            return;
        }

        switch (handle)
        {
            case ShapeHandleKind.RectTopLeft:
                ResizeAxisAlignedBox(world, textBox.BottomRight, minShapeSize, (center, width, height) =>
                {
                    textBox.Pose = CreatePose(center.X, center.Y);
                    textBox.Width = width;
                    textBox.Height = height;
                });
                return;
            case ShapeHandleKind.RectTopRight:
                ResizeAxisAlignedBox(world, textBox.BottomLeft, minShapeSize, (center, width, height) =>
                {
                    textBox.Pose = CreatePose(center.X, center.Y);
                    textBox.Width = width;
                    textBox.Height = height;
                });
                return;
            case ShapeHandleKind.RectBottomRight:
                ResizeAxisAlignedBox(world, textBox.TopLeft, minShapeSize, (center, width, height) =>
                {
                    textBox.Pose = CreatePose(center.X, center.Y);
                    textBox.Width = width;
                    textBox.Height = height;
                });
                return;
            case ShapeHandleKind.RectBottomLeft:
                ResizeAxisAlignedBox(world, textBox.TopRight, minShapeSize, (center, width, height) =>
                {
                    textBox.Pose = CreatePose(center.X, center.Y);
                    textBox.Width = width;
                    textBox.Height = height;
                });
                return;
        }
    }

    private static void ApplyArrowHandleDrag(ArrowShape arrow, ShapeHandleKind handle, Vector world, Vector? lastWorld, double minShapeSize)
    {
        var start = arrow.StartPoint;
        var end = arrow.EndPoint;

        switch (handle)
        {
            case ShapeHandleKind.Move when lastWorld is not null:
                arrow.Translate(world - lastWorld.Value);
                return;
            case ShapeHandleKind.LineStart:
                SetArrowFromEndpoints(arrow, world, end, minShapeSize);
                return;
            case ShapeHandleKind.LineEnd:
                SetArrowFromEndpoints(arrow, start, world, minShapeSize);
                return;
        }
    }

    private static void ApplyCenterlineRectangleDrag(CenterlineRectangleShape shape, ShapeHandleKind handle, Vector world, Vector? lastWorld, double minShapeSize)
    {
        switch (handle)
        {
            case ShapeHandleKind.Move when lastWorld is not null:
                shape.Translate(world - lastWorld.Value);
                return;
            case ShapeHandleKind.LineStart:
                SetCenterlineRectangleFromEndpoints(shape, world, shape.EndPoint, minShapeSize);
                return;
            case ShapeHandleKind.LineEnd:
                SetCenterlineRectangleFromEndpoints(shape, shape.StartPoint, world, minShapeSize);
                return;
            case ShapeHandleKind.CenterlineWidth:
            {
                var lineMid = Midpoint(shape.StartPoint, shape.EndPoint);
                var signed = Dot(world - lineMid, shape.Normal);
                shape.Width = Math.Max(Math.Abs(signed) * 2, minShapeSize);
                return;
            }
        }
    }

    private static void ApplyReferentialDrag(ReferentialShape shape, ShapeHandleKind handle, Vector world, Vector? lastWorld, double minShapeSize)
    {
        switch (handle)
        {
            case ShapeHandleKind.Move when lastWorld is not null:
                shape.Translate(world - lastWorld.Value);
                return;
            case ShapeHandleKind.ReferentialXAxis:
            {
                var xDir = shape.Pose.Orientation.Normalize();
                var projection = Math.Abs(Dot(world - shape.Origin, xDir));
                shape.XAxisLength = Math.Max(projection, minShapeSize);
                return;
            }
            case ShapeHandleKind.ReferentialYAxis:
            {
                var yDir = new Vector(-shape.Pose.Orientation.Y, shape.Pose.Orientation.X).Normalize();
                var projection = Math.Abs(Dot(world - shape.Origin, yDir));
                shape.YAxisLength = Math.Max(projection, minShapeSize);
                return;
            }
        }
    }

    private static void ApplyDimensionDrag(DimensionShape shape, ShapeHandleKind handle, Vector world, Vector? lastWorld, double minShapeSize)
    {
        switch (handle)
        {
            case ShapeHandleKind.Move when lastWorld is not null:
                shape.Translate(world - lastWorld.Value);
                return;
            case ShapeHandleKind.LineStart:
                SetDimensionFromEndpoints(shape, world, shape.EndPoint, minShapeSize);
                shape.Text = shape.Length.ToString("0.##");
                return;
            case ShapeHandleKind.LineEnd:
                SetDimensionFromEndpoints(shape, shape.StartPoint, world, minShapeSize);
                shape.Text = shape.Length.ToString("0.##");
                return;
            case ShapeHandleKind.DimensionOffset:
            {
                var mid = Midpoint(shape.StartPoint, shape.EndPoint);
                var signedOffset = Dot(world - mid, shape.Normal);
                shape.Offset = signedOffset;
                return;
            }
        }
    }

    private static void ApplyAngleDimensionDrag(AngleDimensionShape shape, ShapeHandleKind handle, Vector world, Vector? lastWorld, double minShapeSize)
    {
        switch (handle)
        {
            case ShapeHandleKind.Move when lastWorld is not null:
                shape.Translate(world - lastWorld.Value);
                return;
            case ShapeHandleKind.AngleDimensionStart:
            {
                var end = shape.EndAngleRad;
                var newStart = GetLocalAngle(shape.Pose, world);
                shape.StartAngleRad = newStart;
                shape.SweepAngleRad = ClampSweep(end - newStart);
                shape.Text = $"{Math.Abs(shape.SweepAngleRad * 180 / Math.PI):0.#}°";
                return;
            }
            case ShapeHandleKind.AngleDimensionEnd:
            {
                var end = GetLocalAngle(shape.Pose, world);
                shape.SweepAngleRad = ClampSweep(end - shape.StartAngleRad);
                shape.Text = $"{Math.Abs(shape.SweepAngleRad * 180 / Math.PI):0.#}°";
                return;
            }
            case ShapeHandleKind.AngleDimensionRadius:
                shape.Radius = Math.Max(Distance(shape.Center, world), minShapeSize);
                return;
        }
    }

    private static void ApplyIconDrag(IconShape shape, ShapeHandleKind handle, Vector world, Vector? lastWorld, double minShapeSize)
    {
        switch (handle)
        {
            case ShapeHandleKind.Move when lastWorld is not null:
                shape.Translate(world - lastWorld.Value);
                return;
            case ShapeHandleKind.CircleRadius:
                shape.Size = Math.Max(Distance(shape.Pose.Position, world) * 2, minShapeSize);
                return;
        }
    }

    private static void ApplyArcDrag(ArcShape shape, ShapeHandleKind handle, Vector world, Vector? lastWorld, double minShapeSize)
    {
        switch (handle)
        {
            case ShapeHandleKind.Move when lastWorld is not null:
                shape.Translate(world - lastWorld.Value);
                return;
            case ShapeHandleKind.AngleDimensionStart:
            {
                var end = shape.EndAngleRad;
                var newStart = GetLocalAngle(shape.Pose, world);
                shape.StartAngleRad = newStart;
                shape.SweepAngleRad = ClampSweep(end - newStart);
                return;
            }
            case ShapeHandleKind.AngleDimensionEnd:
            {
                var end = GetLocalAngle(shape.Pose, world);
                shape.SweepAngleRad = ClampSweep(end - shape.StartAngleRad);
                return;
            }
            case ShapeHandleKind.AngleDimensionRadius:
                shape.Radius = Math.Max(Distance(shape.Center, world), minShapeSize);
                return;
        }
    }

    private static void SetLineFromEndpoints(Line line, Vector start, Vector end, double minShapeSize)
    {
        var delta = end - start;
        if (delta.M <= minShapeSize)
            return;

        line.Pose = CreatePose(start.X, start.Y, delta.Normalize());
        line.Length = delta.M;
    }

    private static void SetArrowFromEndpoints(ArrowShape arrow, Vector start, Vector end, double minShapeSize)
    {
        var delta = end - start;
        if (delta.M <= minShapeSize)
            return;

        arrow.Pose = CreatePose(start.X, start.Y, delta.Normalize());
        arrow.Length = delta.M;
        arrow.HeadLength = Math.Max(12, arrow.Length * 0.15);
    }

    private static void SetCenterlineRectangleFromEndpoints(CenterlineRectangleShape shape, Vector start, Vector end, double minShapeSize)
    {
        var delta = end - start;
        if (delta.M <= minShapeSize)
            return;

        shape.Pose = CreatePose(start.X, start.Y, delta.Normalize());
        shape.Length = delta.M;
    }

    private static void SetDimensionFromEndpoints(DimensionShape shape, Vector start, Vector end, double minShapeSize)
    {
        var delta = end - start;
        if (delta.M <= minShapeSize)
            return;

        shape.Pose = CreatePose(start.X, start.Y, delta.Normalize());
        shape.Length = delta.M;
    }

    private static void ResizeRectangle(FlowRectangle rectangle, Vector movingCorner, Vector fixedCorner, double minShapeSize)
    {
        var width = Math.Abs(movingCorner.X - fixedCorner.X);
        var height = Math.Abs(movingCorner.Y - fixedCorner.Y);
        if (width <= minShapeSize || height <= minShapeSize)
            return;

        rectangle.Pose = CreatePose(
            (movingCorner.X + fixedCorner.X) * 0.5,
            (movingCorner.Y + fixedCorner.Y) * 0.5);
        rectangle.Width = width;
        rectangle.Height = height;
    }

    private static void ResizeAxisAlignedBox(Vector movingCorner, Vector fixedCorner, double minShapeSize, Action<Vector, double, double> apply)
    {
        if (!TryBuildAxisAlignedBox(movingCorner, fixedCorner, minShapeSize, out var center, out var width, out var height))
            return;

        apply(center, width, height);
    }

    private static IReadOnlyList<ShapeHandle> GetBoxHandles(Vector topLeft, Vector topRight, Vector bottomRight, Vector bottomLeft, Vector center)
        =>
        [
            new ShapeHandle(ShapeHandleKind.RectTopLeft, topLeft),
            new ShapeHandle(ShapeHandleKind.RectTopRight, topRight),
            new ShapeHandle(ShapeHandleKind.RectBottomRight, bottomRight),
            new ShapeHandle(ShapeHandleKind.RectBottomLeft, bottomLeft),
            new ShapeHandle(ShapeHandleKind.Move, center)
        ];

    private static bool TryBuildAxisAlignedBox(Vector a, Vector b, double minShapeSize, out Vector center, out double width, out double height)
    {
        width = Math.Abs(a.X - b.X);
        height = Math.Abs(a.Y - b.Y);
        center = new Vector((a.X + b.X) * 0.5, (a.Y + b.Y) * 0.5);
        return width > minShapeSize && height > minShapeSize;
    }

    private static Pose CreatePose(double x, double y, Vector? orientation = null)
        => orientation is null
            ? Pose.At(x, y)
            : Pose.At(new Vector(x, y), orientation.Value);

    private static bool IsRectanglePerimeterHit(Vector topLeft, Vector topRight, Vector bottomRight, Vector bottomLeft, Vector point, double tolerance)
    {
        return DistanceToSegment(point, topLeft, topRight) <= tolerance ||
               DistanceToSegment(point, topRight, bottomRight) <= tolerance ||
               DistanceToSegment(point, bottomRight, bottomLeft) <= tolerance ||
               DistanceToSegment(point, bottomLeft, topLeft) <= tolerance;
    }

    private static bool IsInsideConvexQuad(Vector topLeft, Vector topRight, Vector bottomRight, Vector bottomLeft, Vector point)
    {
        var d1 = Cross(topRight - topLeft, point - topLeft);
        var d2 = Cross(bottomRight - topRight, point - topRight);
        var d3 = Cross(bottomLeft - bottomRight, point - bottomRight);
        var d4 = Cross(topLeft - bottomLeft, point - bottomLeft);

        var hasNegative = d1 < 0 || d2 < 0 || d3 < 0 || d4 < 0;
        var hasPositive = d1 > 0 || d2 > 0 || d3 > 0 || d4 > 0;
        return !(hasNegative && hasPositive);
    }

    private static bool IsArrowHit(ArrowShape arrow, Vector point, double tolerance)
    {
        return DistanceToSegment(point, arrow.StartPoint, arrow.EndPoint) <= tolerance ||
               DistanceToSegment(point, arrow.EndPoint, arrow.HeadLeftPoint) <= tolerance ||
               DistanceToSegment(point, arrow.EndPoint, arrow.HeadRightPoint) <= tolerance;
    }

    private static bool IsDimensionHit(DimensionShape dimension, Vector point, double tolerance)
    {
        return DistanceToSegment(point, dimension.StartPoint, dimension.OffsetStart) <= tolerance ||
               DistanceToSegment(point, dimension.EndPoint, dimension.OffsetEnd) <= tolerance ||
               DistanceToSegment(point, dimension.OffsetStart, dimension.OffsetEnd) <= tolerance;
    }

    private static bool IsAngleDimensionHit(AngleDimensionShape dimension, Vector point, double tolerance)
    {
        if (DistanceToSegment(point, dimension.Center, dimension.StartPoint) <= tolerance ||
            DistanceToSegment(point, dimension.Center, dimension.EndPoint) <= tolerance)
            return true;

        var radial = point - dimension.Center;
        var radiusDistance = Math.Abs(radial.M - dimension.Radius);
        if (radiusDistance > tolerance || radial.M <= 0.0000001)
            return false;

        var localAngle = dimension.Pose.Orientation.Normalize().AngleTo(radial.Normalize());
        return IsAngleOnSweep(localAngle, dimension.StartAngleRad, dimension.SweepAngleRad);
    }

    private static bool IsArcHit(ArcShape arc, Vector point, double tolerance)
    {
        var radial = point - arc.Center;
        var radiusDistance = Math.Abs(radial.M - arc.Radius);
        if (radiusDistance > tolerance || radial.M <= 0.0000001)
            return false;

        var localAngle = arc.Pose.Orientation.Normalize().AngleTo(radial.Normalize());
        return IsAngleOnSweep(localAngle, arc.StartAngleRad, arc.SweepAngleRad);
    }

    private static bool IsAngleOnSweep(double angle, double start, double sweep)
    {
        if (Math.Abs(sweep) <= 0.0000001)
            return false;

        if (sweep > 0)
        {
            var delta = NormalizePositive(angle - start);
            return delta <= NormalizePositive(sweep);
        }

        var reverseDelta = NormalizePositive(start - angle);
        return reverseDelta <= NormalizePositive(-sweep);
    }

    private static double GetLocalAngle(Pose pose, Vector world)
    {
        var radial = world - pose.Position;
        if (radial.M <= 0.0000001)
            return 0;

        return pose.Orientation.Normalize().AngleTo(radial.Normalize());
    }

    private static double ClampSweep(double sweep)
    {
        var normalized = NormalizeSigned(sweep);
        if (Math.Abs(normalized) < 0.05)
            return normalized >= 0 ? 0.05 : -0.05;

        return normalized;
    }

    private static double NormalizePositive(double angle)
    {
        var value = angle % TwoPi;
        if (value < 0)
            value += TwoPi;

        return value;
    }

    private static double NormalizeSigned(double angle)
    {
        var normalized = NormalizePositive(angle);
        if (normalized > Math.PI)
            normalized -= TwoPi;

        return normalized;
    }

    private static double Dot(Vector a, Vector b)
        => (a.X * b.X) + (a.Y * b.Y);

    private static double Cross(Vector a, Vector b)
        => (a.X * b.Y) - (a.Y * b.X);

    private static double Distance(Vector a, Vector b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    private static Vector Midpoint(Vector a, Vector b)
        => new((a.X + b.X) * 0.5, (a.Y + b.Y) * 0.5);

    private static double DistanceToSegment(Vector point, Vector segStart, Vector segEnd)
    {
        var dx = segEnd.X - segStart.X;
        var dy = segEnd.Y - segStart.Y;
        var segmentLenSq = (dx * dx) + (dy * dy);
        if (segmentLenSq <= 0.0000001)
            return Distance(point, segStart);

        var t = ((point.X - segStart.X) * dx + (point.Y - segStart.Y) * dy) / segmentLenSq;
        t = Math.Clamp(t, 0, 1);
        var projX = segStart.X + (t * dx);
        var projY = segStart.Y + (t * dy);
        var distanceX = point.X - projX;
        var distanceY = point.Y - projY;
        return Math.Sqrt((distanceX * distanceX) + (distanceY * distanceY));
    }
}
