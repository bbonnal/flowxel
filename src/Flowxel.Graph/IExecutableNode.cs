namespace Flowxel.Graph;

/// <summary>
/// Represents a node that can be executed asynchronously.
/// </summary>
public interface IExecutableNode
{
    /// <summary>
    /// Gets the unique identifier for this node.
    /// </summary>
    Guid Id { get; }
    
    Type InputType { get; }
    Type OutputType { get; }

    /// <summary>
    /// Executes the node's task asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task Execute(CancellationToken cancellationToken = default);
}
