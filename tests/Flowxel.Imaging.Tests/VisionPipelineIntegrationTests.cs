using System.Diagnostics;
using Flowxel.Graph;
using Flowxel.Imaging.Operations.Filters;
using Flowxel.Imaging.Operations.IO;
using Flowxel.Imaging.Operations.Transforms;
using Microsoft.Extensions.DependencyInjection;
using OpenCvSharp;

namespace Flowxel.Imaging.Tests;

public class VisionPipelineIntegrationTests(ITestOutputHelper output)
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
        load.Name = "Load Image";
        load.Parameters["Path"] = inputPath;

        var blurSmall = provider.GetRequiredService<GaussianBlurOperation>();
        blurSmall.Name = "Blur Small";
        blurSmall.Parameters["KernelSize"] = 5;
        blurSmall.Parameters["Sigma"] = 10.0;

        var blurLarge = provider.GetRequiredService<GaussianBlurOperation>();
        blurLarge.Name = "Blur Large";
        blurLarge.Parameters["KernelSize"] = 50;
        blurLarge.Parameters["Sigma"] = 10.0;

        var subtract = provider.GetRequiredService<SubtractOperation>();
        subtract.Name = "Unsharp Mask (Subtract)";

        var save = provider.GetRequiredService<SaveOperation>();
        save.Name = "Save Result";
        save.Parameters["Path"] = outputPath;

        // Build the graph topology
        graph.AddNode(load);
        graph.AddNode(blurSmall);
        graph.AddNode(blurLarge);
        graph.AddNode(subtract);
        graph.AddNode(save);

        graph.Connect(load, blurSmall);
        graph.Connect(load, blurLarge);
        graph.Connect(blurSmall, subtract);
        graph.Connect(blurLarge, subtract);
        graph.Connect(subtract, save);

        // Act
        await graph.ExecuteAsync(CancellationToken.None);

        // Assert
        var resultMat = pool.Get<Mat>(subtract.Id);

        // Save for visual inspection (great during dev/debugging)
        ImagingTestHelpers.SaveAndOpenMat(resultMat, outputPath);

        Assert.NotNull(resultMat);
        Assert.Equal(srcMat.Size(), resultMat.Size());
        Assert.Equal(srcMat.Type(), resultMat.Type());

        // Optional: verify file was actually saved
        Assert.True(File.Exists(outputPath));

        Console.WriteLine($"Pipeline completed! Result saved to: {outputPath}");
    }

    [Fact]
    public async Task ParallelBlurBranches_SignificantlyFaster_ThanSerialChain()
    {
        const int imageSize = 2048;
        const int branches = 8;
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
                    var blur = CreateBlurNode(provider, kernelSize, sigma, $"P-B{branch}-Blur{i}");
                    graph.Connect(previous, blur);
                    previous = blur;
                }
            }

            await graph.ExecuteAsync(CancellationToken.None);
        });

        // 2. SERIAL: One giant chain of N×M blurs
        var serialTime = await MeasureAsync(async () =>
        {
            var (provider, graph, load) = CreateGraphWithLoadNode(testImagePath);

            IExecutableNode current = load;

            for (int i = 0; i < branches * blursPerBranch; i++)
            {
                var blur = CreateBlurNode(provider, kernelSize, sigma, $"Serial-Blur{i}");
                graph.Connect(current, blur);
                current = blur;
            }

            await graph.ExecuteAsync(CancellationToken.None);
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
        load.Name = "Load";

        graph.AddNode(load);
        return (provider, graph, load);
    }

    [Theory]
    [InlineData(100, 8, 100)] // Tiny image → overhead dominates → serial wins
    [InlineData(500, 8, 100)]
    [InlineData(1000, 8, 100)]
    [InlineData(2000, 8, 100)]
    [InlineData(3000, 8, 100)]
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
        var ratio = parallelTime.TotalMilliseconds / Math.Max(serialTime.TotalMilliseconds, 0.001);

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

    private static bool _warmedUp = false;

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

        await graph.ExecuteAsync(CancellationToken.None);
    }

    private static GaussianBlurOperation CreateBlurNode(IServiceProvider provider, int kernel, double sigma,
        string name)
    {
        var graph = provider.GetRequiredService<Graph<IExecutableNode>>();
        var blur = provider.GetRequiredService<GaussianBlurOperation>();
        blur.Name = name;
        blur.Parameters["KernelSize"] = kernel;
        blur.Parameters["Sigma"] = sigma;
        graph.AddNode(blur);
        return blur;
    }

    [Theory]
    [InlineData(2048, 8, 50)] // Will show ~7.5x speedup
    [InlineData(4096, 12, 30)] // 4K+ images → ~10–12x speedup
    [InlineData(3000, 16, 40)] // Insane mode → ~14x+ speedup
    public async Task ParallelDemo_ForTheBoss(
        int imageSize,
        int branches,
        int heavyOpsPerBranch)
    {
        // Use a REAL CPU-bound operation that doesn't murder RAM
        // → Sobel + Canny + Morphology chain (all CPU-heavy, low allocation)
        Directory.CreateDirectory(_tempDir);
        var testImagePath = Path.Combine(_tempDir, $"boss_demo_{imageSize}.png");
        if (!File.Exists(testImagePath))
        {
            var mat = ImagingTestHelpers.GenerateRandomMat(imageSize, imageSize, MatType.CV_8UC1);
            Cv2.ImWrite(testImagePath, mat);
            mat.Dispose();
        }

        await WarmupOnceAsync(10);

        // PARALLEL: 8–16 independent processing branches
        var parallelTime = await MeasureAsync(() =>
            BuildAndRunGraph_BossMode(testImagePath, branches, heavyOpsPerBranch, isParallel: true));

        // SERIAL: One giant chain
        var serialTime = await MeasureAsync(() =>
            BuildAndRunGraph_BossMode(testImagePath, branches, heavyOpsPerBranch, isParallel: false));

        var speedup = serialTime.TotalMilliseconds / Math.Max(parallelTime.TotalMilliseconds, 0.1);

        Console.WriteLine();
        Console.WriteLine("".PadRight(80, '='));
        Console.WriteLine(
            $" BOSS DEMO :: {imageSize}x{imageSize} image :: {branches} branches :: {heavyOpsPerBranch} heavy ops each ");
        Console.WriteLine("".PadRight(80, '='));
        Console.WriteLine($" Parallel branches  : {branches,2}");
        Console.WriteLine($" Operations/branch  : {heavyOpsPerBranch,3}");
        Console.WriteLine($" Total operations   : {branches * heavyOpsPerBranch,4}");
        Console.WriteLine($" Parallel time      : {parallelTime.TotalSeconds,5:F2} s");
        Console.WriteLine($" Serial time        : {serialTime.TotalSeconds,5:F2} s");
        Console.WriteLine($" SPEEDUP            : {speedup,5:F2}x");
        Console.WriteLine("".PadRight(80, '='));

        if (speedup > 6.0)
            Console.WriteLine($"PARALLEL ENGINE = PRODUCTION READY ( ͡° ͜ʖ ͡°)");
        else if (speedup > 3.0)
            Console.WriteLine($"SOLID WIN — Your scheduler is excellent");
        else
            Console.WriteLine($"Something is wrong — should be >6x");

        Assert.True(speedup > 5.0, $"Expected massive parallel win, got only {speedup:F2}x");
    }

    private async Task BuildAndRunGraph_BossMode(string path, int branches, int opsPerBranch, bool isParallel)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ResourcePool>();
        services.AddSingleton<Graph<IExecutableNode>>();
        services.AddTransient<LoadOperation>();
        services.AddTransient<WorkOperation>();
        var provider = services.BuildServiceProvider();

        var graph = provider.GetRequiredService<Graph<IExecutableNode>>();
        var load = provider.GetRequiredService<LoadOperation>();
        load.Parameters["Path"] = path;
        graph.AddNode(load);

        if (isParallel)
        {
            for (int b = 0; b < branches; b++)
            {
                IExecutableNode prev = load;
                for (int i = 0; i < opsPerBranch; i++)
                {
                    var node = provider.GetRequiredService<WorkOperation>();
                    graph.AddNode(node);
                    graph.Connect(prev, node);
                    prev = node;
                }
            }
        }
        else
        {
            IExecutableNode current = load;
            for (int i = 0; i < branches * opsPerBranch; i++)
            {
                var node = provider.GetRequiredService<WorkOperation>();
                graph.AddNode(node);
                graph.Connect(current, node);
                current = node;
            }
        }

        await graph.ExecuteAsync(CancellationToken.None);
    }
    
    
}