using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PrisonBreak.Core.Graphics;
using PrisonBreak.Config;
using PrisonBreak.Systems;

namespace PrisonBreak.ECS.Systems;

public class ComponentRenderSystem : IGameSystem
{
    private ComponentEntityManager _entityManager;
    private EventBus _eventBus;
    private Tilemap _tilemap;
    private Texture2D _debugTexture;
    private bool _debugTexturesCreated;
    
    public void SetEntityManager(ComponentEntityManager entityManager)
    {
        _entityManager = entityManager;
    }
    
    public void SetEventBus(EventBus eventBus)
    {
        _eventBus = eventBus;
    }
    
    public void SetTilemap(Tilemap tilemap)
    {
        _tilemap = tilemap;
    }
    
    public void Initialize()
    {
        CreateDebugTextures();
    }
    
    public void Update(GameTime gameTime)
    {
        // ComponentRenderSystem doesn't need to update anything
        // Animation updates are handled by AnimationSystem
    }
    
    public void Draw(SpriteBatch spriteBatch)
    {
        if (_entityManager == null) return;
        
        // Draw tilemap first (background)
        DrawTilemap(spriteBatch);
        
        // Draw all entities with sprites
        DrawEntities(spriteBatch);
        
        // Draw debug information last (overlay)
        DrawDebugInformation(spriteBatch);
    }
    
    public void Shutdown()
    {
        _debugTexture?.Dispose();
    }
    
    private void DrawTilemap(SpriteBatch spriteBatch)
    {
        if (_tilemap != null)
        {
            _tilemap.Draw(spriteBatch);
        }
    }
    
    private void DrawEntities(SpriteBatch spriteBatch)
    {
        // Get all renderable entities (have both transform and sprite)
        var renderableEntities = _entityManager.GetEntitiesWith<TransformComponent, SpriteComponent>()
            .Where(e => e.GetComponent<SpriteComponent>().Visible)
            .ToList();
        
        // Sort entities by Y position for proper depth ordering (back to front)
        // Use render position if available, otherwise use authoritative position
        renderableEntities.Sort((a, b) => 
        {
            var posA = GetRenderPosition(a);
            var posB = GetRenderPosition(b);
            return posA.Y.CompareTo(posB.Y);
        });
        
        // Draw all sprites
        foreach (var entity in renderableEntities)
        {
            var transform = entity.GetComponent<TransformComponent>();
            var sprite = entity.GetComponent<SpriteComponent>();
            
            DrawSprite(spriteBatch, sprite, transform, entity);
        }
    }
    
    private void DrawSprite(SpriteBatch spriteBatch, SpriteComponent sprite, TransformComponent transform, Entity entity)
    {
        if (sprite.Sprite == null || sprite.Sprite.Animation == null) return;
        
        // Get the current texture region from the animated sprite
        var currentRegion = sprite.Sprite.CurrentRegion;
        if (currentRegion == null) return;
        
        // Use render transform if available (for interpolated networked entities),
        // otherwise fall back to authoritative transform
        var renderPosition = GetRenderPosition(entity);
        var renderRotation = GetRenderRotation(entity, transform);
        var renderScale = GetRenderScale(entity, transform);
        
        // Draw the current frame directly using the texture region
        currentRegion.Draw(spriteBatch, renderPosition, sprite.Tint, renderRotation, 
            Vector2.Zero, renderScale, SpriteEffects.None, 0f);
    }
    
    /// <summary>
    /// Get the position to use for rendering - interpolated if available, otherwise authoritative
    /// </summary>
    private Vector2 GetRenderPosition(Entity entity)
    {
        if (entity.HasComponent<RenderTransformComponent>())
        {
            return entity.GetComponent<RenderTransformComponent>().RenderPosition;
        }
        return entity.GetComponent<TransformComponent>().Position;
    }
    
    /// <summary>
    /// Get the rotation to use for rendering - interpolated if available, otherwise authoritative
    /// </summary>
    private float GetRenderRotation(Entity entity, TransformComponent fallback)
    {
        if (entity.HasComponent<RenderTransformComponent>())
        {
            return entity.GetComponent<RenderTransformComponent>().RenderRotation;
        }
        return fallback.Rotation;
    }
    
    /// <summary>
    /// Get the scale to use for rendering - interpolated if available, otherwise authoritative
    /// </summary>
    private Vector2 GetRenderScale(Entity entity, TransformComponent fallback)
    {
        if (entity.HasComponent<RenderTransformComponent>())
        {
            return entity.GetComponent<RenderTransformComponent>().RenderScale;
        }
        return fallback.Scale;
    }
    
    private void DrawDebugInformation(SpriteBatch spriteBatch)
    {
        if (!_debugTexturesCreated) return;
        
        // Draw debug info for entities that have debug components
        var debugEntities = _entityManager.GetEntitiesWith<DebugComponent, CollisionComponent>();
        
        foreach (var entity in debugEntities)
        {
            var debug = entity.GetComponent<DebugComponent>();
            var collision = entity.GetComponent<CollisionComponent>();
            
            if (debug.ShowCollisionBounds)
            {
                DrawCollisionBounds(spriteBatch, collision, debug);
            }
        }
        
        // Note: Legacy debug mode removed - use DebugComponent instead
    }
    
    private void DrawCollisionBounds(SpriteBatch spriteBatch, CollisionComponent collision, DebugComponent debug)
    {
        DrawCollisionBounds(spriteBatch, collision, debug.CollisionColor, debug.CollisionThickness);
    }
    
    private void DrawCollisionBounds(SpriteBatch spriteBatch, CollisionComponent collision, Color color, int thickness)
    {
        if (_debugTexture == null) return;
        
        var bounds = collision.Collider.rectangleCollider;
        
        // Draw collision rectangle outline
        // Top edge
        spriteBatch.Draw(_debugTexture, new Rectangle(bounds.X, bounds.Y, bounds.Width, thickness), color);
        // Bottom edge
        spriteBatch.Draw(_debugTexture, new Rectangle(bounds.X, bounds.Bottom - thickness, bounds.Width, thickness), color);
        // Left edge
        spriteBatch.Draw(_debugTexture, new Rectangle(bounds.X, bounds.Y, thickness, bounds.Height), color);
        // Right edge
        spriteBatch.Draw(_debugTexture, new Rectangle(bounds.Right - thickness, bounds.Y, thickness, bounds.Height), color);
    }
    
    private void CreateDebugTextures()
    {
        if (PrisonBreak.Core.Core.GraphicsDevice != null && !_debugTexturesCreated)
        {
            _debugTexture = new Texture2D(PrisonBreak.Core.Core.GraphicsDevice, 1, 1);
            _debugTexture.SetData(new[] { Color.White });
            _debugTexturesCreated = true;
        }
    }
}