using Flowxel.Graph;
using OpenCvSharp;

namespace Flowxel.Imaging.Operations.Filters;

public class GaussianBlurOperation(ResourcePool pool, Graph<IExecutableNode> graph) : Node<Mat, Mat>(pool, graph)
{
    protected override Mat ExecuteInternal(
        IReadOnlyList<Mat> inputs, 
        IReadOnlyDictionary<string, object> parameters, 
        CancellationToken ct)
    {
        var kernelSize = Convert.ToInt32(parameters["KernelSize"]);
        var sigma = Convert.ToDouble(parameters["Sigma"]);

        if (kernelSize % 2 == 0) kernelSize++;

            var output = new Mat();
            Cv2.GaussianBlur(inputs[0], output, new Size(kernelSize, kernelSize), sigma);
            return output;
    }
}