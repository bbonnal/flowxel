using Flowxel.Imaging.Pipeline;
using OpenCvSharp;

namespace Flowxel.Imaging.Operations.IO;

public class SaveOperation : Operation<Mat, bool>
{
    public override async ValueTask<bool> ExecuteAsync(Mat input, IReadOnlyDictionary<string, object> parameters,
        CancellationToken ct)
    {
        var path = parameters["Path"] is string pathValue ? pathValue : "";
        var mode = parameters.GetValueOrDefault("Mode") is ImreadModes m ? m : ImreadModes.Grayscale;
        return await Task.Run(() => Cv2.ImWrite(path, input), ct);
    }
}