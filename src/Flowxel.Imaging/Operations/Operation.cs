namespace Flowxel.Imaging.Operations;

public abstract class Operation<TIn, TOut>
{
    public abstract Task<TOut> ExecuteAsync(
        TIn input,
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken ct);
}

public abstract class SourceOperation<TOut>
{
    public abstract Task<TOut> ExecuteAsync(
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken ct);
}

public abstract class SinkOperation<TIn>
{
    public abstract Task ExecuteAsync(
        TIn input,
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken ct);
}

public abstract class CombineOperation<TIn, TOut>
{
    public abstract Task<TOut> ExecuteAsync(
        TIn[] inputs,
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken ct);
}