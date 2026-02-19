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

    public Task Execute(CancellationToken ct)
    {
        // Returns an uncompleted Task to the DAG scheduler for true parallel execution in WhenAll
        return Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();
        
            // Look in the pool for resources provided by predecessors
            var inputs = typeof(TIn) == typeof(Empty) ? [] : _graph.GetPredecessorIds(Id).Select(_pool.Get<TIn>).ToArray();

            // Execute operation
            var output = ExecuteInternal(inputs, Parameters, ct);

            // Publish the result to the pool 
            _pool.Set(Id, output);
        }, ct);
    }

    protected abstract TOut ExecuteInternal(
        IReadOnlyList<TIn> inputs,
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken ct);
}