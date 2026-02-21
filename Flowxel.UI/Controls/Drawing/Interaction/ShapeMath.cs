using Flowxel.Core.Geometry.Primitives;
using Flowxel.Core.Geometry.Shapes;
using Flowxel.UI.Controls.Drawing.Shapes;

namespace Flowxel.UI.Controls.Drawing;

internal static class ShapeMath
{
    private const double TwoPi = Math.PI * 2;

    public static Pose CreatePose(double x, double y, Vector? orientation = null)
        => orientation is null
            ? Pose.At(x, y)
            : Pose.At(new Vector(x, y), orientation.Value);

    public static bool TryBuildAxisAlignedBox(Vector a, Vector b, double minShapeSize, out Vector center, out double width, out double height)
    {
        width = Math.Abs(a.X - b.X);
        height = Math.Abs(a.Y - b.Y);
        center = new Vector((a.X + b.X) * 0.5, (a.Y + b.Y) * 0.5);
        return width > minShapeSize && height > minShapeSize;
    }

    public static IReadOnlyList<ShapeHandle> GetBoxHandles(Vector topLeft, Vector topRight, Vector bottomRight, Vector bottomLeft, Vector center)
        =>
        [
            new ShapeHandle(ShapeHandleKind.RectTopLeft, topLeft),
            new ShapeHandle(ShapeHandleKind.RectTopRight, topRight),
            new ShapeHandle(ShapeHandleKind.RectBottomRight, bottomRight),
            new ShapeHandle(ShapeHandleKind.RectBottomLeft, bottomLeft),
            new ShapeHandle(ShapeHandleKind.Move, center)
        ];

    public static bool IsRectanglePerimeterHit(Vector topLeft, Vector topRight, Vector bottomRight, Vector bottomLeft, Vector point, double tolerance)
    {
        return DistanceToSegment(point, topLeft, topRight) <= tolerance ||
               DistanceToSegment(point, topRight, bottomRight) <= tolerance ||
               DistanceToSegment(point, bottomRight, bottomLeft) <= tolerance ||
               DistanceToSegment(point, bottomLeft, topLeft) <= tolerance;
    }

    public static bool IsInsideConvexQuad(Vector topLeft, Vector topRight, Vector bottomRight, Vector bottomLeft, Vector point)
    {
        var d1 = Cross(topRight - topLeft, point - topLeft);
        var d2 = Cross(bottomRight - topRight, point - topRight);
        var d3 = Cross(bottomLeft - bottomRight, point - bottomRight);
        var d4 = Cross(topLeft - bottomLeft, point - bottomLeft);

        var hasNegative = d1 < 0 || d2 < 0 || d3 < 0 || d4 < 0;
        var hasPositive = d1 > 0 || d2 > 0 || d3 > 0 || d4 > 0;
        return !(hasNegative && hasPositive);
    }

    public static bool IsArrowHit(ArrowShape arrow, Vector point, double tolerance)
    {
        return DistanceToSegment(point, arrow.StartPoint, arrow.EndPoint) <= tolerance ||
               DistanceToSegment(point, arrow.EndPoint, arrow.HeadLeftPoint) <= tolerance ||
               DistanceToSegment(point, arrow.EndPoint, arrow.HeadRightPoint) <= tolerance;
    }

    public static bool IsDimensionHit(DimensionShape dimension, Vector point, double tolerance)
    {
        return DistanceToSegment(point, dimension.StartPoint, dimension.OffsetStart) <= tolerance ||
               DistanceToSegment(point, dimension.EndPoint, dimension.OffsetEnd) <= tolerance ||
               DistanceToSegment(point, dimension.OffsetStart, dimension.OffsetEnd) <= tolerance;
    }

    public static bool IsAngleDimensionHit(AngleDimensionShape dimension, Vector point, double tolerance)
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

    public static bool IsArcHit(ArcShape arc, Vector point, double tolerance)
    {
        var radial = point - arc.Center;
        var radiusDistance = Math.Abs(radial.M - arc.Radius);
        if (radiusDistance > tolerance || radial.M <= 0.0000001)
            return false;

        var localAngle = arc.Pose.Orientation.Normalize().AngleTo(radial.Normalize());
        return IsAngleOnSweep(localAngle, arc.StartAngleRad, arc.SweepAngleRad);
    }

    public static double GetLocalAngle(Pose pose, Vector world)
    {
        var radial = world - pose.Position;
        if (radial.M <= 0.0000001)
            return 0;

        return pose.Orientation.Normalize().AngleTo(radial.Normalize());
    }

    public static double ClampSweep(double sweep)
    {
        var normalized = NormalizeSigned(sweep);
        if (Math.Abs(normalized) < 0.05)
            return normalized >= 0 ? 0.05 : -0.05;

        return normalized;
    }

    public static double Dot(Vector a, Vector b)
        => (a.X * b.X) + (a.Y * b.Y);

    public static double Distance(Vector a, Vector b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    public static Vector Midpoint(Vector a, Vector b)
        => new((a.X + b.X) * 0.5, (a.Y + b.Y) * 0.5);

    public static double DistanceToSegment(Vector point, Vector segStart, Vector segEnd)
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

    private static double Cross(Vector a, Vector b)
        => (a.X * b.Y) - (a.Y * b.X);
}
