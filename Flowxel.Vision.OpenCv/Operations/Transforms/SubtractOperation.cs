using Flowxel.Processing;
using Flowxel.Vision;
using OpenCvSharp;

namespace Flowxel.Vision.OpenCv.Operations.Transforms;

public class SubtractOperation(ResourcePool pool, Graph<IExecutableNode> graph, IVisionBackend? vision = null) : Node<Mat, Mat>(pool, graph, vision)
{
    protected override IReadOnlyList<string> InputPorts => ["left", "right"];

    protected override Mat ExecuteInternal(
        IReadOnlyList<Mat> inputs,
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken ct)
    {
        return (Mat)Vision.Subtract(inputs[0], inputs[1]);
    }
}
