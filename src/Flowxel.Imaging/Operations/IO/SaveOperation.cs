using OpenCvSharp;

namespace Flowxel.Imaging.Operations.IO;

public class SaveOperation : Operation<Mat, Empty>
{
    public override Empty Execute(
        IReadOnlyList<Mat> inputs,
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken ct)
    {
        var path = (string)parameters["Path"];
        Cv2.ImWrite(path, inputs[0]);

        return default;
    }
}