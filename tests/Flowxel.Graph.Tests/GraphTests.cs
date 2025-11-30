namespace Flowxel.Graph.Tests;

public class GraphTests(ITestOutputHelper output)
{

    [Fact]
    public void AddNodeShouldAddNodeSuccessfully()
    {
        // Arrange
        var graph = new Graph<TestNode>();
        var id = Guid.NewGuid();
        var node = new TestNode { Name = "Node1" };

        // Act
        var result = graph.AddNode(id, node);

        // Assert
        Assert.True(result);
        Assert.Equal(1, graph.NodeCount);
        Assert.True(graph.ContainsNode(id));
    }

    [Fact]
    public void AddNodeShouldReturnFalseForDuplicateId()
    {
        // Arrange
        var graph = new Graph<TestNode>();
        var id = Guid.NewGuid();
        var node1 = new TestNode { Name = "Node1" };
        var node2 = new TestNode { Name = "Node2" };

        // Act
        graph.AddNode(id, node1);
        var result = graph.AddNode(id, node2);

        // Assert
        Assert.False(result);
        Assert.Equal(1, graph.NodeCount);
    }

    [Fact]
    public void AddNodeShouldThrowForNullNode()
    {
        // Arrange
        var graph = new Graph<TestNode>();
        var id = Guid.NewGuid();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => graph.AddNode(id, null!));
    }

    [Fact]
    public void RemoveNodeShouldRemoveNodeAndItsEdges()
    {
        // Arrange
        var graph = new Graph<TestNode>();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();

        graph.AddNode(id1, new TestNode { Name = "Node1" });
        graph.AddNode(id2, new TestNode { Name = "Node2" });
        graph.AddNode(id3, new TestNode { Name = "Node3" });
        graph.AddEdge(id1, id2);
        graph.AddEdge(id2, id3);

        // Act
        var result = graph.RemoveNode(id2);

        // Assert
        Assert.True(result);
        Assert.Equal(2, graph.NodeCount);
        Assert.False(graph.ContainsNode(id2));
        Assert.False(graph.HasEdge(id1, id2));
        Assert.False(graph.HasEdge(id2, id3));
    }

    [Fact]
    public void AddEdgeShouldCreateEdgeSuccessfully()
    {
        // Arrange
        var graph = new Graph<TestNode>();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        graph.AddNode(id1, new TestNode { Name = "Node1" });
        graph.AddNode(id2, new TestNode { Name = "Node2" });

        // Act
        graph.AddEdge(id1, id2);

        // Assert
        Assert.True(graph.HasEdge(id1, id2));
        Assert.Equal(1, graph.GetOutDegree(id1));
        Assert.Equal(1, graph.GetInDegree(id2));
    }

    [Fact]
    public void AddEdgeShouldThrowForSelfLoop()
    {
        // Arrange
        var graph = new Graph<TestNode>();
        var id = Guid.NewGuid();
        graph.AddNode(id, new TestNode { Name = "Node1" });

        // Act & Assert
        Assert.Throws<ArgumentException>(() => graph.AddEdge(id, id));
    }

    [Fact]
    public void AddEdgeShouldThrowForNonExistentNodes()
    {
        // Arrange
        var graph = new Graph<TestNode>();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => graph.AddEdge(id1, id2));
    }

    [Fact]
    public void AddEdgeShouldThrowWhenCreatingCycle()
    {
        // Arrange
        var graph = new Graph<TestNode>();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();

        graph.AddNode(id1, new TestNode { Name = "Node1" });
        graph.AddNode(id2, new TestNode { Name = "Node2" });
        graph.AddNode(id3, new TestNode { Name = "Node3" });
        graph.AddEdge(id1, id2);
        graph.AddEdge(id2, id3);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => graph.AddEdge(id3, id1));
    }

    [Fact]
    public void TryAddEdgeShouldReturnFalseWhenCreatingCycle()
    {
        // Arrange
        var graph = new Graph<TestNode>();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();

        graph.AddNode(id1, new TestNode { Name = "Node1" });
        graph.AddNode(id2, new TestNode { Name = "Node2" });
        graph.AddNode(id3, new TestNode { Name = "Node3" });
        graph.AddEdge(id1, id2);
        graph.AddEdge(id2, id3);

        // Act
        var result = graph.TryAddEdge(id3, id1);

        // Assert
        Assert.False(result);
        Assert.False(graph.HasEdge(id3, id1));
    }

    [Fact]
    public void RemoveEdgeShouldRemoveEdgeSuccessfully()
    {
        // Arrange
        var graph = new Graph<TestNode>();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        graph.AddNode(id1, new TestNode { Name = "Node1" });
        graph.AddNode(id2, new TestNode { Name = "Node2" });
        graph.AddEdge(id1, id2);

        // Act
        var result = graph.RemoveEdge(id1, id2);

        // Assert
        Assert.True(result);
        Assert.False(graph.HasEdge(id1, id2));
        Assert.Equal(0, graph.EdgeCount);
    }

    [Fact]
    public void GetSuccessorsShouldReturnCorrectNodes()
    {
        // Arrange
        var graph = new Graph<TestNode>();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();

        graph.AddNode(id1, new TestNode { Name = "Node1" });
        graph.AddNode(id2, new TestNode { Name = "Node2" });
        graph.AddNode(id3, new TestNode { Name = "Node3" });
        graph.AddEdge(id1, id2);
        graph.AddEdge(id1, id3);

        // Act
        var successors = graph.GetSuccessors(id1).ToList();

        // Assert
        Assert.Equal(2, successors.Count);
        Assert.Contains(successors, n => n.Name == "Node2");
        Assert.Contains(successors, n => n.Name == "Node3");
    }

    [Fact]
    public void GetPredecessorsShouldReturnCorrectNodes()
    {
        // Arrange
        var graph = new Graph<TestNode>();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();

        graph.AddNode(id1, new TestNode { Name = "Node1" });
        graph.AddNode(id2, new TestNode { Name = "Node2" });
        graph.AddNode(id3, new TestNode { Name = "Node3" });
        graph.AddEdge(id1, id3);
        graph.AddEdge(id2, id3);

        // Act
        var predecessors = graph.GetPredecessors(id3).ToList();

        // Assert
        Assert.Equal(2, predecessors.Count);
        Assert.Contains(predecessors, n => n.Name == "Node1");
        Assert.Contains(predecessors, n => n.Name == "Node2");
    }

    [Fact]
    public void PathExistsShouldDetectDirectPath()
    {
        // Arrange
        var graph = new Graph<TestNode>();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        graph.AddNode(id1, new TestNode { Name = "Node1" });
        graph.AddNode(id2, new TestNode { Name = "Node2" });
        graph.AddEdge(id1, id2);

        // Act & Assert
        Assert.True(graph.PathExists(id1, id2));
        Assert.False(graph.PathExists(id2, id1));
    }

    [Fact]
    public void PathExistsShouldDetectIndirectPath()
    {
        // Arrange
        var graph = new Graph<TestNode>();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();

        graph.AddNode(id1, new TestNode { Name = "Node1" });
        graph.AddNode(id2, new TestNode { Name = "Node2" });
        graph.AddNode(id3, new TestNode { Name = "Node3" });
        graph.AddEdge(id1, id2);
        graph.AddEdge(id2, id3);

        // Act & Assert
        Assert.True(graph.PathExists(id1, id3));
        Assert.False(graph.PathExists(id3, id1));
    }

    [Fact]
    public void TopologicalSortShouldReturnValidOrdering()
    {
        // Arrange
        var graph = new Graph<TestNode>();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();
        var id4 = Guid.NewGuid();

        graph.AddNode(id1, new TestNode { Name = "Node1" });
        graph.AddNode(id2, new TestNode { Name = "Node2" });
        graph.AddNode(id3, new TestNode { Name = "Node3" });
        graph.AddNode(id4, new TestNode { Name = "Node4" });
        
        // Create dependencies: 1 -> 2 -> 4, 1 -> 3 -> 4
        graph.AddEdge(id1, id2);
        graph.AddEdge(id1, id3);
        graph.AddEdge(id2, id4);
        graph.AddEdge(id3, id4);

        // Act
        var sorted = graph.TopologicalSort();

        // Assert
        Assert.Equal(4, sorted.Count);
        Assert.Equal(id1, sorted[0]);
        Assert.Equal(id4, sorted[3]);
        
        var sortedList = sorted.ToList();
        Assert.True(sortedList.IndexOf(id2) < sortedList.IndexOf(id4));
        Assert.True(sortedList.IndexOf(id3) < sortedList.IndexOf(id4));
    }

    [Fact]
    public void GetRootNodesShouldReturnNodesWithNoPredecessors()
    {
        // Arrange
        var graph = new Graph<TestNode>();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();

        graph.AddNode(id1, new TestNode { Name = "Node1" });
        graph.AddNode(id2, new TestNode { Name = "Node2" });
        graph.AddNode(id3, new TestNode { Name = "Node3" });
        graph.AddEdge(id1, id3);
        graph.AddEdge(id2, id3);

        // Act
        var roots = graph.GetRootNodes().ToList();

        // Assert
        Assert.Equal(2, roots.Count);
        Assert.Contains(id1, roots);
        Assert.Contains(id2, roots);
    }

    [Fact]
    public void GetLeafNodesShouldReturnNodesWithNoSuccessors()
    {
        // Arrange
        var graph = new Graph<TestNode>();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();

        graph.AddNode(id1, new TestNode { Name = "Node1" });
        graph.AddNode(id2, new TestNode { Name = "Node2" });
        graph.AddNode(id3, new TestNode { Name = "Node3" });
        graph.AddEdge(id1, id2);
        graph.AddEdge(id1, id3);

        // Act
        var leaves = graph.GetLeafNodes().ToList();

        // Assert
        Assert.Equal(2, leaves.Count);
        Assert.Contains(id2, leaves);
        Assert.Contains(id3, leaves);
    }

    [Fact]
    public void IsAcyclicShouldReturnTrueForDAG()
    {
        // Arrange
        var graph = new Graph<TestNode>();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();

        graph.AddNode(id1, new TestNode { Name = "Node1" });
        graph.AddNode(id2, new TestNode { Name = "Node2" });
        graph.AddNode(id3, new TestNode { Name = "Node3" });
        graph.AddEdge(id1, id2);
        graph.AddEdge(id2, id3);

        // Act & Assert
        Assert.True(graph.IsAcyclic());
    }

    [Fact]
    public void ClearShouldRemoveAllNodesAndEdges()
    {
        // Arrange
        var graph = new Graph<TestNode>();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        graph.AddNode(id1, new TestNode { Name = "Node1" });
        graph.AddNode(id2, new TestNode { Name = "Node2" });
        graph.AddEdge(id1, id2);

        // Act
        graph.Clear();

        // Assert
        Assert.Equal(0, graph.NodeCount);
        Assert.Equal(0, graph.EdgeCount);
    }

    [Fact]
    public void ComplexGraphShouldHandleMultipleOperations()
    {
        // Arrange
        var graph = new Graph<TestNode>();
        var ids = Enumerable.Range(0, 10).Select(_ => Guid.NewGuid()).ToList();

        foreach (var id in ids)
            graph.AddNode(id, new TestNode { Name = $"Node{ids.IndexOf(id)}" });

        // Create a diamond pattern: 0 -> 1,2 -> 3
        graph.AddEdge(ids[0], ids[1]);
        graph.AddEdge(ids[0], ids[2]);
        graph.AddEdge(ids[1], ids[3]);
        graph.AddEdge(ids[2], ids[3]);

        // Act
        var sorted = graph.TopologicalSort();
        var successorsOf0 = graph.GetSuccessors(ids[0]).ToList();
        var predecessorsOf3 = graph.GetPredecessors(ids[3]).ToList();

        // Assert
        Assert.Equal(10, graph.NodeCount);
        Assert.Equal(4, graph.EdgeCount);
        Assert.Equal(2, successorsOf0.Count);
        Assert.Equal(2, predecessorsOf3.Count);
        Assert.True(graph.PathExists(ids[0], ids[3]));
        
        
        var sortedList = sorted.ToList();
        Assert.True(sortedList.IndexOf(ids[0]) < sortedList.IndexOf(ids[3]));
    }

    private class TestNode : IExecutableNode
    {
        public string Name { get; set; } = string.Empty;
        public Guid Id { get; }
        public Type InputType { get; }
        public Type OutputType { get; }
        public Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}