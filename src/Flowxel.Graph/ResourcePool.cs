namespace Flowxel.Graph;

public class ResourcePool
{
    private readonly Dictionary<Guid, object> _data = new();

    public void Set<T>(Guid nodeId, T value) where T : notnull
    {
        _data[nodeId] = value;
    }

    public T Get<T>(Guid nodeId)
    {
        if (!_data.TryGetValue(nodeId, out var value))
            throw new KeyNotFoundException($"No output from node {nodeId}");

        if (value is not T typed)
            throw new InvalidCastException(
                $"Node {nodeId} produced {value.GetType().Name}, expected {typeof(T).Name}");

        return typed;
    }
}