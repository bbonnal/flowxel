using Flowxel.Core.Geometry.Primitives;
using Flowxel.Core.Geometry.Shapes;
using Flowxel.Processing;
using Flowxel.Vision;
using OpenCvSharp;

namespace Flowxel.Vision.OpenCv.Operations.Extractions;

public class ExtractArcInRegionOperation(ResourcePool pool, Graph<IExecutableNode> graph, IVisionBackend? vision = null) : Node<Mat, Arc[]>(pool, graph, vision)
{
    protected override Arc[] ExecuteInternal(
        IReadOnlyList<Mat> inputs,
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken ct)
    {
        var input = inputs[0];
        var region = (Arc)parameters["Region"];

        var (mask, roi) = RegionMasking.BuildMaskAndBoundingBox(input.Size(), region);
        using (mask)
        {
            if (roi.Width <= 0 || roi.Height <= 0)
                return [];

            using var masked = new Mat();
            Cv2.BitwiseAnd(input, input, masked, mask);

            using var roiMat = new Mat(masked, roi).Clone();
            using var blurred = new Mat();
            Cv2.GaussianBlur(roiMat, blurred, new Size(5, 5), 1.5);

            var circles = Cv2.HoughCircles(blurred, HoughModes.Gradient, 1, 15, 90, 20, 5, Math.Max(8, Math.Max(roi.Width, roi.Height)));
            if (circles.Length == 0)
                return [];

            return circles.Select(circle => new Arc
            {
                Pose = new Pose(new Vector(circle.Center.X + roi.X, circle.Center.Y + roi.Y), new Vector(1, 0)),
                Radius = circle.Radius,
                StartAngle = region.StartAngle,
                EndAngle = region.EndAngle,
                LineWeight = region.LineWeight,
                Fill = region.Fill
            }).ToArray();
        }
    }
}
