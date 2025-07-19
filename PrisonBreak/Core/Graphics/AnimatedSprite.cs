using Microsoft.Xna.Framework;

namespace PrisonBreak.Core.Graphics;

/// <summary>
/// Pure data class for animated sprites. 
/// Animation logic should be handled by AnimationSystem in ECS.
/// </summary>
public class AnimatedSprite
{
    /// <summary>
    /// Gets or Sets the animation definition for this animated sprite.
    /// </summary>
    public Animation Animation { get; set; }

    /// <summary>
    /// Gets or Sets the current frame index.
    /// </summary>
    public int CurrentFrame { get; set; }

    /// <summary>
    /// Gets or Sets the elapsed time since last frame change.
    /// </summary>
    public double ElapsedTime { get; set; }

    /// <summary>
    /// Gets or Sets whether the animation is currently playing.
    /// </summary>
    public bool IsPlaying { get; set; } = true;

    /// <summary>
    /// Gets or Sets whether the animation should loop.
    /// </summary>
    public bool Loop { get; set; } = true;

    /// <summary>
    /// Creates a new animated sprite.
    /// </summary>
    public AnimatedSprite() { }

    /// <summary>
    /// Creates a new animated sprite with the specified animation.
    /// </summary>
    /// <param name="animation">The animation for this animated sprite.</param>
    public AnimatedSprite(Animation animation)
    {
        Animation = animation;
        CurrentFrame = 0;
        ElapsedTime = 0;
    }

    /// <summary>
    /// Gets the current texture region based on the current frame.
    /// </summary>
    public TextureRegion CurrentRegion => Animation?.Frames[CurrentFrame];
}
