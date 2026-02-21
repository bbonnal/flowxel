using Flowxel.Core.Geometry.Primitives;
using Flowxel.Core.Geometry.Shapes;
using Flowxel.Processing;
using Flowxel.Vision;
using OpenCvSharp;

namespace Flowxel.Vision.OpenCv.Operations.Extractions;

public class ExtractContourOperation(ResourcePool pool, Graph<IExecutableNode> graph, IVisionBackend? vision = null) : Node<Mat, OpenCvSharp.Point[][]>(pool, graph, vision)
{
    protected override OpenCvSharp.Point[][] ExecuteInternal(
        IReadOnlyList<Mat> inputs, 
        IReadOnlyDictionary<string, object> parameters, 
        CancellationToken ct)
    {
        
        var contours = Cv2.FindContoursAsArray(
            inputs[0], 
            RetrievalModes.CComp,
            ContourApproximationModes.ApproxSimple);

        
        return contours;
    }
}