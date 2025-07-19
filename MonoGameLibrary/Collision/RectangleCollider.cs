using Microsoft.Xna.Framework;

namespace MonoGameLibrary.RectangleCollider;

/// <summary>
/// Pure data class for rectangle collision detection.
/// Debug rendering should be handled by ComponentRenderSystem with DebugComponent in ECS.
/// </summary>
public class RectangleCollider
{
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public int CollisionHeight { get; set; }
    public int CollisionWidth { get; set; }

    /// <summary>
    /// Gets the collision rectangle.
    /// </summary>
    public Rectangle rectangleCollider => new Rectangle(PositionX, PositionY, CollisionWidth, CollisionHeight);
    
    /// <summary>
    /// Creates a new rectangle collider.
    /// </summary>
    public RectangleCollider()
    {

    }
    
    /// <summary>
    /// Creates a new rectangle collider with the specified parameters.
    /// </summary>
    /// <param name="x">X position of the collider.</param>
    /// <param name="y">Y position of the collider.</param>
    /// <param name="collisionWidth">Width of the collision area.</param>
    /// <param name="collisionHeight">Height of the collision area.</param>
    public RectangleCollider(int x, int y, int collisionWidth, int collisionHeight)
    {
        PositionX = x;
        PositionY = y;
        CollisionWidth = collisionWidth;
        CollisionHeight = collisionHeight;
    }
}