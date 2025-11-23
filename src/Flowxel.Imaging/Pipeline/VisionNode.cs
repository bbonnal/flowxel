using Flowxel.Graph;
using OpenCvSharp;

namespace Flowxel.Imaging.Pipeline;

public class VisionNode(IOperationFactory factory, OperationCatalog catalog) : IExecutableNode
{
    private readonly IOperationFactory _factory = factory ?? throw new ArgumentNullException(nameof(factory));

    private readonly OperationCatalog
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog)); // Need this to query output types

    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; set; } = "Unnamed";
    public string OperationName { get; set; } = "";
    public Dictionary<string, object> Parameters { get; set; } = new();

    public required IVisionPort Input { get; set; } = VisionPort.None;
    public IVisionPort Output { get; private set; } = VisionPort.None;

    public bool IsExecuted { get; private set; }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(OperationName))
                throw new InvalidOperationException($"Node '{Name}' (Id: {Id}) has no OperationName");

            var operation = _factory.CreateOperation(OperationName, Input.ExpectedType);
            var inputValue = ExtractInputValue();
            
            var result = await Task.Run(async () =>
            {
                if (inputValue != null) return await operation.ExecuteAsync(inputValue, Parameters, ct);
                return null;
            }, ct);

            Output = WrapOutput(result);

            IsExecuted = true;

            // Publish output to the shared pool
            var context = VisionPipelineContext.Current;
            if (context != null)
            {
                context.Outputs[Id] = Output;
                context.Results.TryAdd(Id, this);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Node '{Name}' (Id: {Id}, Operation: {OperationName}) failed during execution",
                ex);
        }
    }

    /// <summary>
    /// Gets the expected output type by querying operation metadata
    /// </summary>
    public Type GetExpectedOutputType()
    {
        if (string.IsNullOrWhiteSpace(OperationName))
            return typeof(object);

        try
        {
            var (_, outputType) = _catalog.GetOperationSignature(OperationName);
            return outputType;
        }
        catch
        {
            return typeof(object);
        }
    }

    private object? ExtractInputValue()
    {
        return Input switch
        {
            NonePort => None.Value,
            PortReference pr => pr.GetValue(),
            MultiplePortReference mpr => mpr.GetValue(), // ← ADD THIS LINE
            SinglePort<Mat> sp => sp.Value ?? throw new InvalidOperationException("Mat input is null"),
            MultiplePort<Mat> mp => mp.Values.ToArray(),
            SinglePort<object> sp => sp.Value ?? throw new InvalidOperationException("Object input is null"),
            MultiplePort<object> mp => mp.Values.ToArray(),
            _ => throw new NotSupportedException($"Unsupported input port type: {Input.GetType().Name}")
        };
    }

    private static IVisionPort WrapOutput(object? result)
    {
        return result switch
        {
            null => VisionPort.None,
            Mat mat => VisionPort.From(mat),
            Mat[] mats => VisionPort.From(mats),
            Point[][] contours => VisionPort.From(contours),
            double d => VisionPort.From(d),
            _ => VisionPort.From(result)
        };
    }
}