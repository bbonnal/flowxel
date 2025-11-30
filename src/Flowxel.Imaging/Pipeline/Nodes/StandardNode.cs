using Flowxel.Graph;
using Flowxel.Imaging.Operations;

namespace Flowxel.Imaging.Pipeline.Nodes;

public class StandardNode<TIn, TOut>(Operation<TIn, TOut> operation, ResourcePool pool, Graph<IExecutableNode> graph) : IExecutableNode where TOut : notnull
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public Type InputType => typeof(TIn);
    public Type OutputType => typeof(TOut);

    public Dictionary<string, object> Parameters { get; } = new();

    public async Task ExecuteAsync(CancellationToken ct)
    {
        // Get predecessor from graph
       var predecessorId = graph.GetPredecessorIds(Id).Single();
       
        // Get input from pool (produced by another node)
        var input = pool.Get<TIn>(predecessorId);

        // Execute operation
        var output = await operation.ExecuteAsync(input, Parameters, ct);

        // Publish result to pool (identified by this node's ID)
        pool.Set(Id, output);
    }
}