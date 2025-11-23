# Flowxel.Graph

A robust, generic Directed Acyclic Graph (DAG) library for C# with support for topological sorting and parallel task execution.

## Features

- **Generic Graph Structure**: Works with any node type
- **DAG Enforcement**: Automatically prevents cycle creation
- **Topological Sorting**: Efficient Kahn's algorithm implementation
- **Parallel Execution**: Execute independent tasks concurrently
- **Type-Safe API**: Full type safety with modern C# features
- **Comprehensive Testing**: Extensive unit test coverage

## Installation

Add to your project:

```xml
<PackageReference Include="Flowxel.Graph" Version="1.0.0" />
```

## Quick Start

### Basic Graph Operations

```csharp
using Flowxel.Graph;

// Create a graph with your custom node type
public class TaskNode
{
    public string Name { get; set; }
    public string Description { get; set; }
}

var graph = new Graph<TaskNode>();

// Add nodes
var task1Id = Guid.NewGuid();
var task2Id = Guid.NewGuid();
var task3Id = Guid.NewGuid();

graph.AddNode(task1Id, new TaskNode { Name = "Task 1" });
graph.AddNode(task2Id, new TaskNode { Name = "Task 2" });
graph.AddNode(task3Id, new TaskNode { Name = "Task 3" });

// Add edges (dependencies)
graph.AddEdge(task1Id, task2Id); // Task 2 depends on Task 1
graph.AddEdge(task2Id, task3Id); // Task 3 depends on Task 2

// Query the graph
var successors = graph.GetSuccessors(task1Id);
var predecessors = graph.GetPredecessors(task3Id);
bool hasPath = graph.PathExists(task1Id, task3Id);

// Get topological order
var sorted = graph.TopologicalSort();
```

### Executable Nodes with Parallel Execution

```csharp
using Flowxel.Graph;

// Create a graph with executable nodes
var graph = new Graph<ExecutableNode>();

var node1 = new ExecutableNode
{
    Name = "Download Data",
    Execute = async (ct) =>
    {
        Console.WriteLine("Downloading data...");
        await Task.Delay(1000, ct);
        Console.WriteLine("Download complete");
    }
};

var node2 = new ExecutableNode
{
    Name = "Process Data",
    Execute = async (ct) =>
    {
        Console.WriteLine("Processing data...");
        await Task.Delay(500, ct);
        Console.WriteLine("Processing complete");
    }
};

var node3 = new ExecutableNode
{
    Name = "Generate Report",
    Execute = async (ct) =>
    {
        Console.WriteLine("Generating report...");
        await Task.Delay(300, ct);
        Console.WriteLine("Report complete");
    }
};

// Add nodes and dependencies
graph.AddNode(node1.Id, node1);
graph.AddNode(node2.Id, node2);
graph.AddNode(node3.Id, node3);

graph.AddEdge(node1.Id, node2.Id);
graph.AddEdge(node2.Id, node3.Id);

// Execute with automatic parallelization
var executor = new GraphExecutor<ExecutableNode>(graph);
await executor.ExecuteAsync();
```

### Custom Executable Nodes

```csharp
// Implement IExecutableNode for your custom type
public class DataPipelineStep : IExecutableNode
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; }
    public Func<Task<string>>? DataProcessor { get; set; }
    
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (DataProcessor != null)
        {
            var result = await DataProcessor();
            Console.WriteLine($"{Name}: {result}");
        }
    }
}

var graph = new Graph<DataPipelineStep>();
var step1 = new DataPipelineStep 
{ 
    Name = "Extract",
    DataProcessor = async () => 
    {
        await Task.Delay(100);
        return "Data extracted";
    }
};

graph.AddNode(step1.Id, step1);
var executor = new GraphExecutor<DataPipelineStep>(graph);
await executor.ExecuteAsync();
```

### Advanced: Parallel Execution with Results

```csharp
var graph = new Graph<ExecutableNode>();

// Create nodes that perform calculations
var nodes = new[]
{
    new ExecutableNode { Name = "Calc1" },
    new ExecutableNode { Name = "Calc2" },
    new ExecutableNode { Name = "Calc3" }
};

foreach (var node in nodes)
    graph.AddNode(node.Id, node);

var executor = new GraphExecutor<ExecutableNode>(graph);

// Execute and collect results
var results = await executor.ExecuteWithResultsAsync(async (node, ct) =>
{
    await Task.Delay(100, ct);
    return $"Result from {node.Name}";
});

foreach (var (id, result) in results)
{
    Console.WriteLine(result);
}
```

### Custom Execution Logic

```csharp
var graph = new Graph<ExecutableNode>();
// ... add nodes and edges ...

var executor = new GraphExecutor<ExecutableNode>(graph);

// Use custom execution function
await executor.ExecuteAsync(async (node, ct) =>
{
    Console.WriteLine($"Starting {node.Name}");
    await Task.Delay(100, ct);
    Console.WriteLine($"Completed {node.Name}");
}, cancellationToken);
```

## API Reference

### Graph<TNode>

#### Core Methods

- `bool AddNode(Guid id, TNode node)` - Adds a node to the graph
- `bool RemoveNode(Guid id)` - Removes a node and its edges
- `bool ContainsNode(Guid id)` - Checks if a node exists
- `bool TryGetNode(Guid id, out TNode? node)` - Gets a node by ID

#### Edge Operations

- `void AddEdge(Guid from, Guid to)` - Adds an edge (throws on cycle)
- `bool TryAddEdge(Guid from, Guid to)` - Tries to add an edge (returns false on cycle)
- `bool RemoveEdge(Guid from, Guid to)` - Removes an edge
- `bool HasEdge(Guid from, Guid to)` - Checks if an edge exists

#### Traversal

- `IEnumerable<TNode> GetSuccessors(Guid id)` - Gets successor nodes
- `IEnumerable<TNode> GetPredecessors(Guid id)` - Gets predecessor nodes
- `IEnumerable<Guid> GetSuccessorIds(Guid id)` - Gets successor IDs
- `IEnumerable<Guid> GetPredecessorIds(Guid id)` - Gets predecessor IDs

#### Analysis

- `bool PathExists(Guid from, Guid to)` - Checks if a path exists
- `IReadOnlyList<Guid> TopologicalSort()` - Returns topological order
- `IEnumerable<Guid> GetRootNodes()` - Gets nodes with no predecessors
- `IEnumerable<Guid> GetLeafNodes()` - Gets nodes with no successors
- `bool IsAcyclic()` - Checks if graph is a valid DAG
- `int GetInDegree(Guid id)` - Gets number of incoming edges
- `int GetOutDegree(Guid id)` - Gets number of outgoing edges

#### Properties

- `IReadOnlyCollection<TNode> Nodes` - All nodes in the graph
- `int NodeCount` - Number of nodes
- `int EdgeCount` - Number of edges

### GraphExecutor<TNode>

#### Methods

- `Task ExecuteAsync(CancellationToken cancellationToken = default)` - Executes all nodes
- `Task ExecuteAsync(Func<TNode, CancellationToken, Task> executeFunc, CancellationToken cancellationToken = default)` - Executes with custom function
- `Task<IReadOnlyDictionary<Guid, TResult>> ExecuteWithResultsAsync<TResult>(...)` - Executes and collects results

## Best Practices

### 1. Use TryAddEdge for Dynamic Graphs

```csharp
if (!graph.TryAddEdge(fromId, toId))
{
    Console.WriteLine("Cannot add edge: would create a cycle");
}
```

### 2. Handle Cancellation

```csharp
var cts = new CancellationTokenSource();
cts.CancelAfter(TimeSpan.FromSeconds(30));

try
{
    await executor.ExecuteAsync(cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Execution was cancelled");
}
```

### 3. Verify Graph Structure

```csharp
if (!graph.IsAcyclic())
{
    throw new InvalidOperationException("Graph contains cycles");
}

var sorted = graph.TopologicalSort();
// Process in topological order
```

### 4. Use Parallel Execution Wisely

The executor automatically parallelizes independent nodes. Nodes at the same level (with no dependencies between them) run concurrently:

```csharp
// These two nodes will run in parallel since they're independent
graph.AddNode(id1, node1);
graph.AddNode(id2, node2);
// No edge between them = parallel execution
```

## Real-World Examples

### Build System

```csharp
public class BuildTask : IExecutableNode
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; }
    public string Command { get; set; }
    
    public async Task ExecuteAsync(CancellationToken ct)
    {
        Console.WriteLine($"Building: {Name}");
        // Execute build command
        await Task.Delay(1000, ct); // Simulate build
        Console.WriteLine($"Completed: {Name}");
    }
}

var graph = new Graph<BuildTask>();
var compile = new BuildTask { Name = "Compile", Command = "dotnet build" };
var test = new BuildTask { Name = "Test", Command = "dotnet test" };
var package = new BuildTask { Name = "Package", Command = "dotnet pack" };

graph.AddNode(compile.Id, compile);
graph.AddNode(test.Id, test);
graph.AddNode(package.Id, package);

graph.AddEdge(compile.Id, test.Id);
graph.AddEdge(test.Id, package.Id);

await new GraphExecutor<BuildTask>(graph).ExecuteAsync();
```

### Data Processing Pipeline

```csharp
var graph = new Graph<ExecutableNode>();
var extract = new ExecutableNode { Name = "Extract" };
var transform1 = new ExecutableNode { Name = "Transform-1" };
var transform2 = new ExecutableNode { Name = "Transform-2" };
var load = new ExecutableNode { Name = "Load" };

graph.AddNode(extract.Id, extract);
graph.AddNode(transform1.Id, transform1);
graph.AddNode(transform2.Id, transform2);
graph.AddNode(load.Id, load);

// Extract feeds both transforms (they run in parallel)
graph.AddEdge(extract.Id, transform1.Id);
graph.AddEdge(extract.Id, transform2.Id);

// Both transforms must complete before load
graph.AddEdge(transform1.Id, load.Id);
graph.AddEdge(transform2.Id, load.Id);

await new GraphExecutor<ExecutableNode>(graph).ExecuteAsync();
```



## Performance Characteristics

- **AddNode**: O(1)
- **RemoveNode**: O(E) where E is the number of edges connected to the node
- **AddEdge**: O(V + E) for cycle detection
- **TopologicalSort**: O(V + E)
- **PathExists**: O(V + E)
- **Execution**: O(V + E) with optimal parallelization
