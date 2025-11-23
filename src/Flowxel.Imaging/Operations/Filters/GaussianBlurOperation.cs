using OpenCvSharp;

namespace Flowxel.Imaging.Operations.Filters;

public class GaussianBlurOperation : Operation<Mat, Mat>
{
    public override ValueTask<Mat> ExecuteAsync(Mat input, IReadOnlyDictionary<string, object> parameters, CancellationToken ct)
    {

        var ksize = parameters.GetValueOrDefault("KernelSize", 0);
        var sigma = parameters.GetValueOrDefault("Sigma", 0.0);

        var kernelSize = Convert.ToInt32(ksize);
        var sigmaX = Convert.ToDouble(sigma);

        if (kernelSize > 0 && kernelSize % 2 == 0) kernelSize++;

        var output = new Mat();
        Cv2.GaussianBlur(input, output, new Size(kernelSize, kernelSize), sigmaX);
        return ValueTask.FromResult(output);
    }
}