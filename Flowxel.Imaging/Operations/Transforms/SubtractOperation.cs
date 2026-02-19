using Flowxel.Graph;
using OpenCvSharp;

namespace Flowxel.Imaging.Operations.Transforms;

public class SubtractOperation(ResourcePool pool, Graph<IExecutableNode> graph) : Node<Mat, Mat>(pool, graph)
{
    protected override IReadOnlyList<string> InputPorts => ["left", "right"];

    protected override Mat ExecuteInternal(
        IReadOnlyList<Mat> inputs,
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken ct)
    {
        var output = new Mat();
        Cv2.Subtract(inputs[0], inputs[1], output);
        return output;
    }
}
