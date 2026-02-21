using Flowxel.Processing;
using Flowxel.Vision;
using OpenCvSharp;

namespace Flowxel.Vision.OpenCv.Operations.Filters;

public class GaussianBlurOperation(ResourcePool pool, Graph<IExecutableNode> graph, IVisionBackend? vision = null) : Node<Mat, Mat>(pool, graph, vision)
{
    protected override Mat ExecuteInternal(
        IReadOnlyList<Mat> inputs, 
        IReadOnlyDictionary<string, object> parameters, 
        CancellationToken ct)
    {
        var kernelSize = Convert.ToInt32(parameters["KernelSize"]);
        var sigma = Convert.ToDouble(parameters["Sigma"]);

        if (kernelSize % 2 == 0) kernelSize++;

            return (Mat)Vision.GaussianBlur(inputs[0], kernelSize, sigma);
    }
}
