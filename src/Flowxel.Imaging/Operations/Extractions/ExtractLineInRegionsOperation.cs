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
            using var edges = new Mat();
            Cv2.Canny(roiMat, edges, 60, 180);

            var minLength = Math.Max(8, Math.Min(roi.Width, roi.Height) / 4);
            var segments = Cv2.HoughLinesP(edges, 1, Math.PI / 180, 25, minLength, 8);
            if (segments.Length == 0)
                return [];

            var lines = new List<Line>(segments.Length);
            foreach (var segment in segments)
            {
                var start = new Vector(segment.P1.X + roi.X, segment.P1.Y + roi.Y);
                var end = new Vector(segment.P2.X + roi.X, segment.P2.Y + roi.Y);
                var delta = end - start;
                if (delta.M <= 1e-6)
                    continue;

                lines.Add(new Line
                {
                    Pose = new Pose(start, delta.Normalize()),
                    Length = delta.M
                });
            }

            return lines.ToArray();
        }
    }
}
