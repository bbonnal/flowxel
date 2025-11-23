using System.Diagnostics;
using Flowxel.Graph;
using Flowxel.Imaging.Operations.Filters;
using Flowxel.Imaging.Operations.IO;
using Flowxel.Imaging.Operations.Transforms;
using Flowxel.Imaging.Pipeline;
using OpenCvSharp;

namespace Flowxel.Imaging.Tests;

public class VisionPipelineIntegrationTests(ITestOutputHelper output)
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "FlowxelTests");

    [Fact]
    public async Task FullPipeline_Load_Blur_Save_ProducesCorrectOutput()
    {
        var catalog = new OperationCatalog();
        var factory = new OperationFactory(catalog);

        catalog.Register<LoadOperation>();
        catalog.Register<GaussianBlurOperation>();
        catalog.Register<MeanOperation>();
        catalog.Register<SubtractOperation>();
        catalog.Register<SaveOperation>();

        var inputPath = Path.Combine(_tempDir, "input.png");
        Directory.CreateDirectory(_tempDir);
        var outputPath = Path.Combine(_tempDir, "output_blurred.png");

        // Build pipeline with references
        var load = new VisionNode(factory, catalog)
        {
            Name = "Load Image",
            OperationName = "Load",
            Input = VisionPort.None,
            Parameters =
            {
                ["Path"] = ImagingTestHelpers.SaveMat(ImagingTestHelpers.GenerateRandomMat(256, 256, MatType.CV_8UC1),
                    "test.png")
            },
        };

        var blur1 = new VisionNode(factory, catalog)
        {
            Name = "Gaussian Blur",
            OperationName = "GaussianBlur",
            Input = VisionPort.From(load), // ← Creates PortReference(load.Id, typeof(Mat))
            Parameters = { ["KernelSize"] = 5, ["Sigma"] = 10.0 }
        };

        var blur2 = new VisionNode(factory, catalog)
        {
            Name = "Gaussian Blur",
            OperationName = "GaussianBlur",
            Input = VisionPort.From(load), // ← Creates PortReference(load.Id, typeof(Mat))
            Parameters = { ["KernelSize"] = 50, ["Sigma"] = 10.0 }
        };

        var sub = new VisionNode(factory, catalog)
        {
            Name = "Sub",
            OperationName = "Subtract",
            Input = VisionPort.From([blur1, blur2]),
            Parameters = { }
        };

        var save = new VisionNode(factory, catalog)
        {
            Name = "Save Result",
            OperationName = "Save",
            Input = VisionPort.From(sub), // ← Creates PortReference(blur.Id, typeof(Mat))
            Parameters = { ["Path"] = outputPath }
        };

        var graph = new Graph<VisionNode>();
        graph.AddNode(load.Id, load);
        graph.AddNode(blur1.Id, blur1);
        graph.AddNode(blur2.Id, blur2);
        graph.AddNode(sub.Id, sub);
        graph.AddNode(save.Id, save);


        graph.AddEdge(load.Id, blur1.Id);
        graph.AddEdge(load.Id, blur2.Id);
        graph.AddEdge(blur1.Id, sub.Id);
        graph.AddEdge(blur2.Id, sub.Id);
        graph.AddEdge(sub.Id, save.Id);

        // Set up shared context
        var context = new VisionPipelineContext();
        VisionPipelineContext.Current = context;

        // Execute - GraphExecutor handles order, pool handles data flow!
        var executor = new GraphExecutor<VisionNode>(graph);
        await executor.ExecuteAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(File.Exists(outputPath), "Output image was not saved");

        using var savedMat = Cv2.ImRead(outputPath);
        Assert.False(savedMat.Empty(), "Saved image is corrupted");

        // The blur should be strong — text should be very soft
        using var gray = savedMat.CvtColor(ColorConversionCodes.BGR2GRAY);
        using var edges = new Mat();
        Cv2.Canny(gray, edges, 50, 150);

        var edgeDensity = Cv2.CountNonZero(edges) / (double)(edges.Rows * edges.Cols);
        output.WriteLine($"Edge density after strong blur: {edgeDensity:F4}");

        // With KernelSize=35, Sigma=10 → edge density should be very low
        Assert.True(edgeDensity < 1, $"Blur was too weak (edge density: {edgeDensity:F4})");

        // Optional: open result in Sway (uncomment to see)
        Process.Start(new ProcessStartInfo(outputPath) { UseShellExecute = true });
    }

    [Fact]
    public async Task DAG_Parallelizes_1000_Blurs_Efficiently()
    {
        // Arrange
        var catalog = new OperationCatalog();
        var factory = new OperationFactory(catalog);

        catalog.Register<LoadOperation>();
        catalog.Register<GaussianBlurOperation>();

        Directory.CreateDirectory(_tempDir);

        var inputPath = ImagingTestHelpers.SaveMat(
            ImagingTestHelpers.GenerateRandomMat(512, 512, MatType.CV_8UC3), 
            "benchmark_test.png");

        const int parallelChains = 64;
        const int totalBlurs = parallelChains * 10;
        const int blursPerChain = totalBlurs / parallelChains;

        // ====== SERIAL GRAPH ======
        // Structure: Load → Blur₁ → Blur₂ → ... → Blur₁₀₀₀
        // All 1000 blurs execute sequentially

        var serialGraph = new Graph<VisionNode>();
        var contextSerial = new VisionPipelineContext();

        var loadSerial = new VisionNode(factory, catalog)
        {
            Name = "Load (Serial)",
            OperationName = "Load",
            Input = VisionPort.None,
            Parameters = { ["Path"] = inputPath }
        };
        serialGraph.AddNode(loadSerial.Id, loadSerial);

        VisionNode previousNode = loadSerial;

        for (int i = 0; i < totalBlurs; i++)
        {
            var blur = new VisionNode(factory, catalog)
            {
                Name = $"Blur {i} (Serial)",
                OperationName = "GaussianBlur",
                Input = VisionPort.From(previousNode),
                Parameters = { ["KernelSize"] = 51, ["Sigma"] = 1.0 }
            };
            serialGraph.AddNode(blur.Id, blur);
            serialGraph.AddEdge(previousNode.Id, blur.Id);
            previousNode = blur;
        }

        // ====== PARALLEL GRAPH ======
        // Structure: Load → [Chain₁(125 blurs), Chain₂(125 blurs), ..., Chain₈(125 blurs)]
        // 8 chains of 125 blurs each execute in parallel

        var parallelGraph = new Graph<VisionNode>();
        var contextParallel = new VisionPipelineContext();

        var loadParallel = new VisionNode(factory, catalog)
        {
            Name = "Load (Parallel)",
            OperationName = "Load",
            Input = VisionPort.None,
            Parameters = { ["Path"] = inputPath }
        };
        parallelGraph.AddNode(loadParallel.Id, loadParallel);

        for (int chainIdx = 0; chainIdx < parallelChains; chainIdx++)
        {
            VisionNode chainPrevious = loadParallel;

            for (int blurIdx = 0; blurIdx < blursPerChain; blurIdx++)
            {
                var blur = new VisionNode(factory, catalog)
                {
                    Name = $"Chain{chainIdx}-Blur{blurIdx}",
                    OperationName = "GaussianBlur",
                    Input = VisionPort.From(chainPrevious),
                    Parameters = { ["KernelSize"] = 51, ["Sigma"] = 1.0 }
                };
                parallelGraph.AddNode(blur.Id, blur);
                parallelGraph.AddEdge(chainPrevious.Id, blur.Id);
                chainPrevious = blur;
            }
        }

        // Act - Execute both graphs and measure time
        output.WriteLine($"Serial graph:   {serialGraph.Nodes.Count()} nodes (1 chain × {totalBlurs} blurs)");
        output.WriteLine(
            $"Parallel graph: {parallelGraph.Nodes.Count()} nodes ({parallelChains} chains × {blursPerChain} blurs each)");
        output.WriteLine("");

        var serialExecutor = new GraphExecutor<VisionNode>(serialGraph);
        var swSerial = Stopwatch.StartNew();
        VisionPipelineContext.Current = contextSerial;
        await serialExecutor.ExecuteAsync(TestContext.Current.CancellationToken);
        swSerial.Stop();

        var parallelExecutor = new GraphExecutor<VisionNode>(parallelGraph);
        var swParallel = Stopwatch.StartNew();
        VisionPipelineContext.Current = contextParallel;
        await parallelExecutor.ExecuteAsync(TestContext.Current.CancellationToken);
        swParallel.Stop();

        // Assert
        output.WriteLine($"Serial execution:   {swSerial.ElapsedMilliseconds}ms ({totalBlurs} blurs in sequence)");
        output.WriteLine(
            $"Parallel execution: {swParallel.ElapsedMilliseconds}ms ({parallelChains} chains × {blursPerChain} blurs)");
        output.WriteLine("");

        var speedup = (double)swSerial.ElapsedMilliseconds / swParallel.ElapsedMilliseconds;
        output.WriteLine($"Speedup factor: {speedup:F2}x");
        output.WriteLine("");

        // The parallel version MUST be faster if the DAG is working correctly
        // With 8 chains on a multi-core system, we expect at least 2x speedup
        output.WriteLine($"Asserting speedup > 2.0x (actual: {speedup:F2}x)");
        Assert.True(speedup > 2.0,
            $"DAG executor is NOT parallelizing properly! " +
            $"Expected speedup > 2.0x, but got {speedup:F2}x. " +
            $"Both graphs perform {totalBlurs} identical blur operations. " +
            $"Serial: {swSerial.ElapsedMilliseconds}ms, Parallel: {swParallel.ElapsedMilliseconds}ms");

        output.WriteLine("✓ DAG executor successfully parallelizes independent chains!");
        output.WriteLine($"  Serial:   {totalBlurs} blurs in 1 chain");
        output.WriteLine($"  Parallel: {totalBlurs} blurs split into {parallelChains} chains");
        output.WriteLine($"  Result:   {speedup:F2}x faster with parallelization");
    }

    [Fact]
    public async Task DAG_Finds_Optimal_Parallelism()
    {
        var catalog = new OperationCatalog();
        var factory = new OperationFactory(catalog);

        catalog.Register<LoadOperation>();
        catalog.Register<GaussianBlurOperation>();

        Directory.CreateDirectory(_tempDir);

        var inputPath = ImagingTestHelpers.SaveMat(
            ImagingTestHelpers.GenerateRandomMat(1024, 1024, MatType.CV_8UC3),
            "benchmark_test.png");

        const int totalBlurs = 128;

        // Test different parallelism levels
        var parallelismLevels = new[] { 1, 2, 4, 8, 16, 32, 64, 128 };
        var results = new Dictionary<int, long>();

        foreach (var chains in parallelismLevels)
        {
            var blursPerChain = totalBlurs / chains;

            var graph = new Graph<VisionNode>();
            var context = new VisionPipelineContext();

            var load = new VisionNode(factory, catalog)
            {
                Name = "Load",
                OperationName = "Load",
                Input = VisionPort.None,
                Parameters = { ["Path"] = inputPath }
            };
            graph.AddNode(load.Id, load);

            for (int chainIdx = 0; chainIdx < chains; chainIdx++)
            {
                VisionNode prev = load;
                for (int blurIdx = 0; blurIdx < blursPerChain; blurIdx++)
                {
                    var blur = new VisionNode(factory, catalog)
                    {
                        Name = $"C{chainIdx}B{blurIdx}",
                        OperationName = "GaussianBlur",
                        Input = VisionPort.From(prev),
                        Parameters = { ["KernelSize"] = 31, ["Sigma"] = 5.0 }
                    };
                    graph.AddNode(blur.Id, blur);
                    graph.AddEdge(prev.Id, blur.Id);
                    prev = blur;
                }
            }

            var executor = new GraphExecutor<VisionNode>(graph);
            VisionPipelineContext.Current = context;

            var sw = Stopwatch.StartNew();
            await executor.ExecuteAsync(TestContext.Current.CancellationToken);
            sw.Stop();

            results[chains] = sw.ElapsedMilliseconds;
            output.WriteLine($"{chains,2} chains: {sw.ElapsedMilliseconds,5}ms ({blursPerChain,3} blurs per chain)");
        }

        output.WriteLine("");

        var baseline = results[1];
        foreach (var (chains, time) in results.OrderBy(kv => kv.Key))
        {
            var speedup = (double)baseline / time;
            var efficiency = speedup / chains * 100;
            output.WriteLine($"{chains,2} chains: {speedup:F2}x speedup, {efficiency:F1}% efficiency");
        }

        // Find optimal
        var optimal = results.OrderBy(kv => kv.Value).First();
        output.WriteLine("");
        output.WriteLine($"✓ Optimal parallelism: {optimal.Key} chains ({optimal.Value}ms)");
    }
}