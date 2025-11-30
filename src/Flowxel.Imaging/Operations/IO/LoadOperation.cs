using Flowxel.Imaging.Pipeline;
using OpenCvSharp;

namespace Flowxel.Imaging.Operations.IO;

public class LoadOperation : SourceOperation<Mat>
{
    public override Task<Mat> ExecuteAsync(
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken ct)
    {
        var path = (string)parameters["Path"];
        return Task.Run(() => Cv2.ImRead(path, ImreadModes.Grayscale), ct);
    }
}