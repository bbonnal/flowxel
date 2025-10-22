using System.Diagnostics;
using OpenCvSharp;
using Xunit.Abstractions;

namespace Flowxel.Imaging.Tests;

public class BasicTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly string _tempDir;

    public BasicTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _tempDir = Path.Combine(Path.GetTempPath(), "OpenCvImageTests");
        Directory.CreateDirectory(_tempDir);
    }

    private static Mat GenerateRandomMat()
    {
        const int width = 256;
        const int height = 256;
        var mat = new Mat(height, width, MatType.CV_8UC1);
        var rand = new Random(123);
        var idx = mat.GetGenericIndexer<byte>();
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
            idx[y, x] = (byte)rand.Next(0, 256);
        return mat;
    }

    [Fact]
    public Task SaveRandomMat()
    {
        using var srcMat = GenerateRandomMat();
        var filePath = Path.Combine(_tempDir, "random.png");

        Cv2.ImWrite(filePath, srcMat);
        Assert.True(File.Exists(filePath));

        try
        {
            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _testOutputHelper.WriteLine($"Failed to open image: {ex.Message}");
        }

        return Task.CompletedTask;
    }
}