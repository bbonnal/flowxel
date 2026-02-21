using Flowxel.Core.Geometry.Primitives;
using Flowxel.Core.Geometry.Shapes;
using Flowxel.Processing;
using Flowxel.Vision;
using OpenCvSharp;

namespace Flowxel.Vision.OpenCv.Operations.Extractions;

public class ExtractCircleOperation(ResourcePool pool, Graph<IExecutableNode> graph, IVisionBackend? vision = null) : Node<Mat, Circle>(pool, graph, vision)
{
    protected override Circle ExecuteInternal(
        IReadOnlyList<Mat> inputs, 
        IReadOnlyDictionary<string, object> parameters, 
        CancellationToken ct)
    {
        var output = new Circle();
        
        var circle = Cv2.HoughCircles(
            inputs[0], 
            HoughModes.GradientAlt,
            1, 
            40,
            80,
            0.7,
            10,
            40);

        if (circle.Length == 0) return output;
        
        output.Pose = new Pose(new Vector((double)circle[0].Center.X, (double)circle[0].Center.Y), new Vector(1, 1));
        output.Radius = circle[0].Radius;
        
        return output;
    }
}