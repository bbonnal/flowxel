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

    public static Mat GenerateBlackSquareMat(
        int imageWidth,
        int imageHeight,
        int squareWidth,
        int squareHeight,
        int centerX,
        int centerY,
        double angle,
        MatType type)
    {
        var mat = new Mat(imageHeight, imageWidth, type, Scalar.All(255));

        // Use RotatedRect to calculate corners accounting for the angle
        var center = new Point2f(centerX, centerY);
        var size = new Size2f(squareWidth, squareHeight);
        var rotatedRect = new RotatedRect(center, size, (float)angle);

        var vertices = rotatedRect.Points()
            .Select(p => new Point((int)p.X, (int)p.Y))
            .ToArray();

        Cv2.FillConvexPoly(mat, vertices, Scalar.All(0));

        return mat;
    }

    public static Mat GenerateBlackEllipseMat(
        int imageWidth,
        int imageHeight,
        int ellipseWidth,
        int ellipseHeight,
        int centerX,
        int centerY,
        double angle,
        MatType type)
    {
        var mat = new Mat(imageHeight, imageWidth, type, Scalar.All(255));

        Cv2.Ellipse(
            mat,
            new Point(centerX, centerY),
            new Size(ellipseWidth / 2, ellipseHeight / 2),
            angle,
            0,
            360,
            Scalar.All(0),
            -1);

        return mat;
    }

    public static Mat GenerateBlackTriangleMat(
        int imageWidth,
        int imageHeight,
        int radius,
        int centerX,
        int centerY,
        double angle,
        MatType type)
    {
        var mat = new Mat(imageHeight, imageWidth, type, Scalar.All(255));

        var points = new Point[3];
        for (var i = 0; i < 3; i++)
        {
            // Calculate vertices for an equilateral triangle
            // -90 degrees aligns the first vertex to the top (0 rotation)
            var theta = (angle - 90 + i * 120) * Math.PI / 180.0;
            points[i] = new Point(
                centerX + radius * Math.Cos(theta),
                centerY + radius * Math.Sin(theta));
        }

        Cv2.FillConvexPoly(mat, points, Scalar.All(0));
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
        catch (Exception)
        {
            // ignored
        }
    }
}
