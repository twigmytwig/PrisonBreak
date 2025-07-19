using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using PrisonBreak.Config;
using PrisonBreak.Systems;

namespace PrisonBreak.ECS;

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
        renderableEntities.Sort((a, b) => 
        {
            var transformA = a.GetComponent<TransformComponent>();
            var transformB = b.GetComponent<TransformComponent>();
            return transformA.Position.Y.CompareTo(transformB.Position.Y);
        });
        
        // Draw all sprites
        foreach (var entity in renderableEntities)
        {
            var transform = entity.GetComponent<TransformComponent>();
            var sprite = entity.GetComponent<SpriteComponent>();
            
            DrawSprite(spriteBatch, sprite, transform);
        }
    }
    
    private void DrawSprite(SpriteBatch spriteBatch, SpriteComponent sprite, TransformComponent transform)
    {
        if (sprite.Sprite == null || sprite.Sprite.Animation == null) return;
        
        // Get the current texture region from the animated sprite
        var currentRegion = sprite.Sprite.CurrentRegion;
        if (currentRegion == null) return;
        
        // Draw the current frame directly using the texture region
        currentRegion.Draw(spriteBatch, transform.Position, sprite.Tint, transform.Rotation, 
            Vector2.Zero, transform.Scale, SpriteEffects.None, 0f);
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
        if (Core.GraphicsDevice != null && !_debugTexturesCreated)
        {
            _debugTexture = new Texture2D(Core.GraphicsDevice, 1, 1);
            _debugTexture.SetData(new[] { Color.White });
            _debugTexturesCreated = true;
        }
    }
}