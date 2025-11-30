using Flowxel.Graph;
using Flowxel.Imaging.Operations;

namespace Flowxel.Imaging.Pipeline.Nodes;

public class Node<TIn, TOut>(CombineOperation<TIn, TOut> operation, ResourcePool pool, Graph<IExecutableNode> graph) : IExecutableNode where TOut : notnull
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public Type InputType => typeof(TIn);
    public Type OutputType => typeof(TOut);

    public Dictionary<string, object> Parameters { get; } = new();

    public async Task ExecuteAsync(CancellationToken ct)
    {
        // Get all predecessors from the graph
        var predecessorIds = graph.GetPredecessorIds(Id).ToList();
        
        // Get all inputs from the pool
        var inputs = predecessorIds.Select(id => pool.Get<TIn>(id)).ToArray();

        // Execute operation
        var output = await operation.ExecuteAsync(inputs, Parameters, ct);

        // Publish the result to the pool 
        pool.Set(Id, output);
    }
}