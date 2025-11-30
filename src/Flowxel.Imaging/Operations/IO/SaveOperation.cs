using Flowxel.Imaging.Pipeline;
using OpenCvSharp;

namespace Flowxel.Imaging.Operations.IO;

public class SaveOperation : SinkOperation<Mat>
{
    public override Task ExecuteAsync(
        Mat input,
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken ct)
    {
        var path = (string)parameters["Path"];
        return Task.Run(() => Cv2.ImWrite(path, input), ct);
    }
}