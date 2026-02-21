using Flowxel.Core.Geometry.Shapes;
using Flowxel.Processing;
using Flowxel.Vision;
using OpenCvSharp;

namespace Flowxel.Vision.OpenCv.Operations.Extractions;

public class ExtractContourInRegionOperation(ResourcePool pool, Graph<IExecutableNode> graph, IVisionBackend? vision = null) : Node<Mat, OpenCvSharp.Point[][]>(pool, graph, vision)
{
    protected override OpenCvSharp.Point[][] ExecuteInternal(
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
            using var threshold = new Mat();
            Cv2.Threshold(roiMat, threshold, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);

            var contours = Cv2.FindContoursAsArray(
                threshold,
                RetrievalModes.External,
                ContourApproximationModes.ApproxSimple);

            if (contours.Length == 0)
                return [];

            return contours
                .Select(contour => contour.Select(point => new OpenCvSharp.Point(point.X + roi.X, point.Y + roi.Y)).ToArray())
                .ToArray();
        }
    }
}
