using OpenCvSharp;

namespace Flowxel.Imaging.Operations.Filters;

public class GaussianBlurOperation : Operation<Mat, Mat>
{
    public override Task<Mat> ExecuteAsync(
        Mat input,
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken ct)
    {
        var kernelSize = Convert.ToInt32(parameters["KernelSize"]);
        var sigma = Convert.ToDouble(parameters["Sigma"]);

        if (kernelSize % 2 == 0) kernelSize++;

        return Task.Run(() =>
        {
            var output = new Mat();
            Cv2.GaussianBlur(input, output, new Size(kernelSize, kernelSize), sigma);
            return output;
        }, ct);
    }
}