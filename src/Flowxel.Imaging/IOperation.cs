using OpenCvSharp;

namespace Flowxel.Imaging;

public interface IOperation
{
    ValueTask<object> ExecuteAsync(
        object input,
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken ct = default);
}

// Strongly-typed interface for compile-time safety
public interface IOperation<TIn, TOut> : IOperation
{
}

// Base class that bridges both worlds
public abstract class Operation<TIn, TOut> : IOperation<TIn, TOut>
{
    public Type InputType => typeof(TIn);
    public Type OutputType => typeof(TOut);

    // Type-safe method (used by strongly-typed callers)
    public abstract ValueTask<TOut> ExecuteAsync(
        TIn input,
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken ct = default);

    // Type-erased method (used by graph execution engine)
    async ValueTask<object> IOperation.ExecuteAsync(
        object input,
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken ct)
    {
        var typedInput = (TIn)input;
        var result = await ExecuteAsync(typedInput, parameters, ct);
        return result!;
    }
}