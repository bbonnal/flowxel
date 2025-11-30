namespace Flowxel.Graph;

/// <summary>
/// Represents a directed acyclic graph (DAG) with support for topological sorting and parallel execution.
/// </summary>
/// <typeparam name="TNode">The type of node data stored in the graph.</typeparam>
public class Graph<TNode> where TNode : class, IExecutableNode
{
    private readonly Dictionary<Guid, HashSet<Guid>> _incoming = new();
    private readonly Dictionary<Guid, TNode> _nodes = new();
    private readonly Dictionary<Guid, HashSet<Guid>> _outgoing = new();

    /// <summary>
    /// Gets all nodes in the graph.
    /// </summary>
    public IReadOnlyCollection<TNode> Nodes => _nodes.Values;

    /// <summary>
    /// Gets the number of nodes in the graph.
    /// </summary>
    public int NodeCount => _nodes.Count;

    /// <summary>
    /// Gets the number of edges in the graph.
    /// </summary>
    public int EdgeCount => _outgoing.Values.Sum(set => set.Count);

    /// <summary>
    /// Adds a node to the graph.
    /// </summary>
    /// <param name="node">The node to add.</param>
    /// <returns>True if the node was added; false if a node with the same ID already exists.</returns>
    /// <exception cref="ArgumentNullException">Thrown when node is null.</exception>
    public bool AddNode(TNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (!_nodes.TryAdd(node.Id, node)) return false;

        _outgoing[node.Id] = [];
        _incoming[node.Id] = [];
        return true;
    }

    /// <summary>
    /// Removes a node from the graph along with all its edges.
    /// </summary>
    /// <param name="id">The ID of the node to remove.</param>
    /// <returns>True if the node was removed; false if the node doesn't exist.</returns>
    public bool RemoveNode(Guid id)
    {
        if (!_nodes.ContainsKey(id)) return false;

        // Remove all edges connected to this node
        foreach (var predecessor in _incoming[id].ToList())
            _outgoing[predecessor].Remove(id);
        foreach (var successor in _outgoing[id].ToList())
            _incoming[successor].Remove(id);

        _incoming.Remove(id);
        _outgoing.Remove(id);
        _nodes.Remove(id);

        return true;
    }

    /// <summary>
    /// Checks if a node exists in the graph.
    /// </summary>
    /// <param name="id">The ID of the node to check.</param>
    /// <returns>True if the node exists; otherwise false.</returns>
    public bool ContainsNode(Guid id) => _nodes.ContainsKey(id);

    /// <summary>
    /// Gets a node by its ID.
    /// </summary>
    /// <param name="id">The ID of the node to retrieve.</param>
    /// <param name="node">The node data if found.</param>
    /// <returns>True if the node was found; otherwise false.</returns>
    public bool TryGetNode(Guid id, out TNode? node)
    {
        return _nodes.TryGetValue(id, out node);
    }

    /// <summary>
    /// Adds a directed edge from one node to another.
    /// </summary>
    /// <param name="from">The source node ID.</param>
    /// <param name="to">The destination node ID.</param>
    /// <exception cref="ArgumentException">Thrown when attempting to create a self-loop or when nodes don't exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when adding the edge would create a cycle.</exception>
    public void AddEdge(Guid from, Guid to)
    {
        if (from == to)
            throw new ArgumentException("Self-loops are not allowed in a DAG.");
        if (!_nodes.ContainsKey(from))
            throw new ArgumentException($"Source node {from} does not exist in the graph.", nameof(from));
        if (!_nodes.ContainsKey(to))
            throw new ArgumentException($"Destination node {to} does not exist in the graph.", nameof(to));
        if (_outgoing[from].Contains(to))
            return; // Edge already exists

        if (PathExists(to, from))
            throw new InvalidOperationException($"Adding edge from {from} to {to} would create a cycle.");

        _outgoing[from].Add(to);
        _incoming[to].Add(from);
    }

    /// <summary>
    /// Tries to add a directed edge from one node to another.
    /// </summary>
    /// <param name="from">The source node ID.</param>
    /// <param name="to">The destination node ID.</param>
    /// <returns>True if the edge was added; false if it would create a cycle or already exists.</returns>
    public bool TryAddEdge(Guid from, Guid to)
    {
        if (from == to || !_nodes.ContainsKey(from) || !_nodes.ContainsKey(to))
            return false;
        if (_outgoing[from].Contains(to))
            return false;
        if (PathExists(to, from))
            return false;

        _outgoing[from].Add(to);
        _incoming[to].Add(from);
        return true;
    }

    /// <summary>
    /// Removes a directed edge between two nodes.
    /// </summary>
    /// <param name="from">The source node ID.</param>
    /// <param name="to">The destination node ID.</param>
    /// <returns>True if the edge was removed; false if the edge doesn't exist.</returns>
    public bool RemoveEdge(Guid from, Guid to)
    {
        if (!_nodes.ContainsKey(from) || !_nodes.ContainsKey(to))
            return false;

        var removed = _outgoing[from].Remove(to);
        if (removed)
            _incoming[to].Remove(from);
        return removed;
    }

    /// <summary>
    /// Checks if an edge exists between two nodes.
    /// </summary>
    /// <param name="from">The source node ID.</param>
    /// <param name="to">The destination node ID.</param>
    /// <returns>True if the edge exists; otherwise false.</returns>
    public bool HasEdge(Guid from, Guid to)
    {
        return _nodes.ContainsKey(from) && _outgoing[from].Contains(to);
    }

    /// <summary>
    /// Gets all successor nodes of a given node.
    /// </summary>
    /// <param name="id">The node ID.</param>
    /// <returns>An enumerable of successor nodes.</returns>
    public IEnumerable<TNode> GetSuccessors(Guid id)
    {
        if (!_nodes.ContainsKey(id))
            yield break;

        foreach (var successorId in _outgoing[id])
            yield return _nodes[successorId];
    }

    /// <summary>
    /// Gets all predecessor nodes of a given node.
    /// </summary>
    /// <param name="id">The node ID.</param>
    /// <returns>An enumerable of predecessor nodes.</returns>
    public IEnumerable<TNode> GetPredecessors(Guid id)
    {
        if (!_nodes.ContainsKey(id))
            yield break;

        foreach (var predecessorId in _incoming[id])
            yield return _nodes[predecessorId];
    }

    /// <summary>
    /// Gets the IDs of all successor nodes.
    /// </summary>
    /// <param name="id">The node ID.</param>
    /// <returns>An enumerable of successor node IDs.</returns>
    public IEnumerable<Guid> GetSuccessorIds(Guid id)
    {
        return _outgoing.TryGetValue(id, out var successors) 
            ? successors 
            : Enumerable.Empty<Guid>();
    }

    /// <summary>
    /// Gets the IDs of all predecessor nodes.
    /// </summary>
    /// <param name="id">The node ID.</param>
    /// <returns>An enumerable of predecessor node IDs.</returns>
    public IEnumerable<Guid> GetPredecessorIds(Guid id)
    {
        return _incoming.TryGetValue(id, out var predecessors) 
            ? predecessors 
            : Enumerable.Empty<Guid>();
    }

    /// <summary>
    /// Gets the in-degree (number of predecessors) of a node.
    /// </summary>
    /// <param name="id">The node ID.</param>
    /// <returns>The number of incoming edges.</returns>
    public int GetInDegree(Guid id)
    {
        return _incoming.TryGetValue(id, out var predecessors) ? predecessors.Count : 0;
    }

    /// <summary>
    /// Gets the out-degree (number of successors) of a node.
    /// </summary>
    /// <param name="id">The node ID.</param>
    /// <returns>The number of outgoing edges.</returns>
    public int GetOutDegree(Guid id)
    {
        return _outgoing.TryGetValue(id, out var successors) ? successors.Count : 0;
    }

    /// <summary>
    /// Checks if a path exists between two nodes.
    /// </summary>
    /// <param name="from">The source node ID.</param>
    /// <param name="to">The destination node ID.</param>
    /// <returns>True if a path exists; otherwise false.</returns>
    public bool PathExists(Guid from, Guid to)
    {
        if (!_nodes.ContainsKey(from) || !_nodes.ContainsKey(to))
            return false;

        var stack = new Stack<Guid>();
        var visited = new HashSet<Guid>();

        stack.Push(from);
        visited.Add(from);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (current == to) return true;

            if (!_outgoing.TryGetValue(current, out var successors))
                continue;

            foreach (var successor in successors)
            {
                if (visited.Add(successor))
                    stack.Push(successor);
            }
        }

        return false;
    }

    /// <summary>
    /// Performs a topological sort on the graph.
    /// </summary>
    /// <returns>A list of node IDs in topological order.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the graph contains cycles.</exception>
    public IReadOnlyList<Guid> TopologicalSort()
    {
        var inDegree = _nodes.Keys.ToDictionary(k => k, k => _incoming[k].Count);
        var queue = new Queue<Guid>(inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
        var result = new List<Guid>();

        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            result.Add(id);

            foreach (var successor in _outgoing[id])
            {
                inDegree[successor]--;
                if (inDegree[successor] == 0)
                    queue.Enqueue(successor);
            }
        }

        if (result.Count != _nodes.Count)
            throw new InvalidOperationException("Cannot perform topological sort: graph contains cycles.");

        return result;
    }

    /// <summary>
    /// Gets all root nodes (nodes with no predecessors).
    /// </summary>
    /// <returns>An enumerable of root node IDs.</returns>
    public IEnumerable<Guid> GetRootNodes()
    {
        return _nodes.Keys.Where(id => _incoming[id].Count == 0);
    }

    /// <summary>
    /// Gets all leaf nodes (nodes with no successors).
    /// </summary>
    /// <returns>An enumerable of leaf node IDs.</returns>
    public IEnumerable<Guid> GetLeafNodes()
    {
        return _nodes.Keys.Where(id => _outgoing[id].Count == 0);
    }

    /// <summary>
    /// Checks if the graph is a valid DAG (contains no cycles).
    /// </summary>
    /// <returns>True if the graph is acyclic; otherwise false.</returns>
    public bool IsAcyclic()
    {
        try
        {
            TopologicalSort();
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
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
        var inDegree = Nodes.ToDictionary(n => n.Id, n => GetInDegree(n.Id));
        var ready = new Queue<TNode>(Nodes.Where(n => inDegree[n.Id] == 0));

        while (ready.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Collect all nodes ready to execute at this level
            var currentBatch = new List<TNode>();
            while (ready.Count > 0)
                currentBatch.Add(ready.Dequeue());
            

            // Execute all nodes in the current batch concurrently
            var tasks = currentBatch.Select(node => node.Execute(cancellationToken));
            await Task.WhenAll(tasks);


            // Update in-degrees and enqueue newly ready nodes
            foreach (var successorId in currentBatch.SelectMany(node => GetSuccessorIds(node.Id)))
            {
                inDegree[successorId]--;
                if (inDegree[successorId] != 0) continue;
                if (TryGetNode(successorId, out var successor) && successor != null)
                    ready.Enqueue(successor);
            }
        }

        // Verify all nodes were executed
        if (inDegree.Values.Any(d => d > 0))
            throw new InvalidOperationException("Graph contains cycles and cannot be fully executed.");
    }

    /// <summary>
    /// Clears all nodes and edges from the graph.
    /// </summary>
    public void Clear()
    {
        _nodes.Clear();
        _incoming.Clear();
        _outgoing.Clear();
    }
}