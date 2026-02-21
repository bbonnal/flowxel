using System.Collections.Concurrent;

namespace Flowxel.Processing;

public readonly record struct ResourcePort(Guid NodeId, string PortKey);

public class ResourcePool
{
    private readonly ConcurrentDictionary<ResourcePort, object> _data = new();

    public void Set<T>(Guid nodeId, string portKey, T value) where T : notnull
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(portKey);
        _data[new ResourcePort(nodeId, portKey)] = value;
    }

    public void Set<T>(Guid nodeId, T value) where T : notnull
    {
        Set(nodeId, GraphPorts.DefaultOutput, value);
    }

    public bool TryGet<T>(Guid nodeId, string portKey, out T? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(portKey);

        if (_data.TryGetValue(new ResourcePort(nodeId, portKey), out var boxed) && boxed is T typed)
        {
            value = typed;
            return true;
        }

        value = default;
        return false;
    }

    public bool TryGet<T>(Guid nodeId, out T? value)
    {
        return TryGet(nodeId, GraphPorts.DefaultOutput, out value);
    }

    public T Get<T>(Guid nodeId, string portKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(portKey);
        var key = new ResourcePort(nodeId, portKey);

        if (!_data.TryGetValue(key, out var value))
            throw new KeyNotFoundException($"No output from node {nodeId} on port '{portKey}'.");

        if (value is not T typed)
        {
            throw new InvalidCastException(
                $"Node {nodeId} on port '{portKey}' produced {value.GetType().Name}, expected {typeof(T).Name}");
        }

        return typed;
    }

    public T Get<T>(Guid nodeId)
    {
        return Get<T>(nodeId, GraphPorts.DefaultOutput);
    }
}
