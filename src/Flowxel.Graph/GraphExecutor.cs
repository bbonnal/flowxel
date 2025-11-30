namespace Flowxel.Graph;

/// <summary>
/// Provides execution capabilities for a graph of executable nodes.
/// </summary>
/// <typeparam name="TNode">The type of executable node.</typeparam>
public class GraphExecutor<TNode> where TNode : class, IExecutableNode
{
    private readonly Graph<TNode> _graph;

    /// <summary>
    /// Initializes a new instance of the GraphExecutor class.
    /// </summary>
    /// <param name="graph">The graph to execute.</param>
    public GraphExecutor(Graph<TNode> graph)
    {
        _graph = graph ?? throw new ArgumentNullException(nameof(graph));
    }

    /// <summary>
    /// Executes all nodes in the graph in topological order with maximum parallelism.
    /// Nodes at the same level (with no dependencies between them) are executed concurrently.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the execution.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the graph contains cycles.</exception>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var inDegree = _graph.Nodes.ToDictionary(n => n.Id, n => _graph.GetInDegree(n.Id));
        var ready = new Queue<TNode>(_graph.Nodes.Where(n => inDegree[n.Id] == 0));

        while (ready.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Collect all nodes ready to execute at this level
            var currentBatch = new List<TNode>();
            while (ready.Count > 0)
                currentBatch.Add(ready.Dequeue());
            

            // Execute all nodes in the current batch concurrently
            var tasks = currentBatch.Select(node => node.ExecuteAsync(cancellationToken));
            await Task.WhenAll(tasks);


            // Update in-degrees and enqueue newly ready nodes
            foreach (var successorId in currentBatch.SelectMany(node => _graph.GetSuccessorIds(node.Id)))
            {
                inDegree[successorId]--;
                if (inDegree[successorId] != 0) continue;
                if (_graph.TryGetNode(successorId, out var successor) && successor != null)
                    ready.Enqueue(successor);
            }
        }

        // Verify all nodes were executed
        if (inDegree.Values.Any(d => d > 0))
            throw new InvalidOperationException("Graph contains cycles and cannot be fully executed.");
    }

    /// <summary>
    /// Executes all nodes in the graph with custom execution logic.
    /// </summary>
    /// <param name="executeFunc">A custom function to execute each node.</param>
    /// <param name="cancellationToken">A token to cancel the execution.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ExecuteAsync(
        Func<TNode, CancellationToken, Task> executeFunc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(executeFunc);

        var inDegree = _graph.Nodes.ToDictionary(n => n.Id, n => _graph.GetInDegree(n.Id));
        var ready = new Queue<TNode>(_graph.Nodes.Where(n => inDegree[n.Id] == 0));

        while (ready.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var currentBatch = new List<TNode>();
            while (ready.Count > 0)
                currentBatch.Add(ready.Dequeue());

            var tasks = currentBatch.Select(node => executeFunc(node, cancellationToken));
            await Task.WhenAll(tasks);

            foreach (var successorId in currentBatch.SelectMany(node => _graph.GetSuccessorIds(node.Id)))
            {
                inDegree[successorId]--;
                if (inDegree[successorId] != 0) continue;
                if (_graph.TryGetNode(successorId, out var successor) && successor != null)
                    ready.Enqueue(successor);
            }
        }

        if (inDegree.Values.Any(d => d > 0))
            throw new InvalidOperationException("Graph contains cycles and cannot be fully executed.");
    }
}
