using OpenCvSharp;

namespace Flowxel.Imaging.Operations.IO;

public class LoadOperation : Operation<Empty, Mat>
{
    public override Mat Execute(
        IReadOnlyList<Empty> inputs, 
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken ct)
    {
        var path = (string)parameters["Path"];
        return Cv2.ImRead(path, ImreadModes.Grayscale);
    }
}