using Flowxel.Graph;

namespace Flowxel.Imaging.Operations;

public readonly struct Empty { }

public abstract class Node<TIn, TOut> : IExecutableNode where TOut : notnull
{
    private readonly ResourcePool _pool;
    private readonly Graph<IExecutableNode> _graph;

    protected Node(ResourcePool pool, Graph<IExecutableNode> graph)
    {
        _pool = pool;
        _graph = graph;
    }

    public Guid Id { get; } = Guid.NewGuid();
    public Type InputType => typeof(TIn);
    public Type OutputType => typeof(TOut);

    public Dictionary<string, object> Parameters { get; } = new();

    protected virtual IReadOnlyList<string> InputPorts =>
        typeof(TIn) == typeof(Empty)
            ? []
            : [GraphPorts.DefaultInput];

    protected virtual string OutputPort => GraphPorts.DefaultOutput;

    public Task Execute(CancellationToken ct)
    {
        return Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();

            var inputs = ResolveInputs();

            var output = ExecuteInternal(inputs, Parameters, ct);
            _pool.Set(Id, OutputPort, output);
        }, ct);
    }

    private IReadOnlyList<TIn> ResolveInputs()
    {
        if (typeof(TIn) == typeof(Empty))
            return [];

        var incomingByPort = _graph
            .GetIncomingEdges(Id)
            .GroupBy(edge => edge.ToPortKey, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);

        var inputPorts = InputPorts;
        if (inputPorts.Count == 0)
            return [];

        var inputs = new List<TIn>(inputPorts.Count);
        foreach (var inputPort in inputPorts)
        {
            if (!incomingByPort.TryGetValue(inputPort, out var edges))
                throw new InvalidOperationException($"Missing connection for input port '{inputPort}' on node {Id}.");

            if (edges.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Input port '{inputPort}' on node {Id} expects exactly one source but received {edges.Length}.");
            }

            var edge = edges[0];
            inputs.Add(_pool.Get<TIn>(edge.FromNodeId, edge.FromPortKey));
        }

        return inputs;
    }

    protected abstract TOut ExecuteInternal(
        IReadOnlyList<TIn> inputs,
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken ct);
}
