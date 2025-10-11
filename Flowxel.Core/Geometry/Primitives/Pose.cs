namespace Flowxel.Core.Geometry.Primitives;

/// <summary>
/// Represents a geometric pose in two-dimensional space, supporting transformations such as translation,
/// rotation, and conversion between local and world coordinate systems.
/// </summary>
/// <remarks>
/// A pose is defined by a relative transformation matrix and an optional referential. It encapsulates
/// spatial information such as position and orientation and provides methods to apply transformations
/// and retrieve state in both local and world coordinate systems.
/// </remarks>
public readonly struct Pose
{
    /// <summary>
    /// Gets the referential associated with the <see cref="Pose"/>.
    /// The referential defines the parent coordinate system to which
    /// the pose is relative. If no parent referential is provided,
    /// a default identity referential is used.
    /// </summary>
    /// <remarks>
    /// The referential provides the ability to work with hierarchical
    /// transformations, enabling translation and rotation within local
    /// and world coordinate systems. It maintains a parent-child relationship
    /// where transformations are cumulative across the hierarchy.
    /// </remarks>
    public Referential Referential { get; }

    /// <summary>
    /// Gets the relative transformation matrix describing the pose relative to its
    /// parent coordinate system. This matrix encapsulates translation and rotation
    /// information for the pose in the context of its referential.
    /// </summary>
    /// <remarks>
    /// The <see cref="RelativePose"/> property is used to express the position and
    /// orientation of the pose relative to a parent referential. This allows for the
    /// representation of hierarchical transformations and conversions between local
    /// and global coordinate systems. When combined with the referential's world
    /// transformation, the pose can be fully resolved in an absolute coordinate system.
    /// </remarks>
    public Matrix RelativePose { get; }

    /// <summary>
    /// Gets the matrix representing the conversion from the local coordinate system
    /// defined by the <see cref="Pose"/> to the world coordinate system.
    /// </summary>
    /// <remarks>
    /// The resulting matrix is the combination of the referential's world transformation
    /// matrix (if a referential is provided) and the pose's relative transformation matrix.
    /// If no referential is specified, an identity matrix is used as the default world transformation.
    /// This provides an absolute transformation matrix in a global reference frame.
    /// </remarks>
    public Matrix ToWorld => (Referential?.ToWorld() ?? Matrix.Identity) * RelativePose;

    /// <summary>
    /// Gets the transformation matrix for converting positions and orientations
    /// from world coordinates to the local coordinate system of the <see cref="Pose"/>.
    /// </summary>
    /// <remarks>
    /// The local transformation matrix is obtained by inverting the world transformation matrix.
    /// This allows spatial data expressed in the world coordinate system to be interpreted relative
    /// to the local pose, supporting operations such as hierarchical transformations
    /// and relative positioning.
    /// </remarks>
    public Matrix ToLocal => ToWorld.Invert();

    /// <summary>
    /// Gets the positional component of the <see cref="Pose"/> in the associated referential.
    /// The position is derived from the translation values within the transformation matrix
    /// used to define the relative pose of the object.
    /// </summary>
    /// <remarks>
    /// The position is represented as a <see cref="Vector"/> and is calculated using the
    /// translation components (M13 and M23) of the relative transformation matrix.
    /// This property provides the ability to determine the spatial location of the
    /// <see cref="Pose"/> within its referential.
    /// </remarks>
    public Vector Position => new(RelativePose.M13, RelativePose.M23, Referential);

    /// <summary>
    /// Gets the orientation component of the <see cref="Pose"/> represented as a vector.
    /// The orientation is derived from the relative pose matrix and indicates the direction
    /// or rotational alignment of the pose within its referential or parent coordinate system.
    /// </summary>
    /// <remarks>
    /// The orientation vector is calculated from specific elements of the relative pose matrix,
    /// which encode information about rotation or direction. It is expressed in the same
    /// referential as the pose itself, allowing operations such as rotation or transformation
    /// within hierarchical structures.
    /// </remarks>
    public Vector Orientation => new(RelativePose.M11, RelativePose.M21, Referential);

    /// <summary>
    /// Represents a geometric pose in 2D space, encapsulating position and orientation.
    /// </summary>
    /// <remarks>
    /// This structure supports transformations such as translation, rotation,
    /// and composite transformations. It also allows conversion between local
    /// and world coordinate systems based on a referential hierarchy.
    /// </remarks>
    public Pose(Matrix pose, Referential? parent = null)
    {
        Referential = parent ?? new Referential();
        RelativePose = pose.Normalize();
    }

    /// <summary>
    /// Represents a pose in 2D space, defined by a position and orientation vector, and optionally linked to a parent referential.
    /// </summary>
    /// <remarks>
    /// This struct is used to represent spatial relationships and can support operations like transformation, rotation, and translation.
    /// The pose's position and orientation are stored relative to its parent referential, if provided.
    /// </remarks>
    public Pose(Vector p, Vector v, Referential? parent = null)
    {
        Referential = parent ?? new Referential();
        var xAxis = v.Normalize();
        var yAxis = new Vector(-xAxis.Y, xAxis.X);

        var m = new Matrix(
            xAxis.X, yAxis.X, p.X,
            xAxis.Y, yAxis.Y, p.Y,
            0, 0, 1
        );
        RelativePose = m.Normalize();
    }

    /// <summary>
    /// Translates the current pose by a given vector in its local coordinate system.
    /// </summary>
    /// <param name="v">The vector by which to translate the pose, defined in the pose's referential system.</param>
    /// <returns>A new <see cref="Pose"/> instance representing the translated pose.</returns>
    public Pose Translate(Vector v)
    {
        var vLocal = v.ToReferential(Referential);
        var t = Matrix.Translate(vLocal.X, vLocal.Y);
        return new Pose(t * RelativePose, Referential);
    }

    /// <summary>
    /// Performs a rotation on the current pose by a specified angle and returns the resulting pose.
    /// </summary>
    /// <param name="angle">The angle, in radians, by which the pose should be rotated.</param>
    /// <returns>A new <see cref="Pose"/> instance that represents the result of the rotation.</returns>
    public Pose Rotate(double angle)
    {
        var r = Matrix.Rotate(angle);
        return new Pose(r * RelativePose, Referential);
    }

    /// <summary>
    /// Applies an in-place rotation to the current pose by the specified angle.
    /// </summary>
    /// <param name="angle">The angle of rotation, in radians.</param>
    /// <returns>A new <see cref="Pose"/> instance representing the rotated pose, with the same origin.</returns>
    public Pose RotateInPlace(double angle)
    {
        var r = Matrix.Rotate(angle);
        var t = Matrix.Translate(this.Position.X, this.Position.Y);
        return new Pose(t * r * t.Invert() * RelativePose, Referential);
    }

    /// <summary>
    /// Transforms the current pose using the specified transformation matrix.
    /// </summary>
    /// <param name="m">A <see cref="Matrix"/> representing the transformation to be applied to the pose.</param>
    /// <returns>
    /// A new <see cref="Pose"/> instance representing the transformed pose with the applied matrix.
    /// </returns>
    public Pose Transform(Matrix m)
    {
        return new Pose(m * RelativePose, Referential);
    }
}