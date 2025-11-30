using Flowxel.Graph;
using Flowxel.Imaging.Operations;

namespace Flowxel.Imaging.Pipeline.Nodes;

public class SinkNode<TIn>(SinkOperation<TIn> operation, ResourcePool pool,Graph<IExecutableNode> graph ) : IExecutableNode 
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public Type InputType => typeof(TIn);
    public Type OutputType => typeof(void);

    public Dictionary<string, object> Parameters { get; } = new();

    public async Task ExecuteAsync(CancellationToken ct)
    {
        // Get the predecessor from the graph
        var predecessorId = graph.GetPredecessorIds(Id).Single();
        
       // Get the input from the pool 
        var input = pool.Get<TIn>(predecessorId);

        // Execute operation
        await operation.ExecuteAsync(input, Parameters, ct);
    }
}