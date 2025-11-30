using System.Diagnostics;
using Flowxel.Graph;
using Flowxel.Imaging.Operations.Filters;
using Flowxel.Imaging.Operations.IO;
using Flowxel.Imaging.Operations.Transforms;
using Flowxel.Imaging.Operations;
using OpenCvSharp;

namespace Flowxel.Imaging.Tests;

public class VisionPipelineIntegrationTests(ITestOutputHelper output)
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "FlowxelTests");


    [Fact]
    public async Task FullPipeline_Load_Blur_Save_ProducesCorrectOutput()
    {
         Directory.CreateDirectory(_tempDir);
         
         using var srcMat = ImagingTestHelpers.GenerateRandomMat(256,256, MatType.CV_8UC1);
         var filePath = Path.Combine(_tempDir, "random.png");

         Cv2.ImWrite(filePath, srcMat);
         
         // Create graph and pool
         var graph = new Graph<IExecutableNode>();
         var pool = new ResourcePool();

        var load = new Node<Empty, Mat>(new LoadOperation(), pool, graph)
        {
            Name = "Load Image",
            Parameters =
            {
                ["Path"] = filePath
            }
        };

        var blur1 = new Node<Mat, Mat>(new GaussianBlurOperation(), pool, graph)
        {
            Name = "Blur Small",
            Parameters =
            {
                ["KernelSize"] = 5,
                ["Sigma"] = 10.0
            }
        };

        var blur2 = new Node<Mat, Mat>(new GaussianBlurOperation(), pool, graph)
        {
            Name = "Blur Large",
            Parameters =
            {
                ["KernelSize"] = 50,
                ["Sigma"] = 10.0
            } 
        };

        var subtract = new Node<Mat, Mat>(new SubtractOperation(), pool, graph)
        {
            Name = "Subtract",
        };

        var save = new Node<Mat, Empty>(new SaveOperation(), pool, graph)
        {
            Name = "Save",
            Parameters =
            {
                ["Path"] = "output.png"
            }
        };

        // Build graph (use your existing DAG infrastructure)
        graph.AddNode(load.Id, load);
        graph.AddNode(blur1.Id, blur1);
        graph.AddNode(blur2.Id, blur2);
        graph.AddNode(subtract.Id, subtract);
        graph.AddNode(save.Id, save);

        graph.AddEdge(load.Id, blur1.Id);
        graph.AddEdge(load.Id, blur2.Id);
        graph.AddEdge(blur1.Id, subtract.Id);
        graph.AddEdge(blur2.Id, subtract.Id);
        graph.AddEdge(subtract.Id, save.Id);

        // Execute with your existing executor
        await graph.ExecuteAsync(CancellationToken.None);

        // Results are in the pool, traceable by node ID
        var finalResult = pool.Get<Mat>(subtract.Id);
        
        ImagingTestHelpers.SaveAndOpenMat(finalResult, "output.png");
        
        Console.WriteLine($"Result came from node: {subtract.Id} ({subtract.Name})");
    }
}