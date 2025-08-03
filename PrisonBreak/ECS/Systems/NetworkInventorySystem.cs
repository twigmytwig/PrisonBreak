using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PrisonBreak.Systems;
using PrisonBreak.Core.Networking;
using PrisonBreak.Managers;
using PrisonBreak.Multiplayer.Core;

namespace PrisonBreak.ECS.Systems;

/// <summary>
/// NetworkInventorySystem handles multiplayer synchronization of inventory operations.
/// It wraps the existing InventorySystem and adds networking functionality.
/// </summary>
public class NetworkInventorySystem : IGameSystem
{
    private ComponentEntityManager _entityManager;
    private EventBus _eventBus;
    private NetworkManager _networkManager;
    private InventorySystem _inventorySystem;
    
    // Network ID counter for items (start at 2000 to avoid conflicts with players 1-100 and AI cops 1001-1999)
    private static int _nextItemNetworkId = 2000;

    public void SetEntityManager(ComponentEntityManager entityManager)
    {
        _entityManager = entityManager;
    }

    public void SetEventBus(EventBus eventBus)
    {
        _eventBus = eventBus;
    }
    
    public void SetNetworkManager(NetworkManager networkManager)
    {
        _networkManager = networkManager;
    }
    
    public void SetInventorySystem(InventorySystem inventorySystem)
    {
        _inventorySystem = inventorySystem;
    }
    
    /// <summary>
    /// Get the internal InventorySystem for direct access (used by NetworkManager)
    /// </summary>
    public InventorySystem GetInventorySystem()
    {
        return _inventorySystem;
    }

    public void Initialize()
    {
        // Subscribe to inventory events to detect changes that need networking
        _eventBus?.Subscribe<ItemAddedEvent>(OnItemAdded);
        _eventBus?.Subscribe<ItemRemovedEvent>(OnItemRemoved);
        _eventBus?.Subscribe<InventoryFullEvent>(OnInventoryFull);
        
        // Subscribe to interaction events for networked processing
        _eventBus?.Subscribe<InteractionInputEvent>(OnInteractionInput);
    }

    public void Update(GameTime gameTime)
    {
        // NetworkInventorySystem is event-driven, no frame-by-frame logic needed
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        // NetworkInventorySystem doesn't render anything
    }

    public void Shutdown()
    {
        // Unsubscribe from events
        _eventBus?.Unsubscribe<ItemAddedEvent>(OnItemAdded);
        _eventBus?.Unsubscribe<ItemRemovedEvent>(OnItemRemoved);
        _eventBus?.Unsubscribe<InventoryFullEvent>(OnInventoryFull);
        _eventBus?.Unsubscribe<InteractionInputEvent>(OnInteractionInput);
    }

    /// <summary>
    /// Handle interaction input events in multiplayer mode
    /// </summary>
    private void OnInteractionInput(InteractionInputEvent evt)
    {
        // Only intercept if we're in multiplayer mode and not the host
        if (_networkManager == null || _networkManager.IsHost)
            return;
            
        var playerEntity = _entityManager.GetEntity(evt.EntityId);
        if (playerEntity == null || !playerEntity.HasComponent<TransformComponent>())
            return;

        // Find nearby interactable item
        var nearbyInteractable = FindNearestInteractableItem(playerEntity);
        if (nearbyInteractable != null)
        {
            // Send interaction request to host instead of processing locally
            SendInteractionRequest(playerEntity, nearbyInteractable);
        }
    }

    /// <summary>
    /// Send interaction request to host for validation
    /// </summary>
    private void SendInteractionRequest(Entity player, Entity target)
    {
        if (!player.HasComponent<NetworkComponent>() || !target.HasComponent<NetworkComponent>())
            return;

        var playerNetworkId = player.GetComponent<NetworkComponent>().NetworkId;
        var targetNetworkId = target.GetComponent<NetworkComponent>().NetworkId;
        var playerPosition = player.GetComponent<TransformComponent>().Position;
        
        var requestMessage = new InteractionRequestMessage(
            playerNetworkId, 
            targetNetworkId, 
            "pickup", 
            playerPosition
        );
        
        _networkManager.SendInteractionRequest(requestMessage);
        Console.WriteLine($"[NetworkInventorySystem] Client sent interaction request for item {targetNetworkId}");
    }

    /// <summary>
    /// Find the nearest interactable item (similar to InteractionSystem logic)
    /// </summary>
    private Entity FindNearestInteractableItem(Entity playerEntity)
    {
        var playerTransform = playerEntity.GetComponent<TransformComponent>();
        Vector2 playerCenter = GetSpriteCenterPosition(playerEntity, playerTransform);
        
        var interactableItems = _entityManager.GetEntitiesWith<InteractableComponent, TransformComponent, ItemComponent>().ToList();
        
        Entity closestItem = null;
        float closestDistance = float.MaxValue;

        foreach (var item in interactableItems)
        {
            var interactableComponent = item.GetComponent<InteractableComponent>();
            var itemTransform = item.GetComponent<TransformComponent>();
            
            if (!interactableComponent.IsActive)
                continue;

            Vector2 itemCenter = GetSpriteCenterPosition(item, itemTransform);
            float distance = Vector2.Distance(playerCenter, itemCenter);

            if (distance <= interactableComponent.InteractionRange && distance < closestDistance)
            {
                closestDistance = distance;
                closestItem = item;
            }
        }

        return closestItem;
    }

    /// <summary>
    /// Calculate sprite center position (copied from InteractionSystem)
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
        if (baseSpriteWidth == 0 || baseSpriteHeight == 0)
        {
            baseSpriteWidth = 32f;
            baseSpriteHeight = 32f;
        }
        
        // Calculate the visual size after scaling
        float scaledWidth = baseSpriteWidth * transform.Scale.X;
        float scaledHeight = baseSpriteHeight * transform.Scale.Y;
        
        // Return center position
        return transform.Position + new Vector2(scaledWidth / 2f, scaledHeight / 2f);
    }

    /// <summary>
    /// Process host-side interaction request validation and processing
    /// </summary>
    public void ProcessInteractionRequest(InteractionRequestMessage request)
    {
        if (!_networkManager.IsHost)
            return;

        Console.WriteLine($"[NetworkInventorySystem] Host processing interaction request: Player {request.PlayerId} → Item {request.TargetNetworkId}");

        // Find the entities
        var player = FindEntityByNetworkId(request.PlayerId);
        var target = FindEntityByNetworkId(request.TargetNetworkId);
        
        Console.WriteLine($"[NetworkInventorySystem] Found entities - Player: {player?.Id}, Target: {target?.Id}");

        if (!ValidateInteractionRequest(player, target, request))
        {
            // Send rejection
            var rejection = new InteractionRejectedMessage(request.PlayerId, request.TargetNetworkId, "Invalid interaction");
            _networkManager.SendInteractionRejected(rejection);
            Console.WriteLine($"[NetworkInventorySystem] Host rejected interaction request: Player {request.PlayerId}");
            return;
        }

        // Process the interaction using existing inventory system
        if (request.InteractionType == "pickup" && target.HasComponent<ItemComponent>())
        {
            ProcessItemPickup(player, target);
        }
    }

    /// <summary>
    /// Validate interaction request on host side
    /// </summary>
    private bool ValidateInteractionRequest(Entity player, Entity target, InteractionRequestMessage request)
    {
        // Entity exists check
        if (player == null || target == null)
            return false;

        // Range validation
        var playerPos = player.GetComponent<TransformComponent>().Position;
        var targetPos = target.GetComponent<TransformComponent>().Position;
        
        if (!target.HasComponent<InteractableComponent>())
            return false;
            
        var interactable = target.GetComponent<InteractableComponent>();
        if (!interactable.IsActive)
            return false;
            
        float distance = Vector2.Distance(playerPos, targetPos);
        if (distance > interactable.InteractionRange)
            return false;

        // Inventory space check for pickups
        if (request.InteractionType == "pickup")
        {
            if (!player.HasComponent<InventoryComponent>())
                return false;
                
            var inventory = player.GetComponent<InventoryComponent>();
            if (inventory.ItemCount >= inventory.MaxSlots)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Process item pickup on host and broadcast result
    /// </summary>
    private void ProcessItemPickup(Entity player, Entity item)
    {
        Console.WriteLine($"[NetworkInventorySystem] Host processing pickup: Player {player.Id} → Item {item.Id}");
        
        // Use existing inventory system to process pickup
        bool success = _inventorySystem.TryAddItem(player, item);
        
        if (success)
        {
            var playerNetworkId = player.GetComponent<NetworkComponent>().NetworkId;
            var itemNetworkId = item.GetComponent<NetworkComponent>().NetworkId;
            var itemPosition = item.GetComponent<TransformComponent>().Position;
            var itemType = item.GetComponent<ItemComponent>().ItemId;
            
            // Find which slot the item was added to
            var inventory = player.GetComponent<InventoryComponent>();
            int slotIndex = -1;
            for (int i = 0; i < inventory.MaxSlots; i++)
            {
                if (inventory.Items[i] == item)
                {
                    slotIndex = i;
                    break;
                }
            }

            // Remove item from world by destroying it completely
            if (item.HasComponent<SpriteComponent>())
            {
                item.RemoveComponent<SpriteComponent>();
            }
            if (item.HasComponent<InteractableComponent>())
            {
                item.RemoveComponent<InteractableComponent>();
            }
            if (item.HasComponent<TransformComponent>())
            {
                item.RemoveComponent<TransformComponent>();
            }
            
            Console.WriteLine($"[NetworkInventorySystem] Removed item {itemNetworkId} from world");

            // Broadcast pickup success to all clients
            var pickupMessage = new ItemPickupMessage(
                playerNetworkId, 
                itemNetworkId, 
                slotIndex, 
                true, 
                itemPosition, 
                itemType
            );
            
            _networkManager.SendItemPickup(pickupMessage);
            Console.WriteLine($"[NetworkInventorySystem] Host processed successful pickup: Player {playerNetworkId} picked up {itemType}");
        }
        else
        {
            // Broadcast pickup failure
            var playerNetworkId = player.GetComponent<NetworkComponent>().NetworkId;
            var itemNetworkId = item.GetComponent<NetworkComponent>().NetworkId;
            var itemPosition = item.GetComponent<TransformComponent>().Position;
            var itemType = item.GetComponent<ItemComponent>().ItemId;
            
            var pickupMessage = new ItemPickupMessage(
                playerNetworkId, 
                itemNetworkId, 
                -1, 
                false, 
                itemPosition, 
                itemType
            );
            
            _networkManager.SendItemPickup(pickupMessage);
            Console.WriteLine($"[NetworkInventorySystem] Host processed failed pickup: Player {playerNetworkId} inventory full");
        }
    }

    /// <summary>
    /// Apply pickup result received from host
    /// </summary>
    public void ApplyItemPickupResult(ItemPickupMessage message)
    {
        var item = FindEntityByNetworkId(message.ItemNetworkId);
        
        if (message.Success)
        {
            // On client: find the local player (the one with input component) regardless of network ID
            // This ensures the item goes to the correct local player's inventory
            var localPlayer = FindLocalPlayer(message.PlayerId);
            
            if (localPlayer != null && localPlayer.HasComponent<InventoryComponent>())
            {
                // Create a new item entity for the client's inventory using the item ID
                var inventoryItem = _entityManager.CreateItem(message.ItemType);
                
                // Copy sprite component from original item or create one
                if (item != null && item.HasComponent<SpriteComponent>())
                {
                    var originalSprite = item.GetComponent<SpriteComponent>();
                    inventoryItem.AddComponent(new SpriteComponent(originalSprite.Sprite)
                    {
                        Visible = true,
                        Tint = originalSprite.Tint
                    });
                }
                else
                {
                    // Create sprite based on item type if original doesn't exist
                    // This is a fallback - the sprite system should handle this
                    Console.WriteLine($"[NetworkInventorySystem] Creating item {message.ItemType} without original sprite reference");
                }
                
                // Use InventorySystem to add the item properly (this will fire the ItemAddedEvent)
                bool added = _inventorySystem.TryAddItem(localPlayer, inventoryItem);
                if (added)
                {
                    Console.WriteLine($"[NetworkInventorySystem] Successfully added {message.ItemType} to local player inventory");
                }
                else
                {
                    Console.WriteLine($"[NetworkInventorySystem] Warning: Could not add item to inventory on client");
                }
            }
            else
            {
                Console.WriteLine($"[NetworkInventorySystem] Could not find local player for pickup result");
            }

            // Remove item from world on all clients
            if (item != null)
            {
                if (item.HasComponent<SpriteComponent>())
                {
                    item.RemoveComponent<SpriteComponent>();
                }
                if (item.HasComponent<InteractableComponent>())
                {
                    item.RemoveComponent<InteractableComponent>();
                }
                if (item.HasComponent<TransformComponent>())
                {
                    item.RemoveComponent<TransformComponent>();
                }
                Console.WriteLine($"[NetworkInventorySystem] Client removed item {message.ItemNetworkId} from world");
            }

            Console.WriteLine($"[NetworkInventorySystem] Applied successful pickup result: Player {message.PlayerId} picked up {message.ItemType}");
        }
        else
        {
            Console.WriteLine($"[NetworkInventorySystem] Applied failed pickup result: Player {message.PlayerId} couldn't pick up {message.ItemType}");
        }
    }

    /// <summary>
    /// Event handler for local inventory changes (for host broadcasting)
    /// </summary>
    private void OnItemAdded(ItemAddedEvent evt)
    {
        // Only broadcast if we're the host
        if (_networkManager == null || !_networkManager.IsHost)
            return;

        Console.WriteLine($"[NetworkInventorySystem] Host detected item added: Player {evt.PlayerId}, Slot {evt.SlotIndex}");
    }

    private void OnItemRemoved(ItemRemovedEvent evt)
    {
        // Only broadcast if we're the host
        if (_networkManager == null || !_networkManager.IsHost)
            return;

        Console.WriteLine($"[NetworkInventorySystem] Host detected item removed: Player {evt.PlayerId}, Slot {evt.SlotIndex}");
    }

    private void OnInventoryFull(InventoryFullEvent evt)
    {
        Console.WriteLine($"[NetworkInventorySystem] Inventory full for player {evt.PlayerId}");
    }

    /// <summary>
    /// Find entity by network ID
    /// </summary>
    private Entity FindEntityByNetworkId(int networkId)
    {
        var networkedEntities = _entityManager.GetEntitiesWith<NetworkComponent>();
        return networkedEntities.FirstOrDefault(e => e.GetComponent<NetworkComponent>().NetworkId == networkId);
    }

    /// <summary>
    /// Find the local player entity - the one that this client controls
    /// On client side, we should only add items to OUR local player, regardless of who picked up the item
    /// </summary>
    private Entity FindLocalPlayer(int networkPlayerId)
    {
        // Always find the local controllable player (the one with PlayerInputComponent)
        // This ensures items go to the correct player's inventory on each client
        var localPlayers = _entityManager.GetEntitiesWith<PlayerInputComponent, InventoryComponent>();
        var localPlayer = localPlayers.FirstOrDefault();
        
        if (localPlayer != null)
        {
            var localNetworkId = localPlayer.HasComponent<NetworkComponent>() ? 
                localPlayer.GetComponent<NetworkComponent>().NetworkId : -1;
            Console.WriteLine($"[NetworkInventorySystem] Found local player by input component: Entity {localPlayer.Id}, NetworkId {localNetworkId}");
            
            // Only apply the pickup if it's for THIS local player
            if (localNetworkId == networkPlayerId)
            {
                return localPlayer;
            }
            else
            {
                Console.WriteLine($"[NetworkInventorySystem] Pickup message is for player {networkPlayerId}, but local player is {localNetworkId}. Ignoring.");
                return null;
            }
        }
        
        Console.WriteLine($"[NetworkInventorySystem] Could not find local player with input component");
        return null;
    }

    /// <summary>
    /// Assign network ID to an item entity
    /// </summary>
    public static void AssignNetworkIdToItem(Entity item)
    {
        if (!item.HasComponent<NetworkComponent>())
        {
            item.AddComponent(new NetworkComponent(
                networkId: _nextItemNetworkId++,
                authority: NetworkConfig.NetworkAuthority.Server,
                syncTransform: false,
                syncMovement: false,
                syncInventory: true,
                ownerId: -1 // Items don't have owners initially
            ));
        }
    }
}