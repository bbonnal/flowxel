namespace Flowxel.Graph.Tests;

/// <summary>
/// A simple implementation of an executable node.
/// </summary>
public class ExecutableNode<TIn, TOut>: IExecutableNode
{
    /// <summary>
    /// Gets the unique identifier for this node.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the name of the node.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    public Type InputType => typeof(TIn);
    public Type OutputType => typeof(TOut);

    /// <summary>
    /// Gets or sets the function to execute asynchronously.
    /// </summary>
    public Func<CancellationToken, Task>? Execute { get; set; }

    /// <summary>
    /// Executes the node's task asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (Execute != null)
            await Execute(cancellationToken);
        else
            await Task.CompletedTask;
    }
}