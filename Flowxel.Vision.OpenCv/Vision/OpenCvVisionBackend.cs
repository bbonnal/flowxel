using OpenCvSharp;
using Flowxel.Vision;

namespace Flowxel.Vision.OpenCv;

public sealed class OpenCvVisionBackend : IVisionBackend
{
    public static OpenCvVisionBackend Shared { get; } = new();

    private OpenCvVisionBackend()
    {
    }

    public string Name => "OpenCV";

    public bool Supports(VisionFeature feature) => true;

    public object LoadGrayscale(string path)
        => Cv2.ImRead(path, ImreadModes.Grayscale);

    public void Save(string path, object image)
        => Cv2.ImWrite(path, (Mat)image);

    public object GaussianBlur(object source, int kernelSize, double sigma)
    {
        var output = new Mat();
        Cv2.GaussianBlur((Mat)source, output, new Size(kernelSize, kernelSize), sigma);
        return output;
    }

    public object Subtract(object left, object right)
    {
        var output = new Mat();
        Cv2.Subtract((Mat)left, (Mat)right, output);
        return output;
    }
}
