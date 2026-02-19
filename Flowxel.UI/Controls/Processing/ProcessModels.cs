
namespace Flowxel.UI.Controls.Processing;

public enum ProcessPortDirection
{
    Input,
    Output
}

public enum ProcessBindingSourceKind
{
    CanvasShape,
    OperationOutput,
    ResourcePool
}

public enum ResourceValueKind
{
    Image,
    NumericArray,
    ShapeProperties,
    GraphSeries,
    LineCoordinates,
    Unknown
}

public sealed class ProcessPortDescriptor
{
    public string Key { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string TypeName { get; init; } = string.Empty;

    public ProcessPortDirection Direction { get; init; }

    public string DisplayLabel => $"{Direction}: {Name} ({TypeName})";

    public override string ToString() => DisplayLabel;
}

public sealed class ProcessNodeDescriptor
{
    public string Id { get; init; } = Guid.NewGuid().ToString();

    public string Name { get; init; } = string.Empty;

    public string OperationType { get; init; } = string.Empty;

    public IReadOnlyList<ProcessPortDescriptor> Inputs { get; init; } = [];

    public IReadOnlyList<ProcessPortDescriptor> Outputs { get; init; } = [];

    public string DisplayLabel => $"{Name} [{OperationType}]";

    public override string ToString() => DisplayLabel;
}

public sealed class ProcessLinkDescriptor
{
    public string FromNodeId { get; init; } = string.Empty;

    public string FromPortKey { get; init; } = string.Empty;

    public string ToNodeId { get; init; } = string.Empty;

    public string ToPortKey { get; init; } = string.Empty;

    public string DisplayLabel => $"{FromNodeId}.{FromPortKey} -> {ToNodeId}.{ToPortKey}";

    public override string ToString() => DisplayLabel;
}

public sealed class ProcessBindingDescriptor
{
    public string Id { get; init; } = Guid.NewGuid().ToString();

    public string NodeId { get; init; } = string.Empty;

    public string PortKey { get; init; } = string.Empty;

    public ProcessPortDirection PortDirection { get; init; }

    public ProcessBindingSourceKind SourceKind { get; init; }

    public string SourceReference { get; init; } = string.Empty;

    public string Label { get; init; } = string.Empty;
}

public sealed class ProcessBindingSourceDescriptor
{
    public string Label { get; init; } = string.Empty;

    public ProcessBindingSourceKind Kind { get; init; }

    public string Reference { get; init; } = string.Empty;

    public override string ToString() => Label;
}

public abstract class ResourceViewData;

public sealed class ImageResourceViewData : ResourceViewData
{
    public global::Avalonia.Media.Imaging.Bitmap? Bitmap { get; init; }

    public string? Path { get; init; }
}

public sealed class NumericArrayResourceViewData : ResourceViewData
{
    public IReadOnlyList<double> Values { get; init; } = [];
}

public sealed class GraphSeriesResourceViewData : ResourceViewData
{
    public IReadOnlyList<double> Values { get; init; } = [];
}

public sealed class ShapePropertyResourceViewData : ResourceViewData
{
    public string ShapeType { get; init; } = string.Empty;

    public IReadOnlyList<KeyValuePair<string, string>> Properties { get; init; } = [];
}

public sealed class LineCoordinateEntry
{
    public double StartX { get; init; }

    public double StartY { get; init; }

    public double EndX { get; init; }

    public double EndY { get; init; }

    public double Length { get; init; }

    public string DisplayLabel =>
        $"({StartX:0.###}, {StartY:0.###}) -> ({EndX:0.###}, {EndY:0.###}) | L={Length:0.###}";
}

public sealed class LineCoordinatesResourceViewData : ResourceViewData
{
    public IReadOnlyList<LineCoordinateEntry> Lines { get; init; } = [];
}

public sealed class ResourceEntryDescriptor
{
    public string Key { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string TypeName { get; init; } = string.Empty;

    public string ProducerNode { get; init; } = string.Empty;

    public string Preview { get; init; } = string.Empty;

    public ResourceValueKind ValueKind { get; init; }

    public ResourceViewData? Value { get; init; }
}
