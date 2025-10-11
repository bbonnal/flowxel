
using Flowxel.Core.Geometry.Primitives;

namespace Flowxel.Core.Tests.Geometry;

public class VectorTests
{
    private const double Tolerance = 1e-10;
    
 [Fact]
    public void TestVectorAddition()
    {
        var vector1 = new Vector(1, 2);
        var vector2 = new Vector(3, 4);
        var transformed = vector1 + vector2;

        Assert.Equal(4, transformed.X);
        Assert.Equal(6, transformed.Y);
    }

    [Fact]
    public void TestVectorSubtraction()
    {
        var vector1 = new Vector(1, 2);
        var vector2 = new Vector(3, 4);
        var transformed = vector1 - vector2;

        Assert.Equal(-2, transformed.X);
        Assert.Equal(-2, transformed.Y);
    }

    [Fact]
    public void TestVectorNormalize()
    {
        var vector = new Vector(1, 1);

        // Double-Check Magnitude before
        Assert.InRange(vector.M, Math.Sqrt(2) - Tolerance, Math.Sqrt(2) + Tolerance);

        var normalized = vector.Normalize();

        // Check Magnitude after
        Assert.InRange(normalized.M, 1 - Tolerance, 1 + Tolerance);
    }

    [Fact]
    public void TestVectorCrossProduct()
    {
        var vector1 = new Vector(1, 0);
        var vector2 = new Vector(0, 1);

        var pCross = Vector.Cross(vector1, vector2);
        var nCross = Vector.Cross(vector2, vector1);

        Assert.Equal(+1, pCross);
        Assert.Equal(-1, nCross);
    }

    [Fact]
    public void TestVectorDotProduct()
    {
        Vector vector1;
        Vector vector2;
        double dotProduct;

        vector1 = new Vector(1, 0);
        vector2 = new Vector(0, 1);
        dotProduct = Vector.Dot(vector1, vector2);
        Assert.Equal(0, dotProduct);

        vector1 = new Vector(1, 1);
        vector2 = new Vector(1, 1);
        dotProduct = Vector.Dot(vector1, vector2);
        Assert.Equal(2, dotProduct);
    }

    [Fact]
    public void TestVectorRotation()
    {
        var vector = new Vector(1, 0);
        var angle = Math.PI / 2;

        var transformed = vector.Rotate(angle);

        Assert.InRange(transformed.X, 0 - Tolerance, 0 + Tolerance);
        Assert.InRange(transformed.Y, 1 - Tolerance, 1 + Tolerance);
    }
    
    [Fact]
    public void TestVectorAngleTo()
    {
        var vector1 = new Vector(1, 0);
        var vector2 = new Vector(0, 1);

        var anglePos = vector1.AngleTo(vector2);
        var angleNeg = vector2.AngleTo(vector1);

        Assert.InRange(anglePos, Math.PI/2 - Tolerance, Math.PI/2 + Tolerance);
        Assert.InRange(angleNeg, -Math.PI/2 - Tolerance, -Math.PI/2 + Tolerance);

    }
    
    [Fact]
    public void TestVectorAngleBetween()
    {
        var vector1 = new Vector(1, -1);
        var vector2 = new Vector(1, 1);

        var angle1 = vector1.AngleBetween(vector2);
        var angle2 = vector2.AngleBetween(vector1);

        Assert.InRange(angle1, Math.PI/2 - Tolerance, Math.PI/2 + Tolerance);
        Assert.InRange(angle2, Math.PI/2 - Tolerance, Math.PI/2 + Tolerance);

    }
    
}