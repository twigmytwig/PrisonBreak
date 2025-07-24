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
    private TextureAtlas _uiAtlas;

    // Component indexing for fast queries
    private readonly Dictionary<Type, HashSet<int>> _componentIndex = new();

    public ComponentEntityManager(EventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public void Initialize(ContentManager content)
    {
        _atlas = TextureAtlas.FromFile(content, EntityConfig.TextureAtlas.ConfigFile);
        _uiAtlas = TextureAtlas.FromFile(content, EntityConfig.UIAtlas.ConfigFile);
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
    public Entity CreatePlayer(Vector2 position, PlayerIndex playerIndex = PlayerIndex.One, PlayerType playerType = PlayerType.Prisoner)
    {
        if (_atlas == null)
        {
            throw new InvalidOperationException("EntityManager must be initialized before creating entities");
        }

        var entity = CreateEntity();

        // Transform
        var transform = new TransformComponent(position, EntityConfig.Player.Scale);
        entity.AddComponent(transform);

        // Player-specific components (add PlayerTypeComponent first to get animation name)
        var playerTypeComponent = new PlayerTypeComponent(playerType);
        entity.AddComponent(playerTypeComponent);
        entity.AddComponent(new PlayerInputComponent(playerIndex));
        entity.AddComponent(new PlayerTag(entity.Id));

        // Sprite - use animation from player type
        var sprite = _atlas.CreateAnimatedSprite(playerTypeComponent.AnimationName);
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

        // Inventory - initialize based on player type
        var inventory = new InventoryComponent(playerTypeComponent.InventorySlots);
        entity.AddComponent(inventory);

        // Note: Starting items for cop players will be added after entity creation
        // to ensure proper event handling and UI updates

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
        var copPlayerTypeComponent = new PlayerTypeComponent(PlayerType.Cop);
        entity.AddComponent(copPlayerTypeComponent);

        // Add inventory to cop (like players have)
        var inventory = new InventoryComponent(copPlayerTypeComponent.InventorySlots);
        entity.AddComponent(inventory);

        // Note: AI cops get starting key items added after creation via InventorySystem
        // to ensure proper event handling (even though they don't have UI)

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

    /// <summary>
    /// Creates inventory UI slot entities for a player
    /// </summary>
    public void CreateInventoryUIForPlayer(Entity playerEntity, bool isLocalPlayer = true, int screenHeight = 600)
    {
        if (!playerEntity.HasComponent<PlayerTypeComponent>() || !playerEntity.HasComponent<PlayerTag>())
        {
            Console.WriteLine("Warning: Cannot create inventory UI - player missing required components");
            return;
        }

        var playerType = playerEntity.GetComponent<PlayerTypeComponent>();
        var playerId = playerEntity.GetComponent<PlayerTag>().PlayerId;

        // Position slots at bottom of screen for local player, top for remote
        var baseY = isLocalPlayer ? screenHeight - 80 : 30; // Bottom of screen with some margin
        var baseX = isLocalPlayer ? 100 : 50; // Start from left side

        // Create a slot UI entity for each inventory slot
        for (int i = 0; i < playerType.InventorySlots; i++)
        {
            var slotEntity = CreateEntity();

            // Position slots horizontally, spaced 50px apart (much more spacing)
            var slotPosition = new Vector2(baseX + i * 100, baseY); // TODO: adjust based on screen size
            slotEntity.AddComponent(new TransformComponent(slotPosition, new Vector2(4.0f, 4.0f))); // Make slots 2x larger

            // Console.WriteLine($"Created inventory slot {i} at position {slotPosition}");

            // Use inventory slot sprite from UI atlas - fallback to main atlas if needed
            AnimatedSprite inventorySlotSprite;
            try
            {
                // Try to get the inventory slot sprite from UI atlas
                inventorySlotSprite = _uiAtlas.CreateAnimatedSprite("inventory-slot");
            }
            catch (Exception ex)
            {
                // Fallback: create a simple sprite using the main atlas
                Console.WriteLine($"Warning: 'inventory-slot' sprite not found in UI atlas: {ex.Message}");
                try
                {
                    inventorySlotSprite = _atlas.CreateAnimatedSprite("prisoner-animation"); // Temporary fallback
                    Console.WriteLine("Using prisoner sprite as fallback");
                }
                catch
                {
                    Console.WriteLine("Error: Could not load fallback sprite");
                    throw;
                }
            }

            slotEntity.AddComponent(new SpriteComponent(inventorySlotSprite));
            slotEntity.AddComponent(new InventorySlotUIComponent(playerId, i, isLocalPlayer));
        }

        Console.WriteLine($"Created inventory UI for Player {playerId} with {playerType.InventorySlots} slots");
    }

    /// <summary>
    /// Creates an item entity from the item database
    /// </summary>
    public Entity CreateItem(string itemId)
    {
        if (_uiAtlas == null)
        {
            throw new InvalidOperationException("EntityManager must be initialized before creating items");
        }

        var itemDefinition = ItemDatabase.GetItem(itemId);
        if (itemDefinition == null)
        {
            throw new ArgumentException($"Item '{itemId}' not found in ItemDatabase");
        }

        var item = itemDefinition.Value;
        var entity = CreateEntity();

        // Add ItemComponent with data from database
        entity.AddComponent(new ItemComponent
        {
            ItemName = item.ItemName,
            ItemType = item.ItemType,
            IsStackable = item.IsStackable,
            StackSize = item.StackSize
        });

        // Add sprite from UI atlas
        try
        {
            var sprite = _uiAtlas.CreateAnimatedSprite(item.AtlasRegionName);
            entity.AddComponent(new SpriteComponent(sprite));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not load sprite '{item.AtlasRegionName}' for item '{itemId}': {ex.Message}");
            throw;
        }

        // Items don't need transform by default (they're stored in inventory)
        // Transform will be added when item is dropped/placed in world

        Console.WriteLine($"Created item: {item.ItemName} (ID: {itemId})");
        return entity;
    }

    /// <summary>
    /// Creates a key item entity
    /// </summary>
    public Entity CreateKey()
    {
        return CreateItem("key");
    }

    /// <summary>
    /// Creates an item entity at a specific world position for pickup
    /// </summary>
    public Entity CreateItemAtPosition(string itemId, Vector2 position)
    {
        var itemEntity = CreateItem(itemId);

        // Add transform component for world placement (make items 2x larger)
        itemEntity.AddComponent(new TransformComponent(position, new Vector2(2.0f, 2.0f)));

        // Make item interactable for pickup (increase interaction range)
        var itemComponent = itemEntity.GetComponent<ItemComponent>();
        itemEntity.AddComponent(new InteractableComponent(
            "pickup",
            64f, // Increased from 48f
            $"Press E to pick up {itemComponent.ItemName}"
        ));

        return itemEntity;
    }

    /// <summary>
    /// Creates a chest entity with optional starting items
    /// </summary>
    public Entity CreateChest(Vector2 position, string[] itemIds = null)
    {
        if (_atlas == null)
        {
            throw new InvalidOperationException("EntityManager must be initialized before creating entities");
        }

        var entity = CreateEntity();

        // Transform
        entity.AddComponent(new TransformComponent(position, new Vector2(5f, 5f)));

        // Sprite - try to get chest sprite from UI atlas first, then main atlas
        AnimatedSprite chestSprite;
        try
        {
            chestSprite = _uiAtlas.CreateAnimatedSprite("chest"); // Chest sprite is in UI atlas
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: 'chest' sprite not found in UI atlas: {ex.Message}. Trying main atlas.");
            try
            {
                chestSprite = _atlas.CreateAnimatedSprite("chest"); // Try main atlas as fallback
            }
            catch (Exception ex2)
            {
                Console.WriteLine($"Warning: 'chest' sprite not found in main atlas either: {ex2.Message}. Using fallback.");
                chestSprite = _atlas.CreateAnimatedSprite("prisoner-animation"); // Final fallback
            }
        }
        entity.AddComponent(new SpriteComponent(chestSprite));

        // Make chest interactable
        entity.AddComponent(new InteractableComponent(
            "chest",
            64f,
            "Press E to open chest"
        ));

        // Add container component
        var container = new ContainerComponent(10, "chest"); // 10 slot chest
        entity.AddComponent(container);

        // Populate with initial items if provided
        if (itemIds != null)
        {
            for (int i = 0; i < Math.Min(itemIds.Length, container.MaxItems); i++)
            {
                try
                {
                    var item = CreateItem(itemIds[i]);
                    container.ContainedItems[i] = item;
                    container.ItemCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not add item '{itemIds[i]}' to chest: {ex.Message}");
                }
            }
        }

        // Add collision for chest (optional - chests can be walked through or solid)
        var collider = new RectangleCollider(
            (int)position.X,
            (int)position.Y,
            32, 32); // Standard chest size
        entity.AddComponent(new CollisionComponent(collider) { IsSolid = true });

        _eventBus.Send(new EntitySpawnEvent(entity.Id, position, "Chest"));
        return entity;
    }
}