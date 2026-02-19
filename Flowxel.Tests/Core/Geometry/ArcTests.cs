using Flowxel.Core.Geometry.Primitives;
using Flowxel.Core.Geometry.Shapes;

namespace Flowxel.Core.Tests.Geometry;

public class ArcTests
{
    private const double Tolerance = 1e-6;

    [Fact]
    public void Arc_StoresRadiusAndAngles()
    {
        var arc = new Arc
        {
            Pose = new Pose(new Vector(10, 20), new Vector(1, 0)),
            Radius = 15,
            StartAngle = Math.PI / 6,
            EndAngle = 5 * Math.PI / 6
        };

        Assert.InRange(arc.Radius, 15 - Tolerance, 15 + Tolerance);
        Assert.InRange(arc.StartAngle, Math.PI / 6 - Tolerance, Math.PI / 6 + Tolerance);
        Assert.InRange(arc.EndAngle, 5 * Math.PI / 6 - Tolerance, 5 * Math.PI / 6 + Tolerance);
        Assert.InRange(arc.Pose.Position.X, 10 - Tolerance, 10 + Tolerance);
        Assert.InRange(arc.Pose.Position.Y, 20 - Tolerance, 20 + Tolerance);
    }

    [Fact]
    public void Arc_ExposesBaseStyleProperties()
    {
        var arc = new Arc
        {
            LineWeight = 2.5,
            Fill = true
        };

        Assert.InRange(arc.LineWeight, 2.5 - Tolerance, 2.5 + Tolerance);
        Assert.True(arc.Fill);
    }
}
