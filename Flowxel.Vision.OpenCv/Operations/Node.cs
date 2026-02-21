using Flowxel.Processing;
using Flowxel.Vision;
using Flowxel.Vision.OpenCv;

namespace Flowxel.Vision.OpenCv.Operations;

public abstract class Node<TIn, TOut> : ExecutionNode<TIn, TOut> where TOut : notnull
{
    protected Node(ResourcePool pool, Graph<IExecutableNode> graph, IVisionBackend? vision = null)
        : base(pool, graph)
    {
        Vision = vision ?? OpenCvVisionBackend.Shared;
    }

    protected IVisionBackend Vision { get; }
}
