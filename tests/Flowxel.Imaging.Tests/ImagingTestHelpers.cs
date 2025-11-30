using System.Diagnostics;
using OpenCvSharp;

namespace Flowxel.Imaging.Tests;

public static class ImagingTestHelpers
{
    private static readonly string TempDir = Path.Combine(Path.GetTempPath(), "OpenCvImageTests");
    
    public static Mat GenerateRandomMat(int width, int height, MatType type)
    {
        var mat = new Mat(height, width, type);
        var rand = new Random(123);
        var idx = mat.GetGenericIndexer<byte>();
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
            idx[y, x] = (byte)rand.Next(0, 256);
        return mat;
    }
    
    public static string SaveMat(Mat srcMat, string name)
    {
        Directory.CreateDirectory(TempDir);
        
        var filePath = Path.Combine(TempDir, name);

        Cv2.ImWrite(filePath, srcMat);
        
        return filePath;

    }

    
    public static void SaveAndOpenMat(Mat srcMat, string name)
    {
        Directory.CreateDirectory(TempDir);

        var filePath = Path.Combine(TempDir, name);

        Cv2.ImWrite(filePath, srcMat);
        Assert.True(File.Exists(filePath));

        try
        {
            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            // ignored
        }
    }
}