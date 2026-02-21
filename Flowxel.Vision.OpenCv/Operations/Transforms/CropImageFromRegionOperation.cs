using Flowxel.Core.Geometry.Shapes;
using Flowxel.Processing;
using Flowxel.Vision;
using OpenCvSharp;

namespace Flowxel.Vision.OpenCv.Operations.Transforms;

public class CropImageFromRegionOperation(ResourcePool pool, Graph<IExecutableNode> graph, IVisionBackend? vision = null) : Node<Mat, Mat>(pool, graph, vision)
{
    protected override Mat ExecuteInternal(
        IReadOnlyList<Mat> inputs,
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken ct)
    {
        var input = inputs[0];
        var region = (Shape)parameters["Region"];

        var (mask, roi) = RegionMasking.BuildMaskAndBoundingBox(input.Size(), region);
        using (mask)
        {
            if (roi.Width <= 0 || roi.Height <= 0)
                return new Mat();

            using var masked = new Mat();
            Cv2.BitwiseAnd(input, input, masked, mask);
            return new Mat(masked, roi).Clone();
        }
    }
}
