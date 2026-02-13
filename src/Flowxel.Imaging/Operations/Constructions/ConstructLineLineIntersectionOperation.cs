using Flowxel.Core.Geometry.Primitives;
using Flowxel.Core.Geometry.Shapes;
using Flowxel.Graph;

namespace Flowxel.Imaging.Operations.Constructions;

public class ConstructLineLineIntersectionOperation(ResourcePool pool, Graph<IExecutableNode> graph) : Node<Line, Point>(pool, graph)
{
    protected override Point ExecuteInternal(
        IReadOnlyList<Line> inputs,
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken ct)
    {
        if (inputs.Count != 2)
            throw new InvalidOperationException("ConstructLineLineIntersectionOperation requires exactly two line inputs.");

        var intersection = ComputeLineLineIntersection(inputs[0], inputs[1]);
        return new Point
        {
            Pose = new Pose(intersection, new Vector(1, 0))
        };
    }

    internal static Vector ComputeLineLineIntersection(Line first, Line second)
    {
        var p = first.StartPoint.Position;
        var q = second.StartPoint.Position;
        var r = first.Pose.Orientation.Normalize();
        var s = second.Pose.Orientation.Normalize();

        var denominator = Vector.Cross(r, s);
        if (Math.Abs(denominator) <= 1e-9)
            throw new InvalidOperationException("Lines are parallel and do not have a unique intersection.");

        var t = Vector.Cross(q - p, s) / denominator;
        return p + r.Scale(t);
    }
}
