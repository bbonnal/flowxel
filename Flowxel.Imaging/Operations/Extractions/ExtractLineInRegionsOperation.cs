using Flowxel.Core.Geometry.Primitives;
using Flowxel.Core.Geometry.Shapes;
using Flowxel.Graph;
using OpenCvSharp;

namespace Flowxel.Imaging.Operations.Extractions;

public class ExtractLineInRegionsOperation(ResourcePool pool, Graph<IExecutableNode> graph) : Node<Mat, Line[]>(pool, graph)
{
    protected override Line[] ExecuteInternal(
        IReadOnlyList<Mat> inputs,
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken ct)
    {
        var input = inputs[0];
        var region = (Rectangle)parameters["Region"];

        var (mask, roi) = RegionMasking.BuildMaskAndBoundingBox(input.Size(), region);
        using (mask)
        {
            if (roi.Width <= 0 || roi.Height <= 0)
                return [];

            using var masked = new Mat();
            Cv2.BitwiseAnd(input, input, masked, mask);

            using var roiMat = new Mat(masked, roi).Clone();
            using var gray = roiMat.Channels() == 1 ? roiMat.Clone() : roiMat.CvtColor(ColorConversionCodes.BGR2GRAY);
            using var edges = new Mat();
            Cv2.Canny(gray, edges, 40, 120, 3, true);

            var minLength = Math.Max(8, Math.Min(roi.Width, roi.Height) / 4);
            var segments = Cv2.HoughLinesP(edges, 1, Math.PI / 180, 20, minLength, 10);
            if (segments.Length == 0)
                return [];

            var refined = new List<Line>(segments.Length);
            foreach (var segment in segments)
            {
                ct.ThrowIfCancellationRequested();

                var refinedLine = RefineSegmentToSubpixelLine(edges, segment, roi);
                if (refinedLine is null)
                    continue;

                if (IsDuplicate(refined, refinedLine))
                    continue;

                refined.Add(refinedLine);
            }

            return refined.ToArray();
        }
    }

    private static Line? RefineSegmentToSubpixelLine(Mat edges, LineSegmentPoint segment, Rect roi)
    {
        var p1 = new Point2d(segment.P1.X, segment.P1.Y);
        var p2 = new Point2d(segment.P2.X, segment.P2.Y);
        var direction = new Point2d(p2.X - p1.X, p2.Y - p1.Y);
        var length = Math.Sqrt(direction.X * direction.X + direction.Y * direction.Y);
        if (length <= 1e-6)
            return null;

        var dir = new Point2d(direction.X / length, direction.Y / length);

        var inliers = CollectInliers(edges, p1, p2, dir);
        if (inliers.Count < 12)
            return null;

        var line = Cv2.FitLine(inliers, DistanceTypes.L2, 0, 0.01, 0.01);
        var fitDir = new Point2d(line.Vx, line.Vy);
        var fitDirNorm = Math.Sqrt(fitDir.X * fitDir.X + fitDir.Y * fitDir.Y);
        if (fitDirNorm <= 1e-6)
            return null;

        fitDir = new Point2d(fitDir.X / fitDirNorm, fitDir.Y / fitDirNorm);
        var fitPoint = new Point2d(line.X1, line.Y1);

        var tMin = double.MaxValue;
        var tMax = double.MinValue;
        foreach (var point in inliers)
        {
            var dx = point.X - fitPoint.X;
            var dy = point.Y - fitPoint.Y;
            var t = dx * fitDir.X + dy * fitDir.Y;
            if (t < tMin) tMin = t;
            if (t > tMax) tMax = t;
        }

        if (tMax - tMin <= 1e-6)
            return null;

        var startLocal = new Point2d(fitPoint.X + fitDir.X * tMin, fitPoint.Y + fitDir.Y * tMin);
        var endLocal = new Point2d(fitPoint.X + fitDir.X * tMax, fitPoint.Y + fitDir.Y * tMax);

        var start = new Vector(startLocal.X + roi.X, startLocal.Y + roi.Y);
        var end = new Vector(endLocal.X + roi.X, endLocal.Y + roi.Y);
        var delta = end - start;
        if (delta.M <= 1e-6)
            return null;

        return new Line
        {
            Pose = new Pose(start, delta.Normalize()),
            Length = delta.M,
        };
    }

    private static List<Point2f> CollectInliers(Mat edges, Point2d p1, Point2d p2, Point2d direction)
    {
        var inliers = new List<Point2f>();
        var indexer = edges.GetGenericIndexer<byte>();
        var segmentLength = Math.Sqrt((p2.X - p1.X) * (p2.X - p1.X) + (p2.Y - p1.Y) * (p2.Y - p1.Y));
        var maxProjection = segmentLength + 2;
        const double maxDistance = 1.6;

        for (var y = 0; y < edges.Height; y++)
        {
            for (var x = 0; x < edges.Width; x++)
            {
                if (indexer[y, x] == 0)
                    continue;

                var px = x + 0.5;
                var py = y + 0.5;
                var vx = px - p1.X;
                var vy = py - p1.Y;
                var projection = vx * direction.X + vy * direction.Y;
                if (projection < -2 || projection > maxProjection)
                    continue;

                var perpendicular = Math.Abs(vx * direction.Y - vy * direction.X);
                if (perpendicular > maxDistance)
                    continue;

                inliers.Add(new Point2f((float)px, (float)py));
            }
        }

        return inliers;
    }

    private static bool IsDuplicate(IEnumerable<Line> existing, Line candidate)
    {
        foreach (var current in existing)
        {
            var angle = Math.Abs(current.Pose.Orientation.AngleBetween(candidate.Pose.Orientation));
            if (angle > Math.PI / 90)
                continue;

            var normal = new Vector(-current.Pose.Orientation.Y, current.Pose.Orientation.X);
            var distance = Math.Abs(Vector.Dot(candidate.Pose.Position - current.Pose.Position, normal));
            if (distance <= 2.0)
                return true;
        }

        return false;
    }
}
