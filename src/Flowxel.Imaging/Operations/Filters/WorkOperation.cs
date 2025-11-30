using Flowxel.Graph;
using OpenCvSharp;

namespace Flowxel.Imaging.Operations.Filters;

public class WorkOperation(ResourcePool pool, Graph<IExecutableNode> graph) : Node<Mat, Mat>(pool, graph)
{
    private readonly Random _rnd = new();


    public override Mat ExecuteInternal(IReadOnlyList<Mat> inputs, IReadOnlyDictionary<string, object> parameters,
        CancellationToken ct)
    {
        var src = inputs[0];
        var result = new Mat();

        // This chain is CPU-heavy but reuses memory wisely
        var gray = src.Type() == MatType.CV_8UC1 ? src : src.CvtColor(ColorConversionCodes.BGR2GRAY);
        using var sobel = new Mat();
        using var canny = new Mat();
        using var morph = new Mat();

        Cv2.Sobel(gray, sobel, MatType.CV_16S, 1, 1, 3);
        Cv2.ConvertScaleAbs(sobel, result, 1, 0);
        Cv2.Canny(result, canny, 50, 200);
        Cv2.MorphologyEx(canny, morph, MorphTypes.Dilate, Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3)),
            iterations: 2);

        // Add fake CPU load to simulate real work (remove in prod)
        var dummy = 0.0;
        for (int i = 0; i < 50_000; i++)
            dummy += Math.Sin(i * 0.001) * Math.Cos(i * 0.002);

        // Final output
        morph.ConvertTo(result, MatType.CV_8UC1);
        return result;
    }
}