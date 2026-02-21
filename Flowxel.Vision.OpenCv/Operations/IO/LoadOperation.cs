using Flowxel.Processing;
using Flowxel.Vision;
using OpenCvSharp;

namespace Flowxel.Vision.OpenCv.Operations.IO;

public class LoadOperation(ResourcePool pool, Graph<IExecutableNode> graph, IVisionBackend? vision = null) : Node<Empty, Mat>(pool, graph, vision)
{
    protected override Mat ExecuteInternal(
        IReadOnlyList<Empty> inputs, 
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken ct)
    {
        var path = (string)parameters["Path"];
        return (Mat)Vision.LoadGrayscale(path);
    }
}
