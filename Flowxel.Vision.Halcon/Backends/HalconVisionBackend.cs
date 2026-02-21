using Flowxel.Vision;

namespace Flowxel.Vision.Halcon;

public sealed class HalconVisionBackend : IVisionBackend
{
    public string Name => "Halcon";

    public bool Supports(VisionFeature feature) => false;

    public object LoadGrayscale(string path)
        => throw FeatureNotImplemented(VisionFeature.LoadGrayscale);

    public void Save(string path, object image)
        => throw FeatureNotImplemented(VisionFeature.Save);

    public object GaussianBlur(object source, int kernelSize, double sigma)
        => throw FeatureNotImplemented(VisionFeature.GaussianBlur);

    public object Subtract(object left, object right)
        => throw FeatureNotImplemented(VisionFeature.Subtract);

    private static NotSupportedException FeatureNotImplemented(VisionFeature feature)
        => new($"Halcon backend does not implement feature '{feature}' yet.");
}
