namespace Flowxel.Core.Geometry.Primitives;

/// <summary>
/// Represents a 3x3 matrix used for 2D affine transformations, such as translation, scaling, and rotation.
/// </summary>
/// <remarks>
/// The matrix is row-major and follows the structure:
/// | M11 M12 M13 |
/// | M21 M22 M23 |
/// |  0   0   1  |
/// It includes operations such as translation, scaling, rotation, and multiplication.
/// </remarks>
public readonly struct Matrix(
    double m11,
    double m12,
    double m13,
    double m21,
    double m22,
    double m23,
    double m31,
    double m32,
    double m33)
{
    /// <summary>
    /// Gets the element at the first row and first column of the 3x3 matrix.
    /// </summary>
    /// <remarks>
    /// Represents the scaling or rotation factor along the x-axis in a 2D affine transformation.
    /// This element contributes to the matrix’s top-left component, commonly involved in scaling, rotation, and shearing operations.
    /// </remarks>
    public double M11 { get; } = m11;

    /// <summary>
    /// Gets the element at the first row and second column of the 3x3 matrix.
    /// </summary>
    /// <remarks>
    /// Represents the horizontal shear factor or contributes to the transformation coupling between the x and y axes
    /// in a 2D affine transformation.
    /// </remarks>
    public double M12 { get; } = m12;

    /// <summary>
    /// Gets the element at the first row and third column of the 3x3 matrix.
    /// </summary>
    /// <remarks>
    /// Represents the translation component along the x-axis in a 2D affine transformation.
    /// It defines the horizontal displacement applied to transformed coordinates.
    /// </remarks>
    public double M13 { get; } = m13;

    /// <summary>
    /// Gets the element at the second row and first column of the 3x3 matrix.
    /// </summary>
    /// <remarks>
    /// Represents the vertical shear or scaling factor associated with the x-axis in a 2D affine transformation.
    /// This element contributes to the transformation of the y-coordinate based on x.
    /// </remarks>
    public double M21 { get; } = m21;

    /// <summary>
    /// Gets the element at the second row and second column of the 3x3 matrix.
    /// </summary>
    /// <remarks>
    /// Represents the scaling or rotation factor along the y-axis in a 2D affine transformation.
    /// This element contributes to the matrix’s central component for scaling, rotation, and shearing operations.
    /// </remarks>
    public double M22 { get; } = m22;

    /// <summary>
    /// Gets the element at the second row and third column of the 3x3 matrix.
    /// </summary>
    /// <remarks>
    /// Represents the translation component along the y-axis in a 2D affine transformation.
    /// It defines the vertical displacement applied to transformed coordinates.
    /// </remarks>
    public double M23 { get; } = m23;

    /// <summary>
    /// Gets the element at the third row and first column of the 3x3 matrix.
    /// </summary>
    /// <remarks>
    /// Represents the element of the homogeneous row associated with x-axis translation or perspective adjustment.
    /// In standard affine matrices, this value is typically 0.
    /// </remarks>
    public double M31 { get; } = m31;

    /// <summary>
    /// Gets the element at the third row and second column of the 3x3 matrix.
    /// </summary>
    /// <remarks>
    /// Represents the element of the homogeneous row associated with y-axis translation or perspective adjustment.
    /// In standard affine matrices, this value is typically 0.
    /// </remarks>
    public double M32 { get; } = m32;

    /// <summary>
    /// Gets the element at the third row and third column of the 3x3 matrix.
    /// </summary>
    /// <remarks>
    /// Represents the homogeneous coordinate of the 3x3 matrix.
    /// In standard 2D affine transformations, this value is fixed at 1 to enable translation and scaling operations.
    /// </remarks>
    public double M33 { get; } = m33;


    /// <summary>
    /// Gets the identity matrix for 2D affine transformations.
    /// </summary>
    /// <remarks>
    /// The identity matrix is a special matrix that acts as the neutral element in matrix multiplication.
    /// It does not alter a vector or matrix when applied, and it is represented as:
    /// | 1  0  0 |
    /// | 0  1  0 |
    /// | 0  0  1 |
    /// </remarks>
    public static Matrix Identity
        => new(
            1, 0, 0,
            0, 1, 0,
            0, 0, 1
        );

    /// <summary>
    /// Creates a translation matrix that moves points by specified offsets along the x and y axes.
    /// </summary>
    /// <param name="dx">The translation offset along the x-axis.</param>
    /// <param name="dy">The translation offset along the y-axis.</param>
    /// <returns>A new <see cref="Matrix"/> representing the translation transformation.</returns>
    public static Matrix Translate(double dx, double dy)
        => new(
            1, 0, dx,
            0, 1, dy,
            0, 0, 1
        );

    /// <summary>
    /// Creates a scaling matrix that scales points by specified factors along the x and y axes.
    /// </summary>
    /// <param name="sx">The scaling factor along the x-axis.</param>
    /// <param name="sy">The scaling factor along the y-axis.</param>
    /// <returns>A new <see cref="Matrix"/> representing the scaling transformation.</returns>
    public static Matrix Scale(double sx, double sy)
        => new(
            sx, 0, 0,
            0, sy, 0,
            0, 0, 1
        );

    /// <summary>
    /// Creates a rotation matrix that rotates points by a specified angle around the origin.
    /// </summary>
    /// <param name="angle">The angle of rotation, in radians. Positive values indicate counterclockwise rotation, and negative values indicate clockwise rotation.</param>
    /// <returns>A new <see cref="Matrix"/> representing the rotation transformation.</returns>
    public static Matrix Rotate(double angle)
        => new(
            Math.Cos(angle), -Math.Sin(angle), 0,
            Math.Sin(angle), Math.Cos(angle), 0,
            0, 0, 1);

    /// <summary>
    /// Overrides the multiplication operator to perform matrix multiplication between two <see cref="Matrix"/> structures.
    /// </summary>
    /// <param name="a">The first matrix operand.</param>
    /// <param name="b">The second matrix operand.</param>
    /// <returns>A new <see cref="Matrix"/> representing the result of the multiplication.</returns>
    public static Matrix operator *(Matrix a, Matrix b)
        => new(
            a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31,
            a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32,
            a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33,
            a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31,
            a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32,
            a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33,
            a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31,
            a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32,
            a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33
        );

    /// <summary>
    /// Computes the inverse of the matrix if it is invertible.
    /// </summary>
    /// <returns>A new <see cref="Matrix"/> representing the inverse of the current matrix.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the matrix is not invertible.</exception>
    public Matrix Invert()
    {
        double det = M11 * (M22 * M33 - M23 * M32)
                     - M12 * (M21 * M33 - M23 * M31)
                     + M13 * (M21 * M32 - M22 * M31);

        if (Math.Abs(det) < 1e-12)
            throw new InvalidOperationException("Matrix is not invertible");

        double invDet = 1.0 / det;

        // Compute the adjugate matrix and multiply by 1/det
        return new Matrix(
            (M22 * M33 - M23 * M32) * invDet,
            (M13 * M32 - M12 * M33) * invDet,
            (M12 * M23 - M13 * M22) * invDet,
            (M23 * M31 - M21 * M33) * invDet,
            (M11 * M33 - M13 * M31) * invDet,
            (M13 * M21 - M11 * M23) * invDet,
            (M21 * M32 - M22 * M31) * invDet,
            (M12 * M31 - M11 * M32) * invDet,
            (M11 * M22 - M12 * M21) * invDet
        );
    }


    /// <summary>
    /// Normalizes the matrix such that the x and y axes become orthonormal vectors.
    /// </summary>
    /// <returns>A new <see cref="Matrix"/> with orthonormalized axes based on the original matrix.</returns>
    public Matrix Normalize()
    {
        var xAxis = new Vector(M11, M21).Normalize();
        var yAxis = new Vector(-xAxis.Y, xAxis.X); // perpendicular
        return new Matrix(
            xAxis.X, yAxis.X, M13,
            xAxis.Y, yAxis.Y, M23,
            0, 0, 1
        );
    }
}