using Flowxel.Core.Geometry.Primitives;
using Flowxel.Core.Geometry.Shapes;
using Flowxel.Graph;
using OpenCvSharp;

namespace Flowxel.Imaging.Operations.Extractions;

public class ExtractContourOperation(ResourcePool pool, Graph<IExecutableNode> graph) : Node<Mat, OpenCvSharp.Point[][]>(pool, graph)
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