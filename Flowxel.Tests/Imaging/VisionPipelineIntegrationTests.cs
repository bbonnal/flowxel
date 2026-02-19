using System.Diagnostics;
using Flowxel.Graph;
using Flowxel.Imaging.Operations.Filters;
using Flowxel.Imaging.Operations.IO;
using Flowxel.Imaging.Operations.Transforms;
using Microsoft.Extensions.DependencyInjection;
using OpenCvSharp;

namespace Flowxel.Imaging.Tests;

public class VisionPipelineIntegrationTests
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "FlowxelTests");


    [Fact]
    public async Task FullPipeline_Load_Blur_Save_ProducesCorrectOutput()
    {
        // Arrange
        Directory.CreateDirectory(_tempDir);

        using var srcMat = ImagingTestHelpers.GenerateRandomMat(256, 256, MatType.CV_8UC1);
        var inputPath = Path.Combine(_tempDir, "input_random.png");
        var outputPath = Path.Combine(_tempDir, "output_unsharp.png");

        Cv2.ImWrite(inputPath, srcMat);

        // Setup DI container
        var services = new ServiceCollection();

        // Register core services
        services.AddSingleton<ResourcePool>();
        services.AddSingleton<Graph<IExecutableNode>>();

        // Register all node types (transient = new instance each time we resolve)
        services.AddTransient<LoadOperation>();
        services.AddTransient<GaussianBlurOperation>();
        services.AddTransient<SubtractOperation>();
        services.AddTransient<SaveOperation>();

        var provider = services.BuildServiceProvider();

        // Resolve services
        var graph = provider.GetRequiredService<Graph<IExecutableNode>>();
        var pool = provider.GetRequiredService<ResourcePool>();

        // Resolve nodes via DI (this triggers constructor injection of pool + graph)
        var load = provider.GetRequiredService<LoadOperation>();
        load.Parameters["Path"] = inputPath;

        var blurSmall = provider.GetRequiredService<GaussianBlurOperation>();
        blurSmall.Parameters["KernelSize"] = 5;
        blurSmall.Parameters["Sigma"] = 10.0;

        var blurLarge = provider.GetRequiredService<GaussianBlurOperation>();
        blurLarge.Parameters["KernelSize"] = 50;
        blurLarge.Parameters["Sigma"] = 10.0;

        var subtract = provider.GetRequiredService<SubtractOperation>();

        var save = provider.GetRequiredService<SaveOperation>();
        save.Parameters["Path"] = outputPath;

        // Build the graph topology
        graph.AddNode(load);
        graph.AddNode(blurSmall);
        graph.AddNode(blurLarge);
        graph.AddNode(subtract);
        graph.AddNode(save);

        graph.Connect(load, blurSmall);
        graph.Connect(load, blurLarge);
        graph.Connect(blurSmall, subtract, GraphPorts.DefaultOutput, "left");
        graph.Connect(blurLarge, subtract, GraphPorts.DefaultOutput, "right");
        graph.Connect(subtract, save);

        // Act
        await graph.ExecuteAsync(TestContext.Current.CancellationToken);

        // Assert
        var resultMat = pool.Get<Mat>(subtract.Id);

        // Save for visual inspection (great during dev/debugging)
        ImagingTestHelpers.SaveAndOpenMat(resultMat, outputPath);

        Assert.NotNull(resultMat);
        Assert.Equal(srcMat.Size(), resultMat.Size());
        Assert.Equal(srcMat.Type(), resultMat.Type());

        // Optional: verify if a file was actually saved
        Assert.True(File.Exists(outputPath));

        Console.WriteLine($"Pipeline completed! Result saved to: {outputPath}");
    }

    [Fact]
    public async Task ParallelBlurBranches_SignificantlyFaster_ThanSerialChain()
    {
        const int imageSize = 256;
        const int branches = 64;
        const int blursPerBranch = 100;
        const int kernelSize = 35;
        const double sigma = 12.0;

        // Generate one big test image
        Directory.CreateDirectory(_tempDir);
        var testImagePath = Path.Combine(_tempDir, "perf_test_1024.png");
        using var srcMat = ImagingTestHelpers.GenerateRandomMat(imageSize, imageSize, MatType.CV_8UC1);
        Cv2.ImWrite(testImagePath, srcMat);

        // 1. PARALLEL: N independent branches, each with M blurs
        var parallelTime = await MeasureAsync(async () =>
        {
            var (provider, graph, load) = CreateGraphWithLoadNode(testImagePath);

            for (var branch = 0; branch < branches; branch++)
            {
                IExecutableNode previous = load;

                for (var i = 0; i < blursPerBranch; i++)
                {
                    var blur = CreateBlurNode(provider, kernelSize, sigma);
                    graph.Connect(previous, blur);
                    previous = blur;
                }
            }

            await graph.ExecuteAsync(TestContext.Current.CancellationToken);
        });

        // 2. SERIAL: One giant chain of N×M blurs
        var serialTime = await MeasureAsync(async () =>
        {
            var (provider, graph, load) = CreateGraphWithLoadNode(testImagePath);

            IExecutableNode current = load;

            for (int i = 0; i < branches * blursPerBranch; i++)
            {
                var blur = CreateBlurNode(provider, kernelSize, sigma);
                graph.Connect(current, blur);
                current = blur;
            }

            await graph.ExecuteAsync(TestContext.Current.CancellationToken);
        });

        // Results
        const int totalBlurs = branches * blursPerBranch;
        var speedup = serialTime.TotalMilliseconds / Math.Max(parallelTime.TotalMilliseconds, 0.001);

        Console.WriteLine($"Image size      : {imageSize}x{imageSize}");
        Console.WriteLine($"Branches        : {branches}");
        Console.WriteLine($"Blurs per branch: {blursPerBranch}");
        Console.WriteLine($"Total blur ops  : {totalBlurs}");
        Console.WriteLine($"Parallel time   : {parallelTime.TotalMilliseconds:F1} ms");
        Console.WriteLine($"Serial time     : {serialTime.TotalMilliseconds:F1} ms");
        Console.WriteLine($"Speedup         : {speedup:F2}x");

        // On an 8-core machine you typically get 6–9× speedup
        Assert.True(parallelTime < serialTime * 0.5,
            $"Parallel execution should be at least 2x faster. " +
            $"Got only {speedup:F2}x (parallel: {parallelTime.TotalMilliseconds:F1}ms, " +
            $"serial: {serialTime.TotalMilliseconds:F1}ms)");
    }

    private static (IServiceProvider provider, Graph<IExecutableNode> graph, LoadOperation load)
        CreateGraphWithLoadNode(string path)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ResourcePool>();
        services.AddSingleton<Graph<IExecutableNode>>();
        services.AddTransient<LoadOperation>();
        services.AddTransient<GaussianBlurOperation>();

        var provider = services.BuildServiceProvider();

        var graph = provider.GetRequiredService<Graph<IExecutableNode>>();
        var load = provider.GetRequiredService<LoadOperation>();
        load.Parameters["Path"] = path;

        graph.AddNode(load);
        return (provider, graph, load);
    }

    [Theory]
    [InlineData(100, 8, 100)] // Tiny image → overhead dominates → serial wins
    public async Task ParallelVsSerial_IntuitionBuilder(
        int imageSize,
        int branches,
        int blursPerBranch)
    {
        const int kernelSize = 55;
        const double sigma = 32.0;

        var testImagePath = Path.Combine(_tempDir, $"perf_{imageSize}.png");
        if (!File.Exists(testImagePath))
        {
            using var mat = ImagingTestHelpers.GenerateRandomMat(imageSize, imageSize, MatType.CV_8UC1);
            Cv2.ImWrite(testImagePath, mat);
        }

        // Warm-up once per test run (shared across theories)
        await WarmupOnceAsync(imageSize);

        var parallelTime = await MeasureAsync(() =>
            BuildAndRunGraph(testImagePath, branches, blursPerBranch, kernelSize, sigma, isParallel: true));

        var serialTime = await MeasureAsync(() =>
            BuildAndRunGraph(testImagePath, branches, blursPerBranch, kernelSize, sigma, isParallel: false));

        var totalOperations = branches * blursPerBranch;
        var speedup = serialTime.TotalMilliseconds / Math.Max(parallelTime.TotalMilliseconds, 0.001);

        // Color-coded intuition output
        var verdict = speedup switch
        {
            > 4.0 => "PARALLEL WINS BIG",
            > 1.8 => "Parallel wins",
            > 1.1 => "Slight parallel win",
            > 0.9 => "Almost equal",
            > 0.7 => "Serial slightly better",
            _ => "SERIAL WINS (overhead too high!)"
        };

        var color = speedup switch
        {
            > 4.0 => "92m", // Green
            > 1.5 => "32m", // Yellow-green
            > 1.0 => "33m", // Yellow
            > 0.8 => "31m", // Red
            _ => "91m" // Bright red
        };

        Console.WriteLine(
            $"\nImage: {imageSize}x{imageSize} | " +
            $"Branches: {branches,2} | " +
            $"Blurs/branch: {blursPerBranch,3} | " +
            $"Total ops: {totalOperations,4} | " +
            $"Parallel: {parallelTime.TotalMilliseconds,6:F1}ms | " +
            $"Serial: {serialTime.TotalMilliseconds,6:F1}ms | " +
            $"Speedup: {speedup,5:F2}x → \u001b[{color}{verdict}\u001b[0m");


        Assert.True(speedup > 3.0,
            $"For large workloads, expected strong parallel scaling. Got only {speedup:F2}x");
    }

    private static async Task WarmupOnceAsync(int size)
    {
        if (_warmedUp) return;

        var path = Path.Combine(Path.GetTempPath(), "warmup.png");
        if (!File.Exists(path))
        {
            using var m = ImagingTestHelpers.GenerateRandomMat(size, size, MatType.CV_8UC1);
            Cv2.ImWrite(path, m);
        }

        await BuildAndRunGraph(path, branches: 2, blursPerBranch: 2, kernelSize: 15, sigma: 5, isParallel: true);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        _warmedUp = true;
    }

    private static bool _warmedUp;

    private static async Task<TimeSpan> MeasureAsync(Func<Task> action)
    {
        var sw = Stopwatch.StartNew();
        await action();
        sw.Stop();
        return sw.Elapsed;
    }

    private static async Task BuildAndRunGraph(
        string imagePath,
        int branches,
        int blursPerBranch,
        int kernelSize,
        double sigma,
        bool isParallel)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ResourcePool>();
        services.AddSingleton<Graph<IExecutableNode>>();
        services.AddTransient<LoadOperation>();
        services.AddTransient<GaussianBlurOperation>();
        var provider = services.BuildServiceProvider();

        var graph = provider.GetRequiredService<Graph<IExecutableNode>>();
        var load = provider.GetRequiredService<LoadOperation>();
        load.Parameters["Path"] = imagePath;
        graph.AddNode(load);

        if (isParallel)
        {
            // N independent branches
            for (int b = 0; b < branches; b++)
            {
                IExecutableNode prev = load;
                for (int i = 0; i < blursPerBranch; i++)
                {
                    var blur = provider.GetRequiredService<GaussianBlurOperation>();
                    blur.Parameters["KernelSize"] = kernelSize;
                    blur.Parameters["Sigma"] = sigma;
                    graph.AddNode(blur);
                    graph.Connect(prev, blur);
                    prev = blur;
                }
            }
        }
        else
        {
            // One long serial chain
            IExecutableNode prev = load;
            for (int i = 0; i < branches * blursPerBranch; i++)
            {
                var blur = provider.GetRequiredService<GaussianBlurOperation>();
                blur.Parameters["KernelSize"] = kernelSize;
                blur.Parameters["Sigma"] = sigma;
                graph.AddNode(blur);
                graph.Connect(prev, blur);
                prev = blur;
            }
        }

        await graph.ExecuteAsync(TestContext.Current.CancellationToken);
    }

    private static GaussianBlurOperation CreateBlurNode(IServiceProvider provider, int kernel, double sigma)
    {
        var graph = provider.GetRequiredService<Graph<IExecutableNode>>();
        var blur = provider.GetRequiredService<GaussianBlurOperation>();
        blur.Parameters["KernelSize"] = kernel;
        blur.Parameters["Sigma"] = sigma;
        graph.AddNode(blur);
        return blur;
    }

}
