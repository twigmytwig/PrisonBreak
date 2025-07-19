using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PrisonBreak.Systems;

namespace PrisonBreak.ECS;

/// <summary>
/// System responsible for updating animations on entities with AnimationComponent.
/// </summary>
public class AnimationSystem : IGameSystem
{
    private readonly ComponentEntityManager _entityManager;

    public AnimationSystem(ComponentEntityManager entityManager)
    {
        _entityManager = entityManager;
    }

    public void Initialize()
    {
        // No initialization needed for animation system
    }

    public void Update(GameTime gameTime)
    {
        // Get all entities that have animation components
        var animatedEntities = _entityManager.GetEntitiesWith<AnimationComponent, SpriteComponent>();

        foreach (var entity in animatedEntities)
        {
            ref var animationComponent = ref entity.GetComponent<AnimationComponent>();
            ref var spriteComponent = ref entity.GetComponent<SpriteComponent>();

            // Skip if animation is not playing
            if (!animationComponent.IsPlaying)
                continue;

            var animatedSprite = animationComponent.AnimatedSprite;
            
            // Skip if no animation is set
            if (animatedSprite?.Animation == null)
                continue;

            // Update elapsed time
            animationComponent.ElapsedTime += gameTime.ElapsedGameTime.TotalMilliseconds;

            // Check if it's time to advance to the next frame
            if (animationComponent.ElapsedTime >= animatedSprite.Animation.Delay.TotalMilliseconds)
            {
                // Reset elapsed time
                animationComponent.ElapsedTime -= animatedSprite.Animation.Delay.TotalMilliseconds;
                
                // Advance to next frame
                animatedSprite.CurrentFrame++;

                // Check if we've reached the end of the animation
                if (animatedSprite.CurrentFrame >= animatedSprite.Animation.Frames.Count)
                {
                    if (animationComponent.Loop)
                    {
                        // Loop back to the beginning
                        animatedSprite.CurrentFrame = 0;
                    }
                    else
                    {
                        // Stop at the last frame
                        animatedSprite.CurrentFrame = animatedSprite.Animation.Frames.Count - 1;
                        animationComponent.IsPlaying = false;
                    }
                }

                // Update the sprite component with the new frame
                // The animatedSprite is already updated with the new frame
                // No additional updates needed since we're modifying the same object
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        // Animation system doesn't need to draw anything
        // Rendering is handled by ComponentRenderSystem
    }

    public void Shutdown()
    {
        // No cleanup needed for animation system
    }
}