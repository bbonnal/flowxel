using Flowxel.Core.Geometry.Primitives;
using Flowxel.Core.Geometry.Shapes;
using Flowxel.Processing;

namespace Flowxel.Operations.Constructions;

public class ConstructLineLineBisectorOperation(ResourcePool pool, Graph<IExecutableNode> graph) : ExecutionNode<Line, Line>(pool, graph)
{
    protected override IReadOnlyList<string> InputPorts => ["first", "second"];

    protected override Line ExecuteInternal(
        IReadOnlyList<Line> inputs,
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken ct)
    {
        if (inputs.Count != 2)
            throw new InvalidOperationException("ConstructLineLineBisectorOperation requires exactly two line inputs.");

        var first = inputs[0];
        var second = inputs[1];
        var intersection = ConstructLineLineIntersectionOperation.ComputeLineLineIntersection(first, second);

        var directionA = first.Pose.Orientation.Normalize();
        var directionB = second.Pose.Orientation.Normalize();

        // Keep the acute-angle bisector.
        if (Vector.Dot(directionA, directionB) < 0)
            directionB = directionB.Scale(-1);

        var bisectorDirection = directionA + directionB;
        if (bisectorDirection.M <= 1e-9)
            bisectorDirection = directionA.Rotate(Math.PI / 2);

        return new Line
        {
            Pose = new Pose(intersection, bisectorDirection.Normalize()),
            Length = Math.Max((first.Length + second.Length) * 0.5, 1),
            LineWeight = first.LineWeight,
            Fill = first.Fill
        };
    }
}
