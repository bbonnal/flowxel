using OpenCvSharp;

namespace Flowxel.Imaging.Operations.Transforms;

public class SubtractOperation : CombineOperation<Mat, Mat>
{
    public override Task<Mat> ExecuteAsync(
        Mat[] inputs,
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken ct)
    {
        if (inputs.Length != 2)
            throw new ArgumentException("Subtract requires exactly 2 inputs");

        return Task.Run(() =>
        {
            var output = new Mat();
            Cv2.Subtract(inputs[0], inputs[1], output);
            return output;
        }, ct);
    }
}