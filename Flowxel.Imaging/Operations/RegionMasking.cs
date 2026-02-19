using Flowxel.Core.Geometry.Primitives;
using Flowxel.Core.Geometry.Shapes;
using OpenCvSharp;

namespace Flowxel.Imaging.Operations;

internal static class RegionMasking
{
    public static (Mat Mask, Rect BoundingBox) BuildRectangleMaskAndBoundingBox(Size imageSize, Rectangle rectangle)
    {
        var corners = GetRectangleCorners(rectangle);
        var minX = corners.Min(point => point.X);
        var minY = corners.Min(point => point.Y);
        var maxX = corners.Max(point => point.X);
        var maxY = corners.Max(point => point.Y);

        var roiMinX = Math.Max(0, (int)Math.Floor(minX) - 1);
        var roiMinY = Math.Max(0, (int)Math.Floor(minY) - 1);
        var roiMaxX = Math.Min(imageSize.Width - 1, (int)Math.Ceiling(maxX) + 1);
        var roiMaxY = Math.Min(imageSize.Height - 1, (int)Math.Ceiling(maxY) + 1);

        if (roiMinX > roiMaxX || roiMinY > roiMaxY)
            return (new Mat(), new Rect(0, 0, 0, 0));

        var roiWidth = roiMaxX - roiMinX + 1;
        var roiHeight = roiMaxY - roiMinY + 1;
        var localMask = new Mat(roiHeight, roiWidth, MatType.CV_8UC1, Scalar.All(0));

        var polygon = corners
            .Select(point => new OpenCvSharp.Point(point.X - roiMinX, point.Y - roiMinY))
            .ToArray();

        Cv2.FillConvexPoly(localMask, polygon, Scalar.All(255));
        return (localMask, new Rect(roiMinX, roiMinY, roiWidth, roiHeight));
    }

    public static (Mat Mask, Rect BoundingBox) BuildMaskAndBoundingBox(Size imageSize, Shape region)
    {
        var mask = new Mat(imageSize.Height, imageSize.Width, MatType.CV_8UC1, Scalar.All(0));
        var hasAny = false;
        var minX = imageSize.Width;
        var minY = imageSize.Height;
        var maxX = -1;
        var maxY = -1;
        var indexer = mask.GetGenericIndexer<byte>();

        for (var y = 0; y < imageSize.Height; y++)
        {
            for (var x = 0; x < imageSize.Width; x++)
            {
                var world = new Vector(x + 0.5, y + 0.5);
                if (!Contains(region, world))
                    continue;

                indexer[y, x] = 255;
                hasAny = true;
                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
            }
        }

        if (!hasAny)
            return (mask, new Rect(0, 0, 0, 0));

        return (mask, new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1));
    }

    public static bool Contains(Shape region, Vector worldPoint)
    {
        return region switch
        {
            Rectangle rectangle => ContainsRectangle(rectangle, worldPoint),
            Circle circle => ContainsCircle(circle, worldPoint),
            Arc arc => ContainsArc(arc, worldPoint),
            _ => throw new NotSupportedException($"Unsupported region shape '{region.Type}'.")
        };
    }

    public static bool IsFullArc(Arc arc)
    {
        var span = NormalizeAngle(arc.EndAngle) - NormalizeAngle(arc.StartAngle);
        if (span < 0)
            span += 2 * Math.PI;

        return span >= 2 * Math.PI - 1e-6;
    }

    public static double NormalizeAngle(double angle)
    {
        var normalized = angle % (2 * Math.PI);
        if (normalized < 0)
            normalized += 2 * Math.PI;
        return normalized;
    }

    public static bool IsAngleInArc(double angle, Arc arc)
    {
        if (IsFullArc(arc))
            return true;

        var normalizedAngle = NormalizeAngle(angle);
        var start = NormalizeAngle(arc.StartAngle);
        var end = NormalizeAngle(arc.EndAngle);

        if (start <= end)
            return normalizedAngle >= start && normalizedAngle <= end;

        return normalizedAngle >= start || normalizedAngle <= end;
    }

    private static bool ContainsRectangle(Rectangle rectangle, Vector worldPoint)
    {
        var local = worldPoint.Transform(rectangle.Pose.ToLocal);
        var halfWidth = rectangle.Width * 0.5;
        var halfHeight = rectangle.Height * 0.5;
        return Math.Abs(local.X) <= halfWidth && Math.Abs(local.Y) <= halfHeight;
    }

    private static bool ContainsCircle(Circle circle, Vector worldPoint)
    {
        var local = worldPoint.Transform(circle.Pose.ToLocal);
        return local.M <= circle.Radius;
    }

    private static bool ContainsArc(Arc arc, Vector worldPoint)
    {
        var local = worldPoint.Transform(arc.Pose.ToLocal);
        var radius = local.M;
        if (radius > arc.Radius)
            return false;

        var angle = Math.Atan2(local.Y, local.X);
        return IsAngleInArc(angle, arc);
    }

    private static Vector[] GetRectangleCorners(Rectangle rectangle)
    {
        var center = rectangle.Pose.Position;
        var axisX = rectangle.Pose.Orientation.Normalize();
        var axisY = new Vector(-axisX.Y, axisX.X);
        var halfWidth = rectangle.Width * 0.5;
        var halfHeight = rectangle.Height * 0.5;

        var xOffset = axisX.Scale(halfWidth);
        var yOffset = axisY.Scale(halfHeight);

        return
        [
            center - xOffset - yOffset,
            center + xOffset - yOffset,
            center + xOffset + yOffset,
            center - xOffset + yOffset
        ];
    }
}
