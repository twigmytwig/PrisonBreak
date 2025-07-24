using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PrisonBreak.Systems;

namespace PrisonBreak.ECS.Systems;

public class InteractionSystem : IGameSystem
{
    private ComponentEntityManager _entityManager;
    private EventBus _eventBus;
    private InventorySystem _inventorySystem;

    public void SetEntityManager(ComponentEntityManager entityManager)
    {
        _entityManager = entityManager;
    }

    public void SetEventBus(EventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public void SetInventorySystem(InventorySystem inventorySystem)
    {
        _inventorySystem = inventorySystem;
    }

    public void Initialize()
    {
        // Subscribe to interaction input events
        _eventBus?.Subscribe<InteractionInputEvent>(OnInteractionInput);
    }

    public void Update(GameTime gameTime)
    {
        // No frame-by-frame logic needed - all interaction is event-driven
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        // Interaction system doesn't render anything
    }

    public void Shutdown()
    {
        // Unsubscribe from events
        _eventBus?.Unsubscribe<InteractionInputEvent>(OnInteractionInput);
    }

    private void OnInteractionInput(InteractionInputEvent evt)
    {
        var playerEntity = _entityManager.GetEntity(evt.EntityId);
        if (playerEntity == null || !playerEntity.HasComponent<TransformComponent>())
            return;

        var playerTransform = playerEntity.GetComponent<TransformComponent>();
        
        // Calculate player's visual center position (players are also scaled)
        Vector2 playerCenter = GetSpriteCenterPosition(playerEntity, playerTransform);

        // Find nearby interactables using player's center position
        var nearbyInteractable = FindNearestInteractable(playerCenter);
        if (nearbyInteractable != null)
        {
            ProcessInteraction(playerEntity, nearbyInteractable);
        }
    }

    private Entity FindNearestInteractable(Vector2 playerPosition)
    {
        var interactables = _entityManager.GetEntitiesWith<InteractableComponent, TransformComponent>().ToList();
        
        Entity closestInteractable = null;
        float closestDistance = float.MaxValue;

        foreach (var interactable in interactables)
        {
            var interactableComponent = interactable.GetComponent<InteractableComponent>();
            var interactableTransform = interactable.GetComponent<TransformComponent>();
            
            // Skip inactive interactables
            if (!interactableComponent.IsActive)
                continue;

            // Calculate the visual center of the scaled sprite for accurate interaction detection
            Vector2 spriteCenter = GetSpriteCenterPosition(interactable, interactableTransform);
            float distance = Vector2.Distance(playerPosition, spriteCenter);

            if (distance <= interactableComponent.InteractionRange && distance < closestDistance)
            {
                closestDistance = distance;
                closestInteractable = interactable;
            }
        }

        return closestInteractable;
    }

    private void ProcessInteraction(Entity playerEntity, Entity interactableEntity)
    {
        var interactableComponent = interactableEntity.GetComponent<InteractableComponent>();
        
        // Get player ID for events
        int playerId = -1;
        if (playerEntity.HasComponent<PlayerTag>())
        {
            playerId = playerEntity.GetComponent<PlayerTag>().PlayerId;
        }

        // Send interaction event
        _eventBus?.Send(new InteractionEvent(playerId, interactableEntity, interactableComponent.InteractionType));

        // Handle different interaction types
        switch (interactableComponent.InteractionType.ToLower())
        {
            case "pickup":
                HandleItemPickup(playerEntity, interactableEntity);
                break;
            case "chest":
                HandleChestInteraction(playerEntity, interactableEntity);
                break;
            case "door":
                HandleDoorInteraction(playerEntity, interactableEntity);
                break;
        }
    }

    private void HandleItemPickup(Entity playerEntity, Entity itemEntity)
    {
        if (_inventorySystem == null || !itemEntity.HasComponent<ItemComponent>())
            return;

        var itemComponent = itemEntity.GetComponent<ItemComponent>();
        
        // Create a new item entity specifically for inventory (without world components)
        var inventoryItemEntity = _entityManager.CreateEntity();
        inventoryItemEntity.AddComponent(itemComponent); // Copy the item data
        
        // Copy the sprite component but ensure it's visible for inventory
        if (itemEntity.HasComponent<SpriteComponent>())
        {
            var originalSprite = itemEntity.GetComponent<SpriteComponent>();
            var inventorySprite = new SpriteComponent(originalSprite.Sprite)
            {
                Visible = true, // Always visible in inventory
                Tint = originalSprite.Tint
            };
            inventoryItemEntity.AddComponent(inventorySprite);
        }

        // Try to add the new inventory item to player's inventory
        bool success = _inventorySystem.TryAddItem(playerEntity, inventoryItemEntity);
        
        if (success)
        {
            // Completely remove the world item entity
            if (itemEntity.HasComponent<SpriteComponent>())
            {
                ref var sprite = ref itemEntity.GetComponent<SpriteComponent>();
                sprite.Visible = false;
            }
            
            if (itemEntity.HasComponent<InteractableComponent>())
            {
                ref var interactable = ref itemEntity.GetComponent<InteractableComponent>();
                interactable.IsActive = false;
            }
            
            // TODO: Actually destroy the world entity (remove from entity manager)
            // For now, making it invisible and inactive is sufficient
        }
        else
        {
            // If inventory is full, clean up the temporary inventory entity
            // TODO: Properly destroy the temporary entity
        }
    }

    private void HandleChestInteraction(Entity playerEntity, Entity chestEntity)
    {
        // Send chest UI open event
        _eventBus?.Send(new ChestUIOpenEvent(chestEntity, playerEntity));
    }

    private void HandleDoorInteraction(Entity playerEntity, Entity doorEntity)
    {
        // TODO: Implement door opening/closing logic
        // This might involve changing sprites, collision, etc.
    }

    /// <summary>
    /// Calculates the visual center position of a scaled sprite
    /// </summary>
    private Vector2 GetSpriteCenterPosition(Entity entity, TransformComponent transform)
    {
        // Try to get actual sprite dimensions, otherwise use defaults
        float baseSpriteWidth = 32f;  // Default for main atlas
        float baseSpriteHeight = 32f;
        
        // Check if this is likely a UI sprite by examining the sprite component
        if (entity.HasComponent<SpriteComponent>())
        {
            var spriteComponent = entity.GetComponent<SpriteComponent>();
            if (spriteComponent.Sprite?.CurrentRegion != null)
            {
                // Get actual sprite dimensions from the texture region
                var region = spriteComponent.Sprite.CurrentRegion;
                baseSpriteWidth = region.Width;
                baseSpriteHeight = region.Height;
            }
        }
        
        // If we can't get dimensions, use reasonable defaults based on common sizes
        // Items are typically from main atlas (32x32), UI elements from UI atlas (16x16)
        if (baseSpriteWidth == 0 || baseSpriteHeight == 0)
        {
            // Default to main atlas size for most interactable items
            baseSpriteWidth = 32f;
            baseSpriteHeight = 32f;
        }
        
        // Calculate the visual size after scaling
        float scaledWidth = baseSpriteWidth * transform.Scale.X;
        float scaledHeight = baseSpriteHeight * transform.Scale.Y;
        
        // Calculate the center offset from the top-left position
        Vector2 centerOffset = new Vector2(scaledWidth / 2, scaledHeight / 2);
        
        // Return the center position
        return transform.Position + centerOffset;
    }
}