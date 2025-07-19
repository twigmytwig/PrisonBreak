using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using PrisonBreak.Core.Graphics;
using PrisonBreak.Core.Physics;
using PrisonBreak.Config;

namespace PrisonBreak.ECS;

public class ComponentEntityManager
{
    private readonly Dictionary<int, Entity> _entities = new();
    private readonly EventBus _eventBus;
    private int _nextEntityId = 1;
    private TextureAtlas _atlas;
    
    // Component indexing for fast queries
    private readonly Dictionary<Type, HashSet<int>> _componentIndex = new();
    
    public ComponentEntityManager(EventBus eventBus)
    {
        _eventBus = eventBus;
    }
    
    public void Initialize(ContentManager content)
    {
        _atlas = TextureAtlas.FromFile(content, EntityConfig.TextureAtlas.ConfigFile);
    }
    
    public Entity CreateEntity()
    {
        var entity = new Entity(_nextEntityId++);
        entity.SetComponentCallbacks(OnComponentAdded, OnComponentRemoved);
        _entities[entity.Id] = entity;
        return entity;
    }
    
    private void OnComponentAdded(int entityId, Type componentType)
    {
        if (!_componentIndex.ContainsKey(componentType))
            _componentIndex[componentType] = new HashSet<int>();
        _componentIndex[componentType].Add(entityId);
        
        // Validate component dependencies
        ValidateComponentDependencies(entityId, componentType);
    }
    
    private void ValidateComponentDependencies(int entityId, Type componentType)
    {
        var entity = _entities[entityId];
        
        // CollisionComponent requires TransformComponent
        if (componentType == typeof(CollisionComponent) && !entity.HasComponent<TransformComponent>())
        {
            Console.WriteLine($"Warning: Entity {entityId} has CollisionComponent but missing required TransformComponent");
        }
        
        // MovementComponent should have TransformComponent for position updates
        if (componentType == typeof(MovementComponent) && !entity.HasComponent<TransformComponent>())
        {
            Console.WriteLine($"Warning: Entity {entityId} has MovementComponent but missing TransformComponent");
        }
        
        // SpriteComponent should have TransformComponent for rendering
        if (componentType == typeof(SpriteComponent) && !entity.HasComponent<TransformComponent>())
        {
            Console.WriteLine($"Warning: Entity {entityId} has SpriteComponent but missing TransformComponent");
        }
    }
    
    private void OnComponentRemoved(int entityId, Type componentType)
    {
        if (_componentIndex.TryGetValue(componentType, out var entities))
            entities.Remove(entityId);
    }
    
    public void DestroyEntity(int entityId)
    {
        if (_entities.TryGetValue(entityId, out var entity))
        {
            // Get position for event
            Vector2 position = Vector2.Zero;
            if (entity.HasComponent<TransformComponent>())
            {
                position = entity.GetComponent<TransformComponent>().Position;
            }
            
            // Remove from component index
            foreach (var componentType in entity.GetComponentTypes())
            {
                if (_componentIndex.TryGetValue(componentType, out var entities))
                    entities.Remove(entityId);
            }
            
            _entities.Remove(entityId);
            _eventBus.Send(new EntityDestroyEvent(entityId, position, "Manual"));
        }
    }
    
    public Entity GetEntity(int entityId)
    {
        return _entities.TryGetValue(entityId, out var entity) ? entity : null;
    }
    
    public bool EntityExists(int entityId)
    {
        return _entities.ContainsKey(entityId);
    }
    
    // Fast O(1) query methods using component indexing
    public IEnumerable<Entity> GetEntitiesWith<T1>()
    {
        var type1 = typeof(T1);
        if (!_componentIndex.TryGetValue(type1, out var entities1))
            return Enumerable.Empty<Entity>();
            
        return entities1.Select(id => _entities[id]);
    }
    
    public IEnumerable<Entity> GetEntitiesWith<T1, T2>()
    {
        var type1 = typeof(T1);
        var type2 = typeof(T2);
        
        if (!_componentIndex.TryGetValue(type1, out var entities1) ||
            !_componentIndex.TryGetValue(type2, out var entities2))
            return Enumerable.Empty<Entity>();
            
        return entities1.Intersect(entities2).Select(id => _entities[id]);
    }
    
    public IEnumerable<Entity> GetEntitiesWith<T1, T2, T3>()
    {
        var type1 = typeof(T1);
        var type2 = typeof(T2);
        var type3 = typeof(T3);
        
        if (!_componentIndex.TryGetValue(type1, out var entities1) ||
            !_componentIndex.TryGetValue(type2, out var entities2) ||
            !_componentIndex.TryGetValue(type3, out var entities3))
            return Enumerable.Empty<Entity>();
            
        return entities1.Intersect(entities2).Intersect(entities3).Select(id => _entities[id]);
    }
    
    public IEnumerable<Entity> GetEntitiesWith<T1, T2, T3, T4>()
    {
        return _entities.Values.Where(e => e.HasComponent<T1>() && e.HasComponent<T2>() && e.HasComponent<T3>() && e.HasComponent<T4>());
    }
    
    public IEnumerable<Entity> GetAllEntities()
    {
        return _entities.Values;
    }
    
    public int GetEntityCount()
    {
        return _entities.Count;
    }
    
    // Factory methods for creating specific entity types
    public Entity CreatePlayer(Vector2 position, PlayerIndex playerIndex = PlayerIndex.One)
    {
        if (_atlas == null)
        {
            throw new InvalidOperationException("EntityManager must be initialized before creating entities");
        }
        
        var entity = CreateEntity();
        
        // Transform
        var transform = new TransformComponent(position, EntityConfig.Player.Scale);
        entity.AddComponent(transform);
        
        // Sprite
        var sprite = _atlas.CreateAnimatedSprite(EntityConfig.Player.AnimationName);
        entity.AddComponent(new SpriteComponent(sprite));
        
        // Animation
        entity.AddComponent(new AnimationComponent(sprite));
        
        // Movement
        entity.AddComponent(new MovementComponent(GameConfig.BaseMovementSpeed));
        
        // Collision - get dimensions from sprite's current region and apply scale
        float spriteWidth = 32f; // Default
        float spriteHeight = 32f; // Default
        if (sprite.CurrentRegion != null)
        {
            spriteWidth = sprite.CurrentRegion.Width;
            spriteHeight = sprite.CurrentRegion.Height;
        }
        
        // Apply transform scale to sprite dimensions
        float scaledWidth = spriteWidth * transform.Scale.X;
        float scaledHeight = spriteHeight * transform.Scale.Y;
        
        var colliderWidth = scaledWidth * GameConfig.ColliderWidthRatio;
        var colliderHeight = scaledHeight * GameConfig.ColliderHeightRatio;
        var collider = new RectangleCollider(
            (int)(position.X + (scaledWidth - colliderWidth) / 2),
            (int)(position.Y + (scaledHeight - colliderHeight) / 2),
            (int)colliderWidth,
            (int)colliderHeight
        );
        entity.AddComponent(new CollisionComponent(collider));
        
        // Player-specific components
        entity.AddComponent(new PlayerInputComponent(playerIndex));
        entity.AddComponent(new PlayerTag(entity.Id));
        
        // Debug
        if (EntityConfig.Player.DebugMode)
        {
            entity.AddComponent(new DebugComponent(true) { CollisionColor = GameConfig.PlayerColliderColor });
        }
        
        _eventBus.Send(new EntitySpawnEvent(entity.Id, position, "Player"));
        return entity;
    }
    
    public Entity CreateCop(Vector2 position, AIBehavior behavior = AIBehavior.Patrol)
    {
        if (_atlas == null)
        {
            throw new InvalidOperationException("EntityManager must be initialized before creating entities");
        }
        
        var entity = CreateEntity();
        
        // Transform
        var transform = new TransformComponent(position, EntityConfig.Cop.Scale);
        entity.AddComponent(transform);
        
        // Sprite
        var sprite = _atlas.CreateAnimatedSprite(EntityConfig.Cop.AnimationName);
        entity.AddComponent(new SpriteComponent(sprite));
        
        // Animation
        entity.AddComponent(new AnimationComponent(sprite));
        
        // Movement
        entity.AddComponent(new MovementComponent(EntityConfig.Cop.MovementSpeed));
        
        // Collision - get dimensions from sprite's current region and apply scale
        float spriteWidth = 32f; // Default
        float spriteHeight = 32f; // Default
        if (sprite.CurrentRegion != null)
        {
            spriteWidth = sprite.CurrentRegion.Width;
            spriteHeight = sprite.CurrentRegion.Height;
        }
        
        // Apply transform scale to sprite dimensions
        float scaledWidth = spriteWidth * transform.Scale.X;
        float scaledHeight = spriteHeight * transform.Scale.Y;
        
        var colliderWidth = scaledWidth * GameConfig.ColliderWidthRatio;
        var colliderHeight = scaledHeight * GameConfig.ColliderHeightRatio;
        var collider = new RectangleCollider(
            (int)(position.X + (scaledWidth - colliderWidth) / 2),
            (int)(position.Y + (scaledHeight - colliderHeight) / 2),
            (int)colliderWidth,
            (int)colliderHeight
        );
        entity.AddComponent(new CollisionComponent(collider));
        
        // AI
        entity.AddComponent(new AIComponent(behavior));
        
        // Cop-specific components
        entity.AddComponent(new CopTag(entity.Id));
        
        // Debug
        if (EntityConfig.Cop.DebugMode)
        {
            entity.AddComponent(new DebugComponent(true) { CollisionColor = GameConfig.CopColliderColor });
        }
        
        _eventBus.Send(new EntitySpawnEvent(entity.Id, position, "Cop"));
        return entity;
    }
    
    public void AddBoundsConstraint(Entity entity, Rectangle bounds, bool reflectVelocity = false)
    {
        var constraint = new BoundsConstraintComponent(bounds)
        {
            ReflectVelocityOnCollision = reflectVelocity
        };
        entity.AddComponent(constraint);
    }
    
    // Utility methods
    public Entity FindFirstPlayerEntity()
    {
        return GetEntitiesWith<PlayerTag>().FirstOrDefault();
    }
    
    public IEnumerable<Entity> FindAllCopEntities()
    {
        return GetEntitiesWith<CopTag>();
    }
    
    public void Clear()
    {
        _entities.Clear();
        _nextEntityId = 1;
    }
}