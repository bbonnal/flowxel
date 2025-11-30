using Flowxel.Graph;
using OpenCvSharp;

namespace Flowxel.Imaging.Operations.IO;

public class LoadOperation(ResourcePool pool, Graph<IExecutableNode> graph) : Node<Empty, Mat>(pool, graph)
{
    public override Mat ExecuteInternal(
        IReadOnlyList<Empty> inputs, 
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken ct)
    {
        var path = (string)parameters["Path"];
        return Cv2.ImRead(path, ImreadModes.Grayscale);
    }
}