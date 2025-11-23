using Flowxel.Imaging.Pipeline;
using OpenCvSharp;

namespace Flowxel.Imaging.Operations.IO;

public class LoadOperation : Operation<None, Mat>
{
    public override async ValueTask<Mat> ExecuteAsync(
        None input,
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken ct = default)
    {
        var path = parameters["Path"] as string ?? "";
        var mode = parameters.GetValueOrDefault("Mode") as ImreadModes? ?? ImreadModes.Grayscale;
        return await Task.Run(() => Cv2.ImRead(path, mode), ct);
    }
}