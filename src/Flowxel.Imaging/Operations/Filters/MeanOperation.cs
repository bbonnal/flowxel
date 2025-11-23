using OpenCvSharp;

namespace Flowxel.Imaging.Operations.Filters;

public class MeanOperation : Operation<Mat[], Mat>
{

    public override ValueTask<Mat> ExecuteAsync(Mat[] inputs, IReadOnlyDictionary<string, object> parameters, CancellationToken ct)
    {
        if (inputs.Length == 0 || inputs.Any(m => m.Empty()))
            throw new ArgumentException("Invalid input images");

        var first = inputs[0];
        var channels = first.Channels();
        var size = first.Size();

        // Accumulator in double precision
        using var accumulator = new Mat(size, MatType.CV_64FC(channels));
        accumulator.SetTo(0);

        foreach (var mat in inputs)
        {
            // Convert each input to 64F
            using var converted = new Mat();
            mat.ConvertTo(converted, MatType.CV_64FC(channels));

            Cv2.Add(accumulator, converted, accumulator);
        }

        // Compute average
        using var avg = new Mat();
        Cv2.Divide(accumulator, Scalar.All(inputs.Length), avg);

        // Convert back to original input type
        var result = new Mat();
        avg.ConvertTo(result, first.Type());

        return ValueTask.FromResult(result);
    }
}