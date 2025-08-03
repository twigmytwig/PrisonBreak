using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PrisonBreak.Systems;
using PrisonBreak.Managers;
using PrisonBreak.Multiplayer.Core;

namespace PrisonBreak.ECS.Systems;

/// <summary>
/// System responsible for smoothly interpolating networked entity positions between network updates.
/// This system runs after network systems but before rendering to provide smooth 60fps movement
/// regardless of network update rate (20Hz for players, 10Hz for AI).
/// </summary>
public class NetworkInterpolationSystem : IGameSystem
{
    private ComponentEntityManager _entityManager;
    private NetworkManager _networkManager;

    public void SetEntityManager(ComponentEntityManager entityManager)
    {
        _entityManager = entityManager;
    }

    public void Initialize()
    {
        // Get the NetworkManager singleton
        try
        {
            _networkManager = NetworkManager.Instance;
            Console.WriteLine("[NetworkInterpolationSystem] Initialized with NetworkManager");
        }
        catch (InvalidOperationException)
        {
            _networkManager = null;
            Console.WriteLine("[NetworkInterpolationSystem] No NetworkManager found - interpolation disabled");
        }
    }

    public void Update(GameTime gameTime)
    {
        // Only interpolate in multiplayer mode
        if (_networkManager == null || _networkManager.CurrentGameMode == NetworkConfig.GameMode.SinglePlayer)
            return;

        InterpolateEntities(gameTime);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        // NetworkInterpolationSystem doesn't render anything
    }

    public void Shutdown()
    {
        // Clean up if needed
    }

    /// <summary>
    /// Interpolate positions for all entities that have both NetworkComponent and InterpolationComponent
    /// </summary>
    private void InterpolateEntities(GameTime gameTime)
    {
        // Get all entities that need interpolation
        var interpolatedEntities = _entityManager.GetEntitiesWith<NetworkComponent, InterpolationComponent, TransformComponent>()
            .Where(e => !IsLocalPlayer(e)) // Don't interpolate local player movement
            .ToList();

        foreach (var entity in interpolatedEntities)
        {
            var networkComp = entity.GetComponent<NetworkComponent>();
            ref var interpolationComp = ref entity.GetComponent<InterpolationComponent>();
            ref var transform = ref entity.GetComponent<TransformComponent>();

            // Skip if no valid target to interpolate to
            if (!interpolationComp.HasValidTarget)
                continue;

            // Calculate interpolation progress (0.0 to 1.0)
            double elapsedTime = gameTime.TotalGameTime.TotalSeconds - interpolationComp.InterpolationStartTime;
            float progress = (float)(elapsedTime / interpolationComp.NetworkUpdateInterval);

            // Clamp progress to [0, 1] and handle overshooting
            if (progress >= 1.0f)
            {
                // Interpolation complete - snap to target
                transform.Position = interpolationComp.TargetPosition;
                transform.Rotation = interpolationComp.TargetRotation;
                interpolationComp.HasValidTarget = false;
            }
            else
            {
                // Smooth interpolation using lerp
                transform.Position = Vector2.Lerp(
                    interpolationComp.PreviousPosition,
                    interpolationComp.TargetPosition,
                    SmoothStep(progress)
                );

                transform.Rotation = MathHelper.Lerp(
                    interpolationComp.PreviousRotation,
                    interpolationComp.TargetRotation,
                    SmoothStep(progress)
                );
            }
        }
    }

    /// <summary>
    /// Check if entity is the local player (should not be interpolated)
    /// </summary>
    private bool IsLocalPlayer(Entity entity)
    {
        if (!entity.HasComponent<NetworkComponent>())
            return false;

        var networkComp = entity.GetComponent<NetworkComponent>();
        
        // Local player has client authority and matches local player ID
        return networkComp.Authority == NetworkConfig.NetworkAuthority.Client && 
               networkComp.OwnerId == _networkManager.GetLocalPlayerId();
    }

    /// <summary>
    /// Smooth step function for more natural-feeling interpolation
    /// Provides smooth acceleration and deceleration
    /// </summary>
    private float SmoothStep(float t)
    {
        // Clamp to [0, 1]
        t = MathHelper.Clamp(t, 0f, 1f);
        
        // Smooth step: 3t² - 2t³
        return t * t * (3f - 2f * t);
    }

    /// <summary>
    /// Set new interpolation target for an entity
    /// Called by network systems when receiving position updates
    /// </summary>
    public void SetInterpolationTarget(Entity entity, Vector2 newPosition, float newRotation, GameTime gameTime)
    {
        if (!entity.HasComponent<InterpolationComponent>())
            return;

        ref var interpolationComp = ref entity.GetComponent<InterpolationComponent>();
        ref var transform = ref entity.GetComponent<TransformComponent>();

        // Store current position as previous position
        interpolationComp.PreviousPosition = transform.Position;
        interpolationComp.PreviousRotation = transform.Rotation;

        // Set new target
        interpolationComp.TargetPosition = newPosition;
        interpolationComp.TargetRotation = newRotation;

        // Reset interpolation timing
        interpolationComp.InterpolationStartTime = gameTime.TotalGameTime.TotalSeconds;
        interpolationComp.HasValidTarget = true;
    }
}