using System.Drawing;
using Flowxel.Core.Geometry.Primitives;

namespace Flowxel.Core.Tests.Geometry;

public class PoseTests
{
    private const double Tolerance = 1e-10;
    
    [Fact]
    public void TestPoseTranslation()
    {
        var pose = new Pose(new Vector(1, 1), new Vector(1, 1));
        var vector = new Vector(1, 1);

        var transformed = pose.Translate(vector);

        var expectedPose = new Pose(new Vector(2, 2), new Vector(1, 1));

        Assert.InRange(transformed.Position.X, expectedPose.Position.X - Tolerance, expectedPose.Position.X + Tolerance);
        Assert.InRange(transformed.Position.Y, expectedPose.Position.Y - Tolerance, expectedPose.Position.Y + Tolerance);
        Assert.InRange(transformed.Orientation.X, expectedPose.Orientation.X - Tolerance, expectedPose.Orientation.X + Tolerance);
        Assert.InRange(transformed.Orientation.Y, expectedPose.Orientation.Y - Tolerance, expectedPose.Orientation.Y + Tolerance);
    }

    [Fact]
    public void TestPoseRotate()
    {
        var pose = new Pose(new Vector(1, 1), new Vector(1, 1));
        var angle = Math.PI / 2;

        var transformed = pose.Rotate(angle);

        var expectedPose = new Pose(new Vector(-1, 1), new Vector(-1, 1));

        Assert.InRange(transformed.Position.X, expectedPose.Position.X - Tolerance, expectedPose.Position.X + Tolerance);
        Assert.InRange(transformed.Position.Y, expectedPose.Position.Y - Tolerance, expectedPose.Position.Y + Tolerance);
        Assert.InRange(transformed.Orientation.X, expectedPose.Orientation.X - Tolerance, expectedPose.Orientation.X + Tolerance);
        Assert.InRange(transformed.Orientation.Y, expectedPose.Orientation.Y - Tolerance, expectedPose.Orientation.Y + Tolerance);
    }

    [Fact]
    public void TestPoseTransform()
    {
        var pose = new Pose(new Vector(1, 1), new Vector(1, 1));
        var rotate = Matrix.Rotate(Math.PI / 2);

        var transformed = pose.Transform(rotate);

        var expectedPose = new Pose(new Vector(-1, 1), new Vector(-1, 1));


        Assert.InRange(transformed.Position.X, expectedPose.Position.X - Tolerance, expectedPose.Position.X + Tolerance);
        Assert.InRange(transformed.Position.Y, expectedPose.Position.Y - Tolerance, expectedPose.Position.Y + Tolerance);
        Assert.InRange(transformed.Orientation.X, expectedPose.Orientation.X - Tolerance, expectedPose.Orientation.X + Tolerance);
        Assert.InRange(transformed.Orientation.Y, expectedPose.Orientation.Y - Tolerance, expectedPose.Orientation.Y + Tolerance);
    }
    
    
    [Fact]
    public void TestPoseRotateInPlace()
    {
        var pose = new Pose(new Vector(1, 1), new Vector(1, 1));
        var angle = Math.PI / 2;

        var transformed = pose.RotateInPlace(angle);

        var expectedPose = new Pose(new Vector(1, 1), new Vector(-1, 1));

        Assert.InRange(transformed.Position.X, expectedPose.Position.X - Tolerance, expectedPose.Position.X + Tolerance);
        Assert.InRange(transformed.Position.Y, expectedPose.Position.Y - Tolerance, expectedPose.Position.Y + Tolerance);
        Assert.InRange(transformed.Orientation.X, expectedPose.Orientation.X - Tolerance, expectedPose.Orientation.X + Tolerance);
        Assert.InRange(transformed.Orientation.Y, expectedPose.Orientation.Y - Tolerance, expectedPose.Orientation.Y + Tolerance);
    }
    
}