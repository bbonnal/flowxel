using Flowxel.Core.Geometry.Primitives;
using Flowxel.Core.Geometry.Shapes;
using Flowxel.Graph;

namespace Flowxel.Imaging.Operations.Constructions;

public class ConstructLineArcIntersectionsOperation(ResourcePool pool, Graph<IExecutableNode> graph) : Node<Line, Point[]>(pool, graph)
{
    protected override Point[] ExecuteInternal(
        IReadOnlyList<Line> inputs,
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken ct)
    {
        if (inputs.Count != 1)
            throw new InvalidOperationException("ConstructLineArcIntersectionsOperation requires exactly one line input.");

        var line = inputs[0];
        var arc = (Arc)parameters["Arc"];

        var origin = line.StartPoint.Position;
        var direction = line.Pose.Orientation.Normalize();
        var center = arc.Pose.Position;
        var relative = origin - center;

        var b = 2 * Vector.Dot(relative, direction);
        var c = Vector.Dot(relative, relative) - arc.Radius * arc.Radius;
        var discriminant = b * b - 4 * c;
        if (discriminant < 0)
            throw new InvalidOperationException("Line does not intersect arc.");

        var sqrt = Math.Sqrt(discriminant);
        var t1 = (-b - sqrt) / 2;
        var t2 = (-b + sqrt) / 2;

        var candidates = new[]
        {
            (T: t1, Position: origin + direction.Scale(t1)),
            (T: t2, Position: origin + direction.Scale(t2))
        };

        var accepted = candidates
            .Where(candidate => IsInArc(candidate.Position, arc))
            .OrderBy(candidate => candidate.T)
            .Select(candidate => new Point
            {
                Pose = new Pose(candidate.Position, new Vector(1, 0))
            })
            .ToArray();

        if (accepted.Length != 2)
            throw new InvalidOperationException($"Expected two line-arc intersections but found {accepted.Length}.");

        return accepted;
    }

    private static bool IsInArc(Vector point, Arc arc)
    {
        if (RegionMasking.IsFullArc(arc))
            return true;

        var local = point.Transform(arc.Pose.ToLocal);
        var angle = Math.Atan2(local.Y, local.X);
        return RegionMasking.IsAngleInArc(angle, arc);
    }
}
