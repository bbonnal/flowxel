using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Flowxel.Graph.Tests;

public class GraphExecutorTests
{
    private readonly ITestOutputHelper _output;
    public GraphExecutorTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public async Task ExecuteAsync_ShouldExecuteAllNodes()
    {
        // Arrange
        var graph = new Graph<IExecutableNode>();
        var executionOrder = new List<string>();
        var syncLock = new object();

        var node1 = CreateNode("Node1", executionOrder, syncLock);
        var node2 = CreateNode("Node2", executionOrder, syncLock);
        var node3 = CreateNode("Node3", executionOrder, syncLock);

        graph.AddNode(node1.Id, node1);
        graph.AddNode(node2.Id, node2);
        graph.AddNode(node3.Id, node3);

        graph.AddEdge(node1.Id, node2.Id);
        graph.AddEdge(node2.Id, node3.Id);

        var executor = new GraphExecutor<IExecutableNode>(graph);

        // Act
        await executor.ExecuteAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, executionOrder.Count);
        Assert.Equal("Node1", executionOrder[0]);
        Assert.Equal("Node2", executionOrder[1]);
        Assert.Equal("Node3", executionOrder[2]);
    }

    [Fact]
    public async Task ExecuteAsyncShouldExecuteNodesInParallel()
    {
        // Arrange
        var graph = new Graph<IExecutableNode>();
        var executionOrder = new List<string>();
        var syncLock = new object();
        var startTimes = new Dictionary<string, DateTime>();
        var endTimes = new Dictionary<string, DateTime>();

        // Create a graph where node1 has two independent successors
        var node1 = CreateNodeWithTiming("Node1", executionOrder, syncLock, startTimes, endTimes, 10);
        var node2 = CreateNodeWithTiming("Node2", executionOrder, syncLock, startTimes, endTimes, 50);
        var node3 = CreateNodeWithTiming("Node3", executionOrder, syncLock, startTimes, endTimes, 50);
        var node4 = CreateNodeWithTiming("Node4", executionOrder, syncLock, startTimes, endTimes, 10);

        graph.AddNode(node1.Id, node1);
        graph.AddNode(node2.Id, node2);
        graph.AddNode(node3.Id, node3);
        graph.AddNode(node4.Id, node4);

        graph.AddEdge(node1.Id, node2.Id);
        graph.AddEdge(node1.Id, node3.Id);
        graph.AddEdge(node2.Id, node4.Id);
        graph.AddEdge(node3.Id, node4.Id);

        var executor = new GraphExecutor<IExecutableNode>(graph);

        // Act
        await executor.ExecuteAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(4, executionOrder.Count);

        // Node2 and Node3 should start at roughly the same time (parallel execution)
        var node2Start = startTimes["Node2"];
        var node3Start = startTimes["Node3"];
        var timeDiff = Math.Abs((node2Start - node3Start).TotalMilliseconds);

        _output.WriteLine($"Node2 start: {node2Start:O}");
        _output.WriteLine($"Node3 start: {node3Start:O}");
        _output.WriteLine($"Time difference: {timeDiff}ms");

        Assert.True(timeDiff < 100, $"Nodes should start in parallel, but had {timeDiff}ms difference");
    }

    [Fact]
    public async Task ExecuteAsyncShouldRespectDependencies()
    {
        // Arrange
        var graph = new Graph<IExecutableNode>();
        var executionOrder = new List<string>();
        var syncLock = new object();

        var node1 = CreateNode("Node1", executionOrder, syncLock);
        var node2 = CreateNode("Node2", executionOrder, syncLock);
        var node3 = CreateNode("Node3", executionOrder, syncLock);
        var node4 = CreateNode("Node4", executionOrder, syncLock);

        graph.AddNode(node1.Id, node1);
        graph.AddNode(node2.Id, node2);
        graph.AddNode(node3.Id, node3);
        graph.AddNode(node4.Id, node4);

        // Create: 1 -> 2, 1 -> 3, 2 -> 4, 3 -> 4
        graph.AddEdge(node1.Id, node2.Id);
        graph.AddEdge(node1.Id, node3.Id);
        graph.AddEdge(node2.Id, node4.Id);
        graph.AddEdge(node3.Id, node4.Id);

        var executor = new GraphExecutor<IExecutableNode>(graph);

        // Act
        await executor.ExecuteAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(4, executionOrder.Count);
        Assert.Equal("Node1", executionOrder[0]);
        Assert.Equal("Node4", executionOrder[3]);

        // Node2 and Node3 should execute before Node4
        var node2Index = executionOrder.IndexOf("Node2");
        var node3Index = executionOrder.IndexOf("Node3");
        var node4Index = executionOrder.IndexOf("Node4");

        Assert.True(node2Index < node4Index);
        Assert.True(node3Index < node4Index);
    }

    [Fact]
    public async Task ExecuteAsyncWithCustomFunctionShouldWork()
    {
        // Arrange
        var graph = new Graph<IExecutableNode>();
        var executionOrder = new List<Guid>();
        var syncLock = new object();

        var node1 = new ExecutableNode<object, object> { Name = "Node1" };
        var node2 = new ExecutableNode<object, object> { Name = "Node2" };

        graph.AddNode(node1.Id, node1);
        graph.AddNode(node2.Id, node2);
        graph.AddEdge(node1.Id, node2.Id);

        var executor = new GraphExecutor<IExecutableNode>(graph);

        // Act
        await executor.ExecuteAsync(async (node, ct) =>
        {
            await Task.Delay(10, ct);
            lock (syncLock)
            {
                executionOrder.Add(node.Id);
            }
        }, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, executionOrder.Count);
        Assert.Equal(node1.Id, executionOrder[0]);
        Assert.Equal(node2.Id, executionOrder[1]);
    }

    [Fact]
    public async Task ExecuteAsyncShouldSupportCancellation()
    {
        // Arrange
        var graph = new Graph<IExecutableNode>();
        var cts = new CancellationTokenSource();

        var node1 = new ExecutableNode<object, object>
        {
            Name = "Node1",
            Execute = async (ct) => { await Task.Delay(50, ct); }
        };

        var node2 = new ExecutableNode<object, object>
        {
            Name = "Node2",
            Execute = async (ct) =>
            {
                // Cancel during execution
                cts.Cancel();
                await Task.Delay(100, ct);
            }
        };

        graph.AddNode(node1.Id, node1);
        graph.AddNode(node2.Id, node2);
        graph.AddEdge(node1.Id, node2.Id);

        var executor = new GraphExecutor<IExecutableNode>(graph);

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await executor.ExecuteAsync(cts.Token));
    }

    [Fact]
    public async Task ExecuteAsyncShouldHandleEmptyGraph()
    {
        // Arrange
        var graph = new Graph<IExecutableNode>();
        var executor = new GraphExecutor<IExecutableNode>(graph);

        // Act & Assert (should not throw)
        await executor.ExecuteAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ExecuteAsyncShouldHandleSingleNode()
    {
        // Arrange
        var graph = new Graph<IExecutableNode>();
        var executed = false;

        var node = new ExecutableNode<object, object>
        {
            Name = "SingleNode",
            Execute = async (ct) =>
            {
                await Task.Delay(10, ct);
                executed = true;
            }
        };

        graph.AddNode(node.Id, node);
        var executor = new GraphExecutor<IExecutableNode>(graph);

        // Act
        await executor.ExecuteAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(executed);
    }

    [Fact]
    public async Task ExecuteAsyncShouldHandleComplexDAG()
    {
        // Arrange
        var graph = new Graph<IExecutableNode>();
        var executionOrder = new List<string>();
        var syncLock = new object();

        // Create a more complex DAG
        var nodes = new Dictionary<string, IExecutableNode>();
        for (int i = 1; i <= 7; i++)
        {
            var node = CreateNode($"Node{i}", executionOrder, syncLock);
            nodes[$"Node{i}"] = node;
            graph.AddNode(node.Id, node);
        }

        // Create dependencies:
        // Node1 -> Node2, Node3
        // Node2 -> Node4, Node5
        // Node3 -> Node5
        // Node4 -> Node6
        // Node5 -> Node6, Node7
        graph.AddEdge(nodes["Node1"].Id, nodes["Node2"].Id);
        graph.AddEdge(nodes["Node1"].Id, nodes["Node3"].Id);
        graph.AddEdge(nodes["Node2"].Id, nodes["Node4"].Id);
        graph.AddEdge(nodes["Node2"].Id, nodes["Node5"].Id);
        graph.AddEdge(nodes["Node3"].Id, nodes["Node5"].Id);
        graph.AddEdge(nodes["Node4"].Id, nodes["Node6"].Id);
        graph.AddEdge(nodes["Node5"].Id, nodes["Node6"].Id);
        graph.AddEdge(nodes["Node5"].Id, nodes["Node7"].Id);

        var executor = new GraphExecutor<IExecutableNode>(graph);

        // Act
        await executor.ExecuteAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(7, executionOrder.Count);

        // Verify topological ordering
        var indexOf = executionOrder
            .Select((n, i) => new { Name = n, Index = i })
            .ToDictionary(x => x.Name, x => x.Index);

        Assert.True(indexOf["Node1"] < indexOf["Node2"]);
        Assert.True(indexOf["Node1"] < indexOf["Node3"]);
        Assert.True(indexOf["Node2"] < indexOf["Node4"]);
        Assert.True(indexOf["Node2"] < indexOf["Node5"]);
        Assert.True(indexOf["Node3"] < indexOf["Node5"]);
        Assert.True(indexOf["Node4"] < indexOf["Node6"]);
        Assert.True(indexOf["Node5"] < indexOf["Node6"]);
        Assert.True(indexOf["Node5"] < indexOf["Node7"]);
    }

    [Fact]
    public async Task ExecuteAsyncWithExceptionShouldPropagateException()
    {
        // Arrange
        var graph = new Graph<IExecutableNode>();

        var node1 = new ExecutableNode<object, object>
        {
            Name = "Node1",
            Execute = async (ct) =>
            {
                await Task.Delay(10, ct);
                throw new InvalidOperationException("Test exception");
            }
        };

        graph.AddNode(node1.Id, node1);
        var executor = new GraphExecutor<IExecutableNode>(graph);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await executor.ExecuteAsync(TestContext.Current.CancellationToken));
        Assert.Equal("Test exception", exception.Message);
    }

    #region Helpers

    private IExecutableNode CreateNode(
        string name,
        List<string> executionOrder,
        object syncLock)
    {
        return new ExecutableNode<object, object>
        {
            Name = name,
            Execute = async (ct) =>
            {
                // Simulate some work
                await Task.Delay(10, ct);

                // Thread-safe recording of execution completion
                lock (syncLock)
                {
                    executionOrder.Add(name);
                }
            }
        };
    }

    private IExecutableNode CreateNodeWithTiming(
        string name,
        List<string> executionOrder,
        object syncLock,
        Dictionary<string, DateTime> startTimes,
        Dictionary<string, DateTime> endTimes,
        int delayMs)
    {
        return new ExecutableNode<object, object>
        {
            Name = name,
            Execute = async (ct) =>
            {
                // Record start time (thread-safe)
                lock (syncLock)
                {
                    startTimes[name] = DateTime.UtcNow;
                }

                // Simulate work with configurable duration
                await Task.Delay(delayMs, ct);

                // Record end time and completion order (thread-safe)
                lock (syncLock)
                {
                    endTimes[name] = DateTime.UtcNow;
                    executionOrder.Add(name);
                }
            }
        };
    }

    #endregion
}