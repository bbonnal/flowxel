namespace Flowxel.Core.Geometry.Primitives;

/// <summary>
/// Represents a two-dimensional vector with operations for geometric transformations.
/// </summary>
/// <remarks>
/// This struct supports operations such as addition, subtraction, scaling, normalization, rotation,
/// dot product, cross product, transformations, and calculating angles between vectors. The vector is
/// also associated with a referential coordinate system.
/// </remarks>
public readonly struct Vector(double x, double y, Referential? referential = null)
{
    /// <summary>
    /// Gets the <see cref="Referential"/> associated with the current vector.
    /// </summary>
    /// <remarks>
    /// The referential represents the coordinate system in which this vector is defined.
    /// If no referential is explicitly provided, a default referential instance is created.
    /// Referential transformations can be used to convert vectors between different coordinate systems.
    /// </remarks>
    public Referential Referential { get; } = referential ?? new Referential();

    /// <summary>
    /// Gets the X-coordinate of the vector.
    /// </summary>
    /// <remarks>
    /// This property represents the horizontal component of the vector.
    /// The X-coordinate, together with the Y-coordinate, defines the vector's position
    /// in a two-dimensional space.
    /// </remarks>
    public double X { get; } = x;

    /// <summary>
    /// Gets the y-coordinate of the vector.
    /// </summary>
    /// <remarks>
    /// The y-coordinate represents the vertical component of the vector in its defined referential coordinate system.
    /// This value is immutable and initialized during the creation of the vector.
    /// </remarks>
    public double Y { get; } = y;

    /// <summary>
    /// Gets the magnitude of the vector.
    /// </summary>
    /// <remarks>
    /// The magnitude (or length) of the vector is computed as the square root of the sum of the squares of its X and Y components.
    /// This value represents the Euclidean distance from the origin to the point represented by the vector in a two-dimensional space.
    /// </remarks>
    public double M => Math.Sqrt(X * X + Y * Y);

    /// <summary>
    /// Determines whether the specified object is equal to the current vector instance.
    /// </summary>
    /// <param name="obj">The object to compare with the current vector.</param>
    /// <returns>true if the specified object is a <see cref="Vector"/> and is equal to the current vector; otherwise, false.</returns>
    public override bool Equals(object? obj) => obj is Vector other && Equals(other);

    /// <summary>
    /// Determines whether the specified vector is equal to the current vector instance.
    /// </summary>
    /// <param name="other">The vector to compare with the current vector.</param>
    /// <returns>true if the specified vector has the same coordinates and referential as the current vector; otherwise, false.</returns>
    public bool Equals(Vector other) => Referential.Equals(other.Referential) && X.Equals(other.X) && Y.Equals(other.Y);

    /// <summary>
    /// Returns the hash code for the current vector instance.
    /// </summary>
    /// <returns>A 32-bit signed integer hash code representing the current vector.</returns>
    public override int GetHashCode() => HashCode.Combine(X, Y);

    /// <summary>
    /// Determines whether two <see cref="Vector"/> instances are equal.
    /// </summary>
    /// <param name="v1">The first vector to compare.</param>
    /// <param name="v2">The second vector to compare.</param>
    /// <returns>true if the two vectors are equal; otherwise, false.</returns>
    public static bool operator ==(Vector v1, Vector v2) => v1.Equals(v2);

    /// <summary>
    /// Determines whether two <see cref="Vector"/> instances are not equal.
    /// </summary>
    /// <param name="v1">The first vector to compare.</param>
    /// <param name="v2">The second vector to compare.</param>
    /// <returns>true if the vectors are not equal; otherwise, false.</returns>
    public static bool operator !=(Vector v1, Vector v2) => !v1.Equals(v2);

    /// <summary>
    /// Adds two vectors and returns the resulting vector.
    /// </summary>
    /// <param name="v1">The first vector.</param>
    /// <param name="v2">The second vector.</param>
    /// <returns>A new <see cref="Vector"/> representing the sum of the two vectors.</returns>
    public static Vector operator +(Vector v1, Vector v2)
        => new(v1.X + v2.X, v1.Y + v2.Y);

    /// <summary>
    /// Subtracts one vector from another.
    /// </summary>
    /// <param name="v1">The vector to subtract from.</param>
    /// <param name="v2">The vector to subtract.</param>
    /// <returns>A new vector that represents the difference between the two vectors.</returns>
    public static Vector operator -(Vector v1, Vector v2)
        => new(v1.X - v2.X, v1.Y - v2.Y);

    /// <summary>
    /// Calculates the dot product of two vectors.
    /// </summary>
    /// <param name="v1">The first vector.</param>
    /// <param name="v2">The second vector.</param>
    /// <returns>The dot product of the two vectors.</returns>
    public static double Dot(Vector v1, Vector v2)
        => v1.X * v2.X + v1.Y * v2.Y;

    /// <summary>
    /// Calculates the cross product of two two-dimensional vectors.
    /// </summary>
    /// <param name="v1">The first vector.</param>
    /// <param name="v2">The second vector.</param>
    /// <returns>The cross product of the two vectors.</returns>
    public static double Cross(Vector v1, Vector v2)
        => v1.X * v2.Y - v1.Y * v2.X;

    /// <summary>
    /// Calculates the signed angle, in radians, from the current vector to another vector.
    /// </summary>
    /// <param name="v2">The vector to calculate the angle to.</param>
    /// <returns>The signed angle in radians between the current vector and the specified vector,
    /// using a counterclockwise positive convention.</returns>
    public double AngleTo(Vector v2)
        => Math.Atan2(Cross(this, v2), Dot(this, v2));

    /// <summary>
    /// Calculates the angle between the current vector and another vector in radians.
    /// </summary>
    /// <param name="v2">The vector to calculate the angle with.</param>
    /// <returns>The angle between the two vectors in radians, ranging from 0 to π.</returns>
    public double AngleBetween(Vector v2)
        => Math.Acos(Math.Clamp(Dot(this, v2) / (M * v2.M), -1.0, 1.0));

    /// <summary>
    /// Returns a unit vector in the same direction as the current vector.
    /// </summary>
    /// <returns>A new <see cref="Vector"/> instance with a magnitude of 1, pointing in the same direction as the current vector.</returns>
    public Vector Normalize() => new(X / M, Y / M);

    /// <summary>
    /// Scales the vector by a specified scalar value.
    /// </summary>
    /// <param name="s">The scalar value by which to scale the vector.</param>
    /// <returns>A new vector scaled by the specified scalar.</returns>
    public Vector Scale(double s) => new(X * s, Y * s);

    /// <summary>
    /// Rotates the vector by a specified angle in radians.
    /// </summary>
    /// <param name="angle">The angle, in radians, by which to rotate the vector.</param>
    /// <returns>A new <see cref="Vector"/> instance representing the rotated vector.</returns>
    public Vector Rotate(double angle) => new(X * Math.Cos(angle) - Y * Math.Sin(angle), Y * Math.Cos(angle) + X * Math.Sin(angle));

    /// <summary>
    /// Translates the vector by adding the components of the specified vector to the current vector.
    /// </summary>
    /// <param name="v">The vector to add to the current vector.</param>
    /// <returns>A new <see cref="Vector"/> that represents the result of the translation.</returns>
    public Vector Translate(Vector v) => new(X + v.X, Y + v.Y);

    /// <summary>
    /// Transforms the vector using the specified transformation matrix.
    /// </summary>
    /// <param name="m">The transformation matrix to be applied to the vector.</param>
    /// <returns>A new vector resulting from the transformation of the current vector by the given matrix.</returns>
    public Vector Transform(Matrix m)
        => new(
            m.M11 * X + m.M12 * Y + m.M13,
            m.M21 * X + m.M22 * Y + m.M23,
            Referential
        );

    /// <summary>
    /// Converts the current vector to the specified referential.
    /// </summary>
    /// <param name="targetRef">The target referential to which the vector is transformed.</param>
    /// <returns>A new vector transformed into the specified referential.</returns>
    public Vector ToReferential(Referential targetRef)
        => new(
            Transform(targetRef.ToLocal() * Referential.ToWorld()).X,
            Transform(targetRef.ToLocal() * Referential.ToWorld()).Y,
            targetRef
        );
}