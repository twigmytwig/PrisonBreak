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
        Console.WriteLine($"[DEBUG] Interaction input received for entity {evt.EntityId}");
        
        var playerEntity = _entityManager.GetEntity(evt.EntityId);
        if (playerEntity == null || !playerEntity.HasComponent<TransformComponent>())
        {
            Console.WriteLine("[DEBUG] Player entity not found or missing transform");
            return;
        }

        var playerTransform = playerEntity.GetComponent<TransformComponent>();
        Console.WriteLine($"[DEBUG] Player position: {playerTransform.Position}");

        // Find nearby interactables
        var nearbyInteractable = FindNearestInteractable(playerTransform.Position);
        if (nearbyInteractable != null)
        {
            Console.WriteLine($"[DEBUG] Found nearby interactable: {nearbyInteractable.Id}");
            ProcessInteraction(playerEntity, nearbyInteractable);
        }
        else
        {
            Console.WriteLine("[DEBUG] No nearby interactables found");
        }
    }

    private Entity FindNearestInteractable(Vector2 playerPosition)
    {
        var interactables = _entityManager.GetEntitiesWith<InteractableComponent, TransformComponent>().ToList();
        Console.WriteLine($"[DEBUG] Found {interactables.Count} total interactable entities");
        
        Entity closestInteractable = null;
        float closestDistance = float.MaxValue;

        foreach (var interactable in interactables)
        {
            var interactableComponent = interactable.GetComponent<InteractableComponent>();
            var interactableTransform = interactable.GetComponent<TransformComponent>();
            float distance = Vector2.Distance(playerPosition, interactableTransform.Position);
            
            Console.WriteLine($"[DEBUG] Interactable {interactable.Id} at {interactableTransform.Position}, distance: {distance:F2}, range: {interactableComponent.InteractionRange}, active: {interactableComponent.IsActive}");
            
            // Skip inactive interactables
            if (!interactableComponent.IsActive)
            {
                Console.WriteLine($"[DEBUG] Skipping inactive interactable {interactable.Id}");
                continue;
            }

            if (distance <= interactableComponent.InteractionRange && distance < closestDistance)
            {
                closestDistance = distance;
                closestInteractable = interactable;
                Console.WriteLine($"[DEBUG] New closest interactable: {interactable.Id} at distance {distance:F2}");
            }
        }

        if (closestInteractable == null)
        {
            Console.WriteLine("[DEBUG] No interactable within range found");
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
            default:
                Console.WriteLine($"Unknown interaction type: {interactableComponent.InteractionType}");
                break;
        }
    }

    private void HandleItemPickup(Entity playerEntity, Entity itemEntity)
    {
        if (_inventorySystem == null)
        {
            Console.WriteLine("Cannot pickup item - InventorySystem not set");
            return;
        }

        // Get the item data before destroying the world entity
        if (!itemEntity.HasComponent<ItemComponent>())
        {
            Console.WriteLine("Cannot pickup item - missing ItemComponent");
            return;
        }

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

            Console.WriteLine($"Item picked up successfully!");
        }
        else
        {
            // If inventory is full, clean up the temporary inventory entity
            // TODO: Properly destroy the temporary entity
            Console.WriteLine("Could not pick up item - inventory might be full");
        }
    }

    private void HandleChestInteraction(Entity playerEntity, Entity chestEntity)
    {
        Console.WriteLine($"Chest interaction detected - opening chest UI for chest {chestEntity.Id}");
        
        // Send chest UI open event
        _eventBus?.Send(new ChestUIOpenEvent(chestEntity, playerEntity));
    }

    private void HandleDoorInteraction(Entity playerEntity, Entity doorEntity)
    {
        // Simple door interaction - toggle door state
        Console.WriteLine("Door interaction detected - toggling door (not fully implemented)");
        
        // TODO: Implement door opening/closing logic
        // This might involve changing sprites, collision, etc.
    }
}