using Flowxel.Core.Geometry.Primitives;
using Flowxel.Core.Geometry.Shapes;
using Flowxel.Graph;
using Flowxel.Imaging.Operations;
using Flowxel.Imaging.Operations.Constructions;
using Flowxel.Imaging.Operations.Extractions;
using Flowxel.Imaging.Operations.Transforms;
using OpenCvSharp;
using Point = Flowxel.Core.Geometry.Shapes.Point;

namespace Flowxel.Imaging.Tests;

public class AdvancedOperationsTests
{
    private const double Tolerance = 1.0;

    [Fact]
    public async Task CropImageFromRectangleRegion_CropsAroundTheRequestedRegion()
    {
        var source = new Mat(120, 120, MatType.CV_8UC1, Scalar.All(255));
        Cv2.Rectangle(source, new OpenCvSharp.Point(40, 45), new OpenCvSharp.Point(80, 65), Scalar.All(0), -1);

        var region = new Rectangle
        {
            Pose = new Pose(new Vector(60, 55), new Vector(1, 0)),
            Width = 40,
            Height = 20
        };

        var output = await ExecuteMatOperationAsync(source, (pool, graph) => new CropImageFromRegionOperation(pool, graph), operation =>
        {
            operation.Parameters["Region"] = region;
        });

        Assert.InRange(output.Width, 39, 41);
        Assert.InRange(output.Height, 19, 21);
        Assert.Equal(0, output.At<byte>(output.Height / 2, output.Width / 2));
    }

    [Fact]
    public async Task ExtractLineInRegions_FindsLineInsideRectangleRegion()
    {
        var source = new Mat(120, 120, MatType.CV_8UC1, Scalar.All(255));
        Cv2.Line(source, new OpenCvSharp.Point(20, 60), new OpenCvSharp.Point(100, 60), Scalar.All(0), 2);

        var region = new Rectangle
        {
            Pose = new Pose(new Vector(60, 60), new Vector(1, 0)),
            Width = 90,
            Height = 30
        };

        var lines = await ExecuteMatOperationAsync(source, (pool, graph) => new ExtractLineInRegionsOperation(pool, graph), operation =>
        {
            operation.Parameters["Region"] = region;
        });

        Assert.NotEmpty(lines);
        Assert.Contains(lines, line => Math.Abs(line.Pose.Position.Y - 60) <= 3);
    }

    [Fact]
    public async Task ExtractArcInRegion_FindsArcInsideArcRegion()
    {
        var source = new Mat(200, 200, MatType.CV_8UC1, Scalar.All(255));
        Cv2.Ellipse(source, new OpenCvSharp.Point(100, 100), new Size(40, 40), 0, 20, 160, Scalar.All(0), 2);

        var region = new Arc
        {
            Pose = new Pose(new Vector(100, 100), new Vector(1, 0)),
            Radius = 50,
            StartAngle = 10 * Math.PI / 180.0,
            EndAngle = 170 * Math.PI / 180.0
        };

        var arcs = await ExecuteMatOperationAsync(source, (pool, graph) => new ExtractArcInRegionOperation(pool, graph), operation =>
        {
            operation.Parameters["Region"] = region;
        });

        Assert.NotEmpty(arcs);
        Assert.InRange(arcs[0].Pose.Position.X, 100 - 4, 100 + 4);
        Assert.InRange(arcs[0].Pose.Position.Y, 100 - 4, 100 + 4);
        Assert.InRange(arcs[0].Radius, 35, 45);
    }

    [Fact]
    public async Task ExtractContourInRegion_OnlyReturnsContoursInsideRegion()
    {
        var source = new Mat(180, 180, MatType.CV_8UC1, Scalar.All(255));
        Cv2.Rectangle(source, new OpenCvSharp.Point(35, 35), new OpenCvSharp.Point(70, 70), Scalar.All(0), -1);
        Cv2.Rectangle(source, new OpenCvSharp.Point(120, 120), new OpenCvSharp.Point(150, 150), Scalar.All(0), -1);

        var region = new Rectangle
        {
            Pose = new Pose(new Vector(52, 52), new Vector(1, 0)),
            Width = 50,
            Height = 50
        };

        var contours = await ExecuteMatOperationAsync(source, (pool, graph) => new ExtractContourInRegionOperation(pool, graph), operation =>
        {
            operation.Parameters["Region"] = region;
        });

        Assert.Single(contours);
        Assert.All(contours[0], point =>
        {
            Assert.InRange(point.X, 30, 75);
            Assert.InRange(point.Y, 30, 75);
        });
    }

    [Fact]
    public async Task ConstructLineLineIntersection_ReturnsTheCrossingPoint()
    {
        var pool = new ResourcePool();
        var graph = new Graph<IExecutableNode>();

        var lineA = new SourceLineOperation(pool, graph, CreateLine(10, 50, 90, 50));
        var lineB = new SourceLineOperation(pool, graph, CreateLine(50, 10, 50, 90));
        var intersection = new ConstructLineLineIntersectionOperation(pool, graph);

        graph.AddNode(lineA);
        graph.AddNode(lineB);
        graph.AddNode(intersection);
        graph.Connect(lineA, intersection);
        graph.Connect(lineB, intersection);

        await graph.ExecuteAsync(TestContext.Current.CancellationToken);

        var result = pool.Get<Point>(intersection.Id);
        Assert.InRange(result.Pose.Position.X, 50 - Tolerance, 50 + Tolerance);
        Assert.InRange(result.Pose.Position.Y, 50 - Tolerance, 50 + Tolerance);
    }

    [Fact]
    public async Task ConstructLineLineBisector_ReturnsABisectingLine()
    {
        var pool = new ResourcePool();
        var graph = new Graph<IExecutableNode>();

        var lineA = new SourceLineOperation(pool, graph, CreateLine(10, 50, 90, 50));
        var lineB = new SourceLineOperation(pool, graph, CreateLine(50, 10, 50, 90));
        var bisector = new ConstructLineLineBisectorOperation(pool, graph);

        graph.AddNode(lineA);
        graph.AddNode(lineB);
        graph.AddNode(bisector);
        graph.Connect(lineA, bisector);
        graph.Connect(lineB, bisector);

        await graph.ExecuteAsync(TestContext.Current.CancellationToken);

        var result = pool.Get<Line>(bisector.Id);
        Assert.InRange(result.Pose.Position.X, 50 - Tolerance, 50 + Tolerance);
        Assert.InRange(result.Pose.Position.Y, 50 - Tolerance, 50 + Tolerance);
        Assert.InRange(result.Pose.Orientation.X, 0.6, 0.8);
        Assert.InRange(result.Pose.Orientation.Y, 0.6, 0.8);
    }

    [Fact]
    public async Task ConstructLineArcIntersections_ReturnsTwoIntersections()
    {
        var pool = new ResourcePool();
        var graph = new Graph<IExecutableNode>();

        var line = new SourceLineOperation(pool, graph, CreateLine(20, 50, 80, 50));
        var operation = new ConstructLineArcIntersectionsOperation(pool, graph);
        operation.Parameters["Arc"] = new Arc
        {
            Pose = new Pose(new Vector(50, 50), new Vector(1, 0)),
            Radius = 20,
            StartAngle = 0,
            EndAngle = 2 * Math.PI
        };

        graph.AddNode(line);
        graph.AddNode(operation);
        graph.Connect(line, operation);

        await graph.ExecuteAsync(TestContext.Current.CancellationToken);

        var intersections = pool.Get<Point[]>(operation.Id);
        Assert.Equal(2, intersections.Length);
        Assert.InRange(intersections[0].Pose.Position.X, 30 - Tolerance, 30 + Tolerance);
        Assert.InRange(intersections[0].Pose.Position.Y, 50 - Tolerance, 50 + Tolerance);
        Assert.InRange(intersections[1].Pose.Position.X, 70 - Tolerance, 70 + Tolerance);
        Assert.InRange(intersections[1].Pose.Position.Y, 50 - Tolerance, 50 + Tolerance);
    }

    [Fact]
    public async Task SquareWithRectangleRoi_ExtractsExpectedTopEdgeLine()
    {
        var source = new Mat(160, 160, MatType.CV_8UC1, Scalar.All(255));
        Cv2.Rectangle(source, new OpenCvSharp.Point(40, 40), new OpenCvSharp.Point(120, 120), Scalar.All(0), 2);

        var roi = new Rectangle
        {
            Pose = new Pose(new Vector(80, 40), new Vector(1, 0)),
            Width = 100,
            Height = 20
        };

        var lines = await ExecuteMatOperationAsync(
            source,
            (pool, graph) => new ExtractLineInRegionsOperation(pool, graph),
            operation => { operation.Parameters["Region"] = roi; });

        Assert.NotEmpty(lines);
        Assert.Contains(lines, line =>
            Math.Abs(line.Pose.Position.Y - 40) <= 3 &&
            Math.Abs(line.Pose.Orientation.Y) <= 0.2);
    }

    [Fact]
    public async Task CircleWithCircleRoi_CropThenExtractCircle_ReturnsExpectedCenterAndRadius()
    {
        var source = new Mat(220, 220, MatType.CV_8UC1, Scalar.All(255));
        Cv2.Circle(source, new OpenCvSharp.Point(110, 90), 30, Scalar.All(0), 2);

        var circleRoi = new Circle
        {
            Pose = new Pose(new Vector(110, 90), new Vector(1, 0)),
            Radius = 40
        };

        var pool = new ResourcePool();
        var graph = new Graph<IExecutableNode>();

        var srcNode = new SourceMatOperation(pool, graph, source);
        var crop = new CropImageFromRegionOperation(pool, graph);
        crop.Parameters["Region"] = circleRoi;
        var detect = new ExtractCircleOperation(pool, graph);

        graph.AddNode(srcNode);
        graph.AddNode(crop);
        graph.AddNode(detect);
        graph.Connect(srcNode, crop);
        graph.Connect(crop, detect);

        await graph.ExecuteAsync(TestContext.Current.CancellationToken);

        var cropped = pool.Get<Mat>(crop.Id);
        var detected = pool.Get<Circle>(detect.Id);

        Assert.InRange(cropped.Width, 78, 82);
        Assert.InRange(cropped.Height, 78, 82);
        Assert.InRange(detected.Pose.Position.X, 40 - 4, 40 + 4);
        Assert.InRange(detected.Pose.Position.Y, 40 - 4, 40 + 4);
        Assert.InRange(detected.Radius, 26, 34);
    }

    private static async Task<TOutput> ExecuteMatOperationAsync<TOutput>(
        Mat source,
        Func<ResourcePool, Graph<IExecutableNode>, Node<Mat, TOutput>> operationFactory,
        Action<Node<Mat, TOutput>> configure)
        where TOutput : notnull
    {
        var pool = new ResourcePool();
        var graph = new Graph<IExecutableNode>();

        var sourceNode = new SourceMatOperation(pool, graph, source);
        var operation = operationFactory(pool, graph);
        configure(operation);

        graph.AddNode(sourceNode);
        graph.AddNode(operation);
        graph.Connect(sourceNode, operation);

        await graph.ExecuteAsync(TestContext.Current.CancellationToken);
        return pool.Get<TOutput>(operation.Id);
    }

    private static Line CreateLine(double x1, double y1, double x2, double y2)
    {
        var start = new Vector(x1, y1);
        var delta = new Vector(x2 - x1, y2 - y1);
        return new Line
        {
            Pose = new Pose(start, delta.Normalize()),
            Length = delta.M
        };
    }

    private sealed class SourceMatOperation(ResourcePool pool, Graph<IExecutableNode> graph, Mat source)
        : Node<Empty, Mat>(pool, graph)
    {
        protected override Mat ExecuteInternal(IReadOnlyList<Empty> inputs, IReadOnlyDictionary<string, object> parameters, CancellationToken ct)
            => source.Clone();
    }

    private sealed class SourceLineOperation(ResourcePool pool, Graph<IExecutableNode> graph, Line line)
        : Node<Empty, Line>(pool, graph)
    {
        protected override Line ExecuteInternal(IReadOnlyList<Empty> inputs, IReadOnlyDictionary<string, object> parameters, CancellationToken ct)
            => line;
    }
}
