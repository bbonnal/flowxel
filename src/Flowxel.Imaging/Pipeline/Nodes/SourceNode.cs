using Flowxel.Graph;
using Flowxel.Imaging.Operations;

namespace Flowxel.Imaging.Pipeline.Nodes;

public class SourceNode<TOut>(SourceOperation<TOut> operation, ResourcePool pool, Graph<IExecutableNode> graph) : IExecutableNode where TOut : notnull
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public Type InputType => typeof(void);
    public Type OutputType => typeof(TOut);

    public Dictionary<string, object> Parameters { get; } = new();

    public async Task ExecuteAsync(CancellationToken ct)
    {
        // Execute operation
        var output = await operation.ExecuteAsync(Parameters, ct);
        
        // Publish the result to the pool
        pool.Set(Id, output);
    }
}