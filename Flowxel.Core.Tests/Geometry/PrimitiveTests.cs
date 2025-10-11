
using Flowxel.Core.Geometry.Primitives;

namespace Flowxel.Core.Tests.Geometry;

public class PrimitivesTests
{
    private const double Tolerance = 1e-10;

    [Fact]
    public void TestMatrixIdentity()
    {
        var identity = Matrix.Identity;

        Assert.Equal(1, identity.M11);
        Assert.Equal(0, identity.M12);
        Assert.Equal(0, identity.M13);
        Assert.Equal(0, identity.M21);
        Assert.Equal(1, identity.M22);
        Assert.Equal(0, identity.M23);
        Assert.Equal(1, identity.M33); // implicit bottom row
    }

    [Fact]
    public void TestMatrixTranslate()
    {
        // Move in the positive
        var translate = Matrix.Translate(3, 5);
        var point = new Vector(1, 2);
        var transformed = point.Transform(translate);

        Assert.Equal(4, transformed.X);
        Assert.Equal(7, transformed.Y);

        // Move in the negative
        translate = Matrix.Translate(-3, -5);
        point = new Vector(1, 2);
        transformed = point.Transform(translate);

        Assert.Equal(-2, transformed.X);
        Assert.Equal(-3, transformed.Y);
    }

    [Fact]
    public void TestMatrixScale()
    {
        Matrix scale;
        Vector vector;
        Vector transformed;

        // Scale up
        scale = Matrix.Scale(2, 2);
        vector = new Vector(2, 2);
        transformed = vector.Transform(scale);

        Assert.Equal(4, transformed.X);
        Assert.Equal(4, transformed.Y);

        // Scale down
        scale = Matrix.Scale(0.5, 0.5);
        vector = new Vector(2, 2);
        transformed = vector.Transform(scale);

        Assert.Equal(1, transformed.X);
        Assert.Equal(1, transformed.Y);
    }

    [Fact]
    public void TestMatrixRotatePoint()
    {
        // Rotate positive
        var rotate = Matrix.Rotate(Math.PI / 2);
        var point = new Vector(1, 0);
        var transformed = point.Transform(rotate);

        Assert.InRange(transformed.X, 0 - Tolerance, 0 + Tolerance);
        Assert.InRange(transformed.Y, 1 - Tolerance, 1 + Tolerance);

        // Rotate negative
        rotate = Matrix.Rotate(-Math.PI / 2);
        point = new Vector(1, 0);
        transformed = point.Transform(rotate);

        Assert.InRange(transformed.X, 0 - Tolerance, 0 + Tolerance);
        Assert.InRange(transformed.Y, -1 - Tolerance, -1 + Tolerance);


        // Rotate positive more than one turn
        rotate = Matrix.Rotate(5 * Math.PI / 2);
        point = new Vector(1, 0);
        transformed = point.Transform(rotate);

        Assert.InRange(transformed.X, 0 - Tolerance, 0 + Tolerance);
        Assert.InRange(transformed.Y, 1 - Tolerance, 1 + Tolerance);

        // Rotate negative more than one turn
        rotate = Matrix.Rotate(-5 * Math.PI / 2);
        point = new Vector(1, 0);
        transformed = point.Transform(rotate);

        Assert.InRange(transformed.X, 0 - Tolerance, 0 + Tolerance);
        Assert.InRange(transformed.Y, -1 - Tolerance, -1 + Tolerance);
    }

    [Fact]
    public void TestMatrixRotateVector()
    {
        // Rotate positive
        var rotate = Matrix.Rotate(Math.PI / 2);
        var vector = new Vector(1, 1);
        var transformed = vector.Transform(rotate);

        Assert.InRange(transformed.X, -1 - Tolerance, -1 + Tolerance);
        Assert.InRange(transformed.Y, 1 - Tolerance, 1 + Tolerance);

        // Rotate negative
        rotate = Matrix.Rotate(-Math.PI / 2);
        vector = new Vector(1, 1);
        transformed = vector.Transform(rotate);

        Assert.InRange(transformed.X, 1 - Tolerance, 1 + Tolerance);
        Assert.InRange(transformed.Y, -1 - Tolerance, -1 + Tolerance);
    }

    [Fact]
    public void TestMatrixTransformForwardAndBackPoint()
    {
        var translate = Matrix.Translate(3, 0);
        var scale = Matrix.Scale(2, 2);
        var rotate = Matrix.Rotate(Math.PI / 2);

        // Apply scale then translate then rotate point a.k.a rotate(translate(scale(point)))
        var transformMatrix = rotate * translate * scale;
        var point = new Vector(1, 1);

        var transformedForward = point.Transform(transformMatrix);

        Assert.InRange(transformedForward.X, -2 - Tolerance, -2 + Tolerance);
        Assert.InRange(transformedForward.Y, 5 - Tolerance, 5 + Tolerance);

        // Apply the inverse transformation to bring point back to its original place
        var invTransfromMatrix = transformMatrix.Invert();

        var transformedBack = transformedForward.Transform(invTransfromMatrix);

        Assert.InRange(transformedBack.X, point.X - Tolerance, point.X + Tolerance);
        Assert.InRange(transformedBack.Y, point.Y - Tolerance, point.Y + Tolerance);
    }

    [Fact]
    public void TestPointTranslation()
    {
        var point = new Vector(0, 0);
        var vector = new Vector(1, 1);

        var transformed = point.Translate(vector);

        Assert.Equal(1, transformed.X);
        Assert.Equal(1, transformed.Y);
    }


}