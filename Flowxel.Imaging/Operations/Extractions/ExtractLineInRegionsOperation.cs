using System.Diagnostics;
using Flowxel.Core.Geometry.Primitives;
using Flowxel.Core.Geometry.Shapes;
using Flowxel.Graph;
using OpenCvSharp;

namespace Flowxel.Imaging.Operations.Extractions;

public class ExtractLineInRegionsOperation(ResourcePool pool, Graph<IExecutableNode> graph) : Node<Mat, Line>(pool, graph)
{
    protected override Line ExecuteInternal(
        IReadOnlyList<Mat> inputs,
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken ct)
    {
        var totalSw = Stopwatch.StartNew();
        var input = inputs[0];
        var region = (Rectangle)parameters["Region"];

        var maskSw = Stopwatch.StartNew();
        var (mask, roi) = RegionMasking.BuildRectangleMaskAndBoundingBox(input.Size(), region);
        maskSw.Stop();

        Console.WriteLine(
            $"[ExtractLine] start node={Id} input={input.Width}x{input.Height} mask={maskSw.Elapsed.TotalMilliseconds:F2}ms roi={roi.Width}x{roi.Height}@({roi.X},{roi.Y})");

        using (mask)
        {
            if (roi.Width <= 0 || roi.Height <= 0)
            {
                totalSw.Stop();
                Console.WriteLine($"[ExtractLine] end node={Id} empty-roi total={totalSw.Elapsed.TotalMilliseconds:F2}ms");
                return CreateEmptyLine();
            }

            var roiSw = Stopwatch.StartNew();
            using var roiMat = new Mat(input, roi).Clone();
            roiSw.Stop();

            var graySw = Stopwatch.StartNew();
            using var gray = roiMat.Channels() == 1 ? roiMat.Clone() : roiMat.CvtColor(ColorConversionCodes.BGR2GRAY);
            graySw.Stop();

            var cannySw = Stopwatch.StartNew();
            using var edges = new Mat();
            Cv2.Canny(gray, edges, 40, 120, 3, true);
            cannySw.Stop();

            var maskEdgesSw = Stopwatch.StartNew();
            using var interiorMask = mask.Clone();
            if (roi.Width >= 3 && roi.Height >= 3)
                Cv2.Erode(mask, interiorMask, Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3)));
            Cv2.BitwiseAnd(edges, interiorMask, edges);
            maskEdgesSw.Stop();

            var houghSw = Stopwatch.StartNew();
            var minLength = Math.Max(8, Math.Min(roi.Width, roi.Height) / 4);
            var segments = Cv2.HoughLinesP(edges, 1, Math.PI / 180, 20, minLength, 10);
            houghSw.Stop();

            Console.WriteLine(
                $"[ExtractLine] preprocess node={Id} roiClone={roiSw.Elapsed.TotalMilliseconds:F2}ms gray={graySw.Elapsed.TotalMilliseconds:F2}ms canny={cannySw.Elapsed.TotalMilliseconds:F2}ms maskEdges={maskEdgesSw.Elapsed.TotalMilliseconds:F2}ms hough={houghSw.Elapsed.TotalMilliseconds:F2}ms segments={segments.Length}");

            if (segments.Length == 0)
            {
                totalSw.Stop();
                Console.WriteLine($"[ExtractLine] end node={Id} no-segments total={totalSw.Elapsed.TotalMilliseconds:F2}ms");
                return CreateEmptyLine();
            }

            var refined = new List<Line>(segments.Length);
            double refineMs = 0;
            double collectMs = 0;
            long inspectedPixels = 0;
            long edgePixels = 0;
            long inliers = 0;

            foreach (var segment in segments)
            {
                ct.ThrowIfCancellationRequested();

                var segmentSw = Stopwatch.StartNew();
                var refinedLine = RefineSegmentToSubpixelLine(
                    edges,
                    segment,
                    roi,
                    out var collectMsForSegment,
                    out var inspectedPixelsForSegment,
                    out var edgePixelsForSegment,
                    out var inliersForSegment);
                segmentSw.Stop();

                refineMs += segmentSw.Elapsed.TotalMilliseconds;
                collectMs += collectMsForSegment;
                inspectedPixels += inspectedPixelsForSegment;
                edgePixels += edgePixelsForSegment;
                inliers += inliersForSegment;

                if (refinedLine is null)
                    continue;

                if (IsDuplicate(refined, refinedLine))
                    continue;

                refined.Add(refinedLine);
            }

            totalSw.Stop();
            Console.WriteLine(
                $"[ExtractLine] refine node={Id} refine={refineMs:F2}ms collectInliers={collectMs:F2}ms scannedPixels={inspectedPixels} edgePixels={edgePixels} keptInliers={inliers} refined={refined.Count}/{segments.Length}");
            Console.WriteLine($"[ExtractLine] end node={Id} total={totalSw.Elapsed.TotalMilliseconds:F2}ms");

            if (refined.Count == 0)
                return CreateEmptyLine();

            var longest = refined.MaxBy(line => line.Length);
            return longest ?? CreateEmptyLine();
        }
    }

    private static Line CreateEmptyLine()
    {
        return new Line
        {
            Pose = new Pose(new Vector(0, 0), new Vector(1, 0)),
            Length = 0
        };
    }

    private static Line? RefineSegmentToSubpixelLine(
        Mat edges,
        LineSegmentPoint segment,
        Rect roi,
        out double collectMs,
        out int inspectedPixels,
        out int edgePixels,
        out int inlierCount)
    {
        collectMs = 0;
        inspectedPixels = 0;
        edgePixels = 0;
        inlierCount = 0;

        var p1 = new Point2d(segment.P1.X, segment.P1.Y);
        var p2 = new Point2d(segment.P2.X, segment.P2.Y);
        var direction = new Point2d(p2.X - p1.X, p2.Y - p1.Y);
        var length = Math.Sqrt(direction.X * direction.X + direction.Y * direction.Y);
        if (length <= 1e-6)
            return null;

        var dir = new Point2d(direction.X / length, direction.Y / length);

        var collectSw = Stopwatch.StartNew();
        var inliers = CollectInliers(edges, p1, p2, dir, out inspectedPixels, out edgePixels);
        collectSw.Stop();
        collectMs = collectSw.Elapsed.TotalMilliseconds;
        inlierCount = inliers.Count;
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

    private static List<Point2f> CollectInliers(
        Mat edges,
        Point2d p1,
        Point2d p2,
        Point2d direction,
        out int inspectedPixels,
        out int edgePixels)
    {
        var inliers = new List<Point2f>();
        var indexer = edges.GetGenericIndexer<byte>();
        var segmentLength = Math.Sqrt((p2.X - p1.X) * (p2.X - p1.X) + (p2.Y - p1.Y) * (p2.Y - p1.Y));
        var maxProjection = segmentLength + 2;
        const double maxDistance = 1.6;
        inspectedPixels = edges.Width * edges.Height;
        edgePixels = 0;

        for (var y = 0; y < edges.Height; y++)
        {
            for (var x = 0; x < edges.Width; x++)
            {
                if (indexer[y, x] == 0)
                    continue;
                edgePixels++;

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
