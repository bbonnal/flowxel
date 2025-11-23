namespace Flowxel.Imaging;

public class OperationCatalog
{
    private readonly Dictionary<string, Type> _operations = new(StringComparer.OrdinalIgnoreCase);

    public void Register<TImplementation>() 
        where TImplementation : IOperation
    {
        var name = typeof(TImplementation).Name.Replace("Operation", "", StringComparison.OrdinalIgnoreCase);
        _operations[name] = typeof(TImplementation);
    }

    public void Register(string name, Type implementationType)
    {
        if (!typeof(IOperation).IsAssignableFrom(implementationType))
            throw new ArgumentException($"{implementationType} must implement IOperation");

        _operations[name] = implementationType;
    }

    public IOperation CreateOperation(string name)
    {
        var type = GetOperationType(name);
        return (IOperation)(Activator.CreateInstance(type)
                   ?? throw new InvalidOperationException($"Failed to create {type}"));
    }

    public IOperation CreateOperation(string name, Type expectedInputType)
    {
        var type = GetOperationType(name);
        
        // Validate input type compatibility
        var (inputType, _) = GetOperationSignature(type);
        
        if (!IsCompatibleInput(expectedInputType, inputType))
            throw new InvalidOperationException(
                $"Operation '{name}' expects input type {inputType.Name}, but got {expectedInputType.Name}");

        return (IOperation)(Activator.CreateInstance(type)
                   ?? throw new InvalidOperationException($"Failed to create {type}"));
    }

    public Type GetOperationType(string name)
    {
        return _operations.GetValueOrDefault(name)
               ?? throw new InvalidOperationException(
                   $"Operation '{name}' not found. Available: {string.Join(", ", _operations.Keys)}");
    }

    public (Type InputType, Type OutputType) GetOperationSignature(string name)
    {
        var type = GetOperationType(name);
        return GetOperationSignature(type);
    }

    private static (Type InputType, Type OutputType) GetOperationSignature(Type operationType)
    {
        var opInterface = operationType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IOperation<,>))
            ?? throw new InvalidOperationException($"{operationType} doesn't implement IOperation<,>");

        var args = opInterface.GetGenericArguments();
        return (args[0], args[1]);
    }

    private static bool IsCompatibleInput(Type actualType, Type expectedType)
    {
        return expectedType.IsAssignableFrom(actualType) || 
               actualType == typeof(object); // Runtime polymorphism
    }

    public IEnumerable<string> Available => _operations.Keys;
}