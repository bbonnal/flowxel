using OpenCvSharp;

namespace Flowxel.Imaging.Operations.Transforms;

public class SubtractOperation : Operation<Mat[], Mat>
{
    public override ValueTask<Mat> ExecuteAsync(Mat[] inputs, IReadOnlyDictionary<string, object> parameters, CancellationToken ct)
    {
        if (inputs.Length != 2)
            throw new ArgumentException("Subtract operation requires exactly 2 input images");

        if (inputs.Any(m => m.Empty()))
            throw new ArgumentException("Input images cannot be empty");

        var first = inputs[0];
        var second = inputs[1];

        // Check dimensions match
        if (first.Size() != second.Size())
            throw new InvalidOperationException("Input images must have the same dimensions");

        if (first.Channels() != second.Channels())
            throw new InvalidOperationException("Input images must have the same number of channels");

        var result = new Mat();
        
        // Subtract second from first: result = first - second
        Cv2.Subtract(first, second, result);

        return ValueTask.FromResult(result);
    }
}