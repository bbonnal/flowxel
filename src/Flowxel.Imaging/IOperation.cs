using OpenCvSharp;

namespace Flowxel.Imaging;

public interface IOperation
{
    string Name { get; }
    ValueTask<Mat> ExecuteAsync(Mat input, IReadOnlyDictionary<string, object> parameters, CancellationToken ct = default);
}