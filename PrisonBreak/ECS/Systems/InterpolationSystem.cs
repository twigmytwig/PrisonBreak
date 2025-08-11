using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PrisonBreak.ECS;
using PrisonBreak.Systems;

namespace PrisonBreak.ECS.Systems;

/// <summary>
/// System responsible for smoothly interpolating networked entity positions
/// between network updates to eliminate stuttering movement
/// </summary>
public class InterpolationSystem : IGameSystem
{
    private ComponentEntityManager _entityManager;
    private EventBus _eventBus;
    
    public void SetEntityManager(ComponentEntityManager entityManager)
    {
        _entityManager = entityManager;
    }
    
    public void SetEventBus(EventBus eventBus)
    {
        _eventBus = eventBus;
    }
    
    public void Initialize()
    {
        Console.WriteLine("[InterpolationSystem] Initialized - ready to smooth network movement");
    }
    
    public void Update(GameTime gameTime)
    {
        // Find all entities that are currently interpolating
        var interpolatingEntities = _entityManager.GetEntitiesWith<InterpolationComponent, RenderTransformComponent>()
            .Where(e => e.GetComponent<InterpolationComponent>().IsInterpolating)
            .ToList();

        foreach (var entity in interpolatingEntities)
        {
            UpdateEntityInterpolation(entity, gameTime);
        }
    }
    
    public void Draw(SpriteBatch spriteBatch)
    {
        // InterpolationSystem doesn't render anything itself
    }
    
    public void Shutdown()
    {
        Console.WriteLine("[InterpolationSystem] Shutdown");
    }
    
    /// <summary>
    /// Update interpolation progress for a single entity
    /// </summary>
    private void UpdateEntityInterpolation(Entity entity, GameTime gameTime)
    {
        var interpolation = entity.GetComponent<InterpolationComponent>();
        var renderTransform = entity.GetComponent<RenderTransformComponent>();
        
        // Calculate time elapsed since interpolation started
        var currentTime = gameTime.TotalGameTime.TotalSeconds;
        var elapsed = (float)(currentTime - interpolation.LastUpdateTime);
        
        // Calculate interpolation progress (0.0 to 1.0)
        var progress = Math.Min(elapsed / interpolation.Duration, 1.0f);
        
        // Apply smooth interpolation using smoothstep for natural feel
        var smoothProgress = SmoothStep(progress);
        
        // Interpolate position
        var interpolatedPosition = Vector2.Lerp(interpolation.FromPosition, interpolation.ToPosition, smoothProgress);
        
        // Update render transform with interpolated position
        renderTransform.RenderPosition = interpolatedPosition;
        entity.AddComponent(renderTransform);
        
        // Update interpolation progress
        interpolation.Progress = progress;
        
        // Check if interpolation is complete
        if (progress >= 1.0f)
        {
            interpolation.IsInterpolating = false;
            // Ensure we end exactly at the target position
            renderTransform.RenderPosition = interpolation.ToPosition;
            entity.AddComponent(renderTransform);
        }
        
        entity.AddComponent(interpolation);
    }
    
    /// <summary>
    /// Smooth step interpolation for natural movement feel
    /// Better than linear interpolation - starts slow, speeds up, then slows down
    /// </summary>
    private float SmoothStep(float t)
    {
        // Hermite interpolation: 3t² - 2t³
        return t * t * (3f - 2f * t);
    }
    
    /// <summary>
    /// Start interpolation for an entity from current position to target
    /// Called by NetworkManager when network updates arrive
    /// </summary>
    public static void StartInterpolation(Entity entity, Vector2 targetPosition)
    {
        if (!entity.HasComponent<RenderTransformComponent>())
        {
            // Add RenderTransformComponent if it doesn't exist
            var transform = entity.GetComponent<TransformComponent>();
            entity.AddComponent(RenderTransformComponent.FromTransform(transform));
        }
        
        var renderTransform = entity.GetComponent<RenderTransformComponent>();
        var currentTime = DateTime.Now.TimeOfDay.TotalSeconds; // Use system time instead of GameTime
        
        // Calculate interpolation duration based on distance
        var distance = Vector2.Distance(renderTransform.RenderPosition, targetPosition);
        var baseDuration = 0.05f; // 50ms base duration (20Hz network updates)
        var duration = Math.Max(baseDuration, distance / 1000f); // Adjust based on distance
        
        // Create or update interpolation component
        var interpolation = InterpolationComponent.StartInterpolation(
            renderTransform.RenderPosition, 
            targetPosition, 
            currentTime, 
            duration
        );
        
        entity.AddComponent(interpolation);
    }
}