namespace Flowxel.Vision;

public interface IVisionBackend
{
    string Name { get; }

    bool Supports(VisionFeature feature);

    object LoadGrayscale(string path);
    void Save(string path, object image);
    object GaussianBlur(object source, int kernelSize, double sigma);
    object Subtract(object left, object right);
}

public enum VisionFeature
{
    LoadGrayscale,
    Save,
    GaussianBlur,
    Subtract,
}
