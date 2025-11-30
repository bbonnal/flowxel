namespace Flowxel.Imaging.Operations;

public readonly struct Empty { }

public abstract class Operation<TIn, TOut>
{
    public abstract TOut Execute(
        IReadOnlyList<TIn> inputs,                
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken ct);
}
