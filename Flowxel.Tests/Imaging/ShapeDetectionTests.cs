using Flowxel.Core.Geometry.Shapes;
using Flowxel.Processing;
using Flowxel.Vision.OpenCv.Operations.Extractions;
using Flowxel.Vision.OpenCv.Operations.Filters;
using Flowxel.Vision.OpenCv.Operations.IO;
using Flowxel.Vision.OpenCv.Operations.Transforms;
using Microsoft.Extensions.DependencyInjection;
using OpenCvSharp;
using Point = OpenCvSharp.Point;

namespace Flowxel.Imaging.Tests;

public class ShapeDetectionTests
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "FlowxelTests");


    [Fact]
    public async Task ProduceSquareInMat()
    {
        // Arrange
        Directory.CreateDirectory(_tempDir);

        using var srcMat = ImagingTestHelpers.GenerateBlackSquareMat(
            256,
            256,
            24,
            24,
            24,
            24,
            45,
            MatType.CV_8UC1);
        var outputPath = Path.Combine(_tempDir, "output_unsharp.png");

        ImagingTestHelpers.SaveAndOpenMat(srcMat, outputPath);
    }

    [Fact]
    public async Task ProduceEllipseInMat()
    {
        // Arrange
        Directory.CreateDirectory(_tempDir);

        using var srcMat = ImagingTestHelpers.GenerateBlackEllipseMat(
            256,
            256,
            24,
            24,
            24,
            24,
            45,
            MatType.CV_8UC1);
        var outputPath = Path.Combine(_tempDir, "output_unsharp.png");

        ImagingTestHelpers.SaveAndOpenMat(srcMat, outputPath);
    }

    [Fact]
    public async Task DetectCircleInMat()
    {
        Directory.CreateDirectory(_tempDir);

        using var srcMat = ImagingTestHelpers.GenerateBlackEllipseMat(
            256,
            256,
            24,
            24,
            24,
            24,
            45,
            MatType.CV_8UC1);

        var inputPath = Path.Combine(_tempDir, "ellipseInMat.png");
        var outputPath = Path.Combine(_tempDir, "ellipseFoundInMat.png");

        Cv2.ImWrite(inputPath, srcMat);

        // Setup DI container
        var services = new ServiceCollection();

        // Register core services
        services.AddSingleton<ResourcePool>();
        services.AddSingleton<Graph<IExecutableNode>>();

        // Register all node types (transient = new instance each time we resolve)
        services.AddTransient<LoadOperation>();
        services.AddTransient<ExtractCircleOperation>();
        services.AddTransient<SaveOperation>();

        var provider = services.BuildServiceProvider();

        // Resolve services
        var graph = provider.GetRequiredService<Graph<IExecutableNode>>();
        var pool = provider.GetRequiredService<ResourcePool>();

        // Resolve nodes via DI (this triggers constructor injection of pool + graph)
        var load = provider.GetRequiredService<LoadOperation>();
        load.Parameters["Path"] = inputPath;

        var findEllipse = provider.GetRequiredService<ExtractCircleOperation>();


        // Build the graph topology
        graph.AddNode(load);
        graph.AddNode(findEllipse);

        graph.Connect(load, findEllipse);

        // Act
        await graph.ExecuteAsync(CancellationToken.None);

        // Assert
        var result = pool.Get<Circle>(findEllipse.Id);

        Console.WriteLine($"Found circle at ({result.Pose.Position.X}, {result.Pose.Position.Y}) with radius {result.Radius}");
        
        // Convert to BGR so we can draw a colored overlay
        using var outputMat = new Mat();
        Cv2.CvtColor(srcMat, outputMat, ColorConversionCodes.GRAY2BGR);

        Cv2.Circle(outputMat, new Point(result.Pose.Position.X, result.Pose.Position.Y), (int)result.Radius, Scalar.Red,
            thickness: 2);


        // Save for visual inspection (great during dev/debugging)
        ImagingTestHelpers.SaveAndOpenMat(outputMat, outputPath);
    }
    
    
    
    [Fact]
    public async Task DetectContoursInMat()
    {
        Directory.CreateDirectory(_tempDir);

        using var srcMat = ImagingTestHelpers.GenerateBlackEllipseMat(
            256,
            256,
            24,
            24,
            24,
            24,
            45,
            MatType.CV_8UC1);

        var inputPath = Path.Combine(_tempDir, "ellipseInMat.png");
        var outputPath = Path.Combine(_tempDir, "ellipseFoundInMat.png");

        Cv2.ImWrite(inputPath, srcMat);

        // Setup DI container
        var services = new ServiceCollection();

        // Register core services
        services.AddSingleton<ResourcePool>();
        services.AddSingleton<Graph<IExecutableNode>>();

        // Register all node types (transient = new instance each time we resolve)
        services.AddTransient<LoadOperation>();
        services.AddTransient<ExtractContourOperation>();
        services.AddTransient<SaveOperation>();

        var provider = services.BuildServiceProvider();

        // Resolve services
        var graph = provider.GetRequiredService<Graph<IExecutableNode>>();
        var pool = provider.GetRequiredService<ResourcePool>();

        // Resolve nodes via DI (this triggers constructor injection of pool + graph)
        var load = provider.GetRequiredService<LoadOperation>();
        load.Parameters["Path"] = inputPath;

        var findContour = provider.GetRequiredService<ExtractContourOperation>();


        // Build the graph topology
        graph.AddNode(load);
        graph.AddNode(findContour);

        graph.Connect(load, findContour);

        // Act
        await graph.ExecuteAsync(CancellationToken.None);

        // Assert
        var result = pool.Get<OpenCvSharp.Point[][]>(findContour.Id);

        // Convert to BGR so we can draw a colored overlay
        using var outputMat = new Mat();
        Cv2.CvtColor(srcMat, outputMat, ColorConversionCodes.GRAY2BGR);

        Cv2.DrawContours(outputMat, result, -1, Scalar.Red, 2);

        // Save for visual inspection (great during dev/debugging)
        ImagingTestHelpers.SaveAndOpenMat(outputMat, outputPath);
    }
}