using System.Diagnostics;
using Flowxel.Graph;
using Flowxel.Imaging.Operations.Filters;
using Flowxel.Imaging.Operations.IO;
using Flowxel.Imaging.Operations.Transforms;
using OpenCvSharp;

namespace Flowxel.Imaging.Tests;

public class VisionPipelineIntegrationTests(ITestOutputHelper output)
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "FlowxelTests");

    [Fact]
    public async Task FullPipeline_Load_Blur_Save_ProducesCorrectOutput()
    {
    }
}