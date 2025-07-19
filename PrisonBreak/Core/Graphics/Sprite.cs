#nullable enable
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PrisonBreak.Core.Graphics;

public class Sprite
{
    public PrisonBreak.Core.Physics.RectangleCollider? RectangleCollider { get; set; }

    /// <summary>
    /// Gets or Sets the source texture region represented by this sprite.
    /// </summary>
    public TextureRegion Region { get; set; }


    /// <summary>
    /// Gets or Sets the xy-coordinate origin point, relative to the top-left corner, of this sprite.
    /// </summary>
    /// <remarks>
    /// Default value is Vector2.Zero
    /// </remarks>
    public Vector2 Origin { get; set; } = Vector2.Zero;

    /// <summary>
    /// Gets or Sets the sprite effects to apply when rendering this sprite.
    /// </summary>
    /// <remarks>
    /// Default value is SpriteEffects.None
    /// </remarks>
    public SpriteEffects Effects { get; set; } = SpriteEffects.None;

    /// <summary>
    /// Gets or Sets the layer depth to apply when rendering this sprite.
    /// </summary>
    /// <remarks>
    /// Default value is 0.0f
    /// </remarks>
    public float LayerDepth { get; set; } = 0.0f;

    /// <summary>
    /// Gets the width, in pixels, of this sprite. 
    /// </summary>
    public float Width => Region.Width;

    /// <summary>
    /// Gets the height, in pixels, of this sprite.
    /// </summary>
    public float Height => Region.Height;

    /// <summary>
    /// Creates a new sprite.
    /// </summary>
    public Sprite() { }

    /// <summary>
    /// Creates a new sprite using the specified source texture region.
    /// </summary>
    /// <param name="region">The texture region to use as the source texture region for this sprite.</param>
    public Sprite(TextureRegion region)
    {
        Region = region;
    }

    /// <summary>
    /// Sets the origin of this sprite to the center.
    /// </summary>
    public void CenterOrigin()
    {
        Origin = new Vector2(Region.Width, Region.Height) * 0.5f;
    }


}
