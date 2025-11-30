using Flowxel.Graph;

namespace Flowxel.Imaging.Operations;

public class Node<TIn, TOut>(Operation<TIn, TOut> operation, ResourcePool pool, Graph<IExecutableNode> graph) : IExecutableNode
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public Type InputType => typeof(TIn);
    public Type OutputType => typeof(TOut);

    public Dictionary<string, object> Parameters { get; } = new();

    public Task Execute(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        
        // Look in the pool for resources provided by predecessors
        var inputs = typeof(TIn) == typeof(Empty) ? [] : graph.GetPredecessorIds(Id).Select(pool.Get<TIn>).ToArray();

        // Execute operation
        var output = operation.Execute(inputs, Parameters, ct);

        // Publish the result to the pool 
        pool.Set(Id, output);
        
        return Task.CompletedTask;
    }
}