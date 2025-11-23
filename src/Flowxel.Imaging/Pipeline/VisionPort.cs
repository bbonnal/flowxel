using OpenCvSharp;

namespace Flowxel.Imaging.Pipeline;

public interface IVisionPort
{
    Type ExpectedType { get; }
    object? GetValue(); // Lazy resolution
}

public readonly struct None
{
    public static readonly None Value = new();
    public None() { }
    public override string ToString() => "None";
}


public class  NonePort : IVisionPort
{
    public NonePort() { }
    public static NonePort Instance { get; } = new();
    public Type ExpectedType => typeof(None);
    public object? GetValue()
    {
        throw new NotImplementedException();
    }

}

public sealed record SinglePort<T>(T Value) : IVisionPort
{
    public Type ExpectedType => typeof(T);
    public object? GetValue()
    {
        throw new NotImplementedException();
    }

}

public sealed record MultiplePort<T>(IReadOnlyList<T> Values) : IVisionPort
{
    public Type ExpectedType => typeof(T[]);
    public object? GetValue()
    {
        throw new NotImplementedException();
    }

}

public class PortReference(Guid sourceNodeId, Type expectedType) : IVisionPort
{
    public Type ExpectedType => expectedType;

    public object? GetValue()
    {
        var context = VisionPipelineContext.Current 
                      ?? throw new InvalidOperationException("No active VisionPipelineContext");

        if (!context.Outputs.TryGetValue(sourceNodeId, out var sourcePort))
            throw new InvalidOperationException(
                $"Output from node {sourceNodeId} not found in pipeline context. Node may not have executed yet.");

        // Extract the actual value from the source port
        return sourcePort switch
        {
            NonePort => None.Value,
            SinglePort<Mat> sp => sp.Value,        // ← Returns Mat
            MultiplePort<Mat> mp => mp.Values.ToArray(),  // ← Returns Mat[]
            SinglePort<double> sp => sp.Value,     // ← Returns double
            SinglePort<object> sp => sp.Value,     // ← Returns object
            MultiplePort<object> mp => mp.Values.ToArray(),
            _ => throw new NotSupportedException($"Unsupported port type: {sourcePort.GetType().Name}")
        };
    }
}

/// <summary>
/// A port that references multiple nodes' outputs via the shared pool
/// </summary>
public class MultiplePortReference : IVisionPort
{
    private readonly Guid[] _sourceNodeIds;
    private readonly Type _expectedType;

    public MultiplePortReference(Guid[] sourceNodeIds, Type expectedType)
    {
        _sourceNodeIds = sourceNodeIds ?? throw new ArgumentNullException(nameof(sourceNodeIds));
        _expectedType = expectedType;
    }

    public Type ExpectedType => _expectedType.MakeArrayType(); // Mat[] for example

    public object? GetValue()
    {
        var context = VisionPipelineContext.Current 
                      ?? throw new InvalidOperationException("No active VisionPipelineContext");

        var results = new List<object?>();
        
        foreach (var nodeId in _sourceNodeIds)
        {
            if (!context.Outputs.TryGetValue(nodeId, out var sourcePort))
                throw new InvalidOperationException(
                    $"Output from node {nodeId} not found in pipeline context.");

            var value = sourcePort switch
            {
                NonePort => None.Value,
                SinglePort<Mat> sp => sp.Value,
                MultiplePort<Mat> mp => mp.Values.ToArray(),
                SinglePort<object> sp => sp.Value,
                MultiplePort<object> mp => mp.Values.ToArray(),
                _ => throw new NotSupportedException($"Unsupported port type: {sourcePort.GetType().Name}")
            };
            
            results.Add(value);
        }

        // Convert to typed array if possible
        if (_expectedType == typeof(Mat) && results.All(r => r is Mat))
            return results.Cast<Mat>().ToArray();
        
        return results.ToArray();
    }
}

public static class VisionPort
{
    public static IVisionPort None => new NonePort();
    
    /// <summary>
    /// Creates a reference to a single node's output
    /// </summary>
    public static IVisionPort From(VisionNode node)
    {
        var expectedType = node.GetExpectedOutputType();
        return new PortReference(node.Id, expectedType);
    }
    
    /// <summary>
    /// Creates a reference to multiple nodes' outputs (for combining operations)
    /// </summary>
    public static IVisionPort From(params VisionNode[] nodes)
    {
        if (nodes == null || nodes.Length == 0)
            throw new ArgumentException("At least one node must be provided", nameof(nodes));

        // Assume all nodes have the same output type (validate if needed)
        var expectedType = nodes[0].GetExpectedOutputType();
        var nodeIds = nodes.Select(n => n.Id).ToArray();
        
        return new MultiplePortReference(nodeIds, expectedType);
    }
    
    public static IVisionPort From(Mat value) => new SinglePort<Mat>(value);
    public static IVisionPort From(Mat[] values) => new MultiplePort<Mat>(values);
    public static IVisionPort From(Point[][] contours) => new MultiplePort<Point[]>(contours);
    public static IVisionPort From(double value) => new SinglePort<double>(value);
    public static IVisionPort From(object value) => new SinglePort<object>(value);
}