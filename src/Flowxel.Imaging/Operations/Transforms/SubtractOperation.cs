using OpenCvSharp;

namespace Flowxel.Imaging.Operations.Transforms;

public class SubtractOperation : Operation<Mat, Mat>
{
    public override Mat Execute(
        IReadOnlyList<Mat> inputs,
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken ct)
    {
        var output = new Mat();
        Cv2.Subtract(inputs[0], inputs[1], output);
        return output;
    }
}