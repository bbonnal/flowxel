namespace Flowxel.Core.Geometry.Primitives;

/// <summary>
/// Represents a hierarchical referential system used for geometric transformations.
/// </summary>
/// <remarks>
/// A referential defines a local coordinate system, including its relationship to a parent coordinate system if one exists.
/// It provides methods to convert transformations between local and world coordinate systems.
/// </remarks>
public class Referential
{
    /// <summary>
    /// Gets the transformation matrix that defines the local coordinate system
    /// of the referential relative to its parent or world space.
    /// </summary>
    /// <remarks>
    /// The Transform property specifies the geometric transformation applied to this referential.
    /// It typically includes translation, rotation, and scaling components.
    /// When composed with the parent referential's transform (if any), it defines the
    /// absolute position, rotation, and scale of this referential in world space.
    /// </remarks>
    public Matrix Transform { get; init; } = Matrix.Identity;

    /// <summary>
    /// Gets the parent referential of the current referential within the hierarchy.
    /// </summary>
    /// <remarks>
    /// The Parent property defines the hierarchical relationship between this referential
    /// and another referential. If a parent is defined, the current referential's transformation
    /// is relative to the parent's coordinate system. Otherwise, the transformation is relative
    /// to the global or world coordinate system.
    /// </remarks>
    public Referential? Parent { get; set; }

    /// <summary>
    /// Converts the current referential's local coordinate system to the world coordinate system.
    /// </summary>
    /// <returns>
    /// A <see cref="Matrix"/> representing the transformation from the local coordinate system
    /// to the world coordinate system. If the referential has a parent, the transformation
    /// is calculated recursively using the parent's world transformation.
    /// </returns>
    public Matrix ToWorld()
        => (Parent?.ToWorld() ?? Matrix.Identity) * Transform;

    /// <summary>
    /// Converts the world coordinate system to the current referential's local coordinate system.
    /// </summary>
    /// <returns>
    /// A <see cref="Matrix"/> representing the transformation from the world coordinate system
    /// to the local coordinate system. If the referential has a parent, the transformation is
    /// recursively computed using the parent's world transformation.
    /// </returns>
    public Matrix ToLocal()
        => ToWorld().Invert();
}