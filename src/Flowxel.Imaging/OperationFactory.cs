namespace Flowxel.Imaging;

public interface IOperationFactory
{
    IOperation CreateOperation(string name);
    IOperation CreateOperation(string name, Type expectedInputType);
}

public class OperationFactory : IOperationFactory
{
    private readonly OperationCatalog _catalog;

    public OperationFactory(OperationCatalog catalog)
    {
        _catalog = catalog;
    }

    public IOperation CreateOperation(string name)
        => _catalog.CreateOperation(name);

    public IOperation CreateOperation(string name, Type expectedInputType)
        => _catalog.CreateOperation(name, expectedInputType);
}