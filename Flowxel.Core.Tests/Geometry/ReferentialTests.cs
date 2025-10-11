
using Flowxel.Core.Geometry.Primitives;

namespace Flowxel.Core.Tests.Geometry;

public class ReferentialTests
{
    
    private const double Tolerance = 1e-10;
    [Fact]
    public void TestReferentialToWorld()
    {
        var ref1 = new Referential(); //Identity

        var ref2 = new Referential
        {
            Parent = ref1,
            Transform = Matrix.Translate(1, 1)
        };

        var ref3 = new Referential
        {
            Parent = ref2,
            Transform = Matrix.Translate(1, 1)
        };

        Assert.Equal(2, ref3.ToWorld().M13);
        Assert.Equal(2, ref3.ToWorld().M23);
    }

    [Fact]
    public void TestPointInReferential()
    {
        var ref1 = new Referential(); //Identity

        var ref2 = new Referential
        {
            Parent = ref1,
            Transform = Matrix.Translate(1, 1)
        };

        var ref3 = new Referential
        {
            Parent = ref2,
            Transform = Matrix.Translate(1, 1)
        };

        var point = new Vector(0, 0, ref3);

        var pointInWorld = point.Transform(point.Referential.ToWorld());

        Assert.Equal(2, pointInWorld.X);
        Assert.Equal(2, pointInWorld.Y);
    }

    [Fact]
    public void TestVectorTransformToWorld()
    {
        var ref1 = new Referential();
        var ref2 = new Referential
        {
            Parent = ref1,
            Transform = Matrix.Rotate(Math.PI / 2)
        };

        var vector = new Vector(1, 1, ref2);

        var vectorToWorld = vector.ToReferential(ref1);
        
        
        Assert.InRange(vectorToWorld.X, -1 - Tolerance, -1 + Tolerance);
        Assert.InRange(vectorToWorld.Y, 1 - Tolerance, 1 + Tolerance);
        
    }
}