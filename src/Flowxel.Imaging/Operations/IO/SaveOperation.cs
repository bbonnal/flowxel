using Flowxel.Graph;
using OpenCvSharp;

namespace Flowxel.Imaging.Operations.IO;

public class SaveOperation(ResourcePool pool, Graph<IExecutableNode> graph) : Node<Mat, Empty>(pool, graph)
{
    public override Empty ExecuteInternal(
        IReadOnlyList<Mat> inputs,
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken ct)
    {
        var path = (string)parameters["Path"];
        Cv2.ImWrite(path, inputs[0]);

        return default;
    }
}