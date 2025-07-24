using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PrisonBreak.Systems;

namespace PrisonBreak.ECS.Systems;

public class InventorySystem : IGameSystem
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
        // Subscribe to relevant events when they're added in future phases
    }

    public void Update(GameTime gameTime)
    {
        // No frame-by-frame logic needed for core inventory management
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        // Inventory system doesn't render anything
    }

    public void Shutdown()
    {
        // Unsubscribe from events when they're added in future phases
    }

    /// <summary>
    /// Attempts to add an item to the first available slot in player's inventory
    /// </summary>
    public bool TryAddItem(Entity playerEntity, Entity itemEntity)
    {
        if (playerEntity == null || itemEntity == null)
            return false;

        if (!playerEntity.HasComponent<InventoryComponent>())
            return false;

        ref var inventory = ref playerEntity.GetComponent<InventoryComponent>();

        // Check if inventory is full
        if (IsInventoryFull(playerEntity))
        {
            // Send inventory full event
            if (playerEntity.HasComponent<PlayerTag>())
            {
                var playerTag = playerEntity.GetComponent<PlayerTag>();
                _eventBus?.Send(new InventoryFullEvent(playerTag.PlayerId, itemEntity));
            }
            return false;
        }

        // Find first empty slot
        for (int i = 0; i < inventory.MaxSlots; i++)
        {
            if (inventory.Items[i] == null)
            {
                inventory.Items[i] = itemEntity;
                inventory.ItemCount++;

                // Send item added event
                if (playerEntity.HasComponent<PlayerTag>())
                {
                    var playerTag = playerEntity.GetComponent<PlayerTag>();
                    Console.WriteLine($"[DEBUG] InventorySystem: Sending ItemAddedEvent - PlayerId: {playerTag.PlayerId}, ItemEntity: {itemEntity.Id}, SlotIndex: {i}");
                    _eventBus?.Send(new ItemAddedEvent(playerTag.PlayerId, itemEntity, i));
                }
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Attempts to remove an item from a specific slot
    /// </summary>
    public bool TryRemoveItem(Entity playerEntity, int slotIndex)
    {
        if (playerEntity == null || !playerEntity.HasComponent<InventoryComponent>())
            return false;

        ref var inventory = ref playerEntity.GetComponent<InventoryComponent>();

        if (slotIndex < 0 || slotIndex >= inventory.MaxSlots)
            return false;

        if (inventory.Items[slotIndex] == null)
            return false;

        var removedItem = inventory.Items[slotIndex];
        inventory.Items[slotIndex] = null;
        inventory.ItemCount--;

        // Send item removed event
        if (playerEntity.HasComponent<PlayerTag>())
        {
            var playerTag = playerEntity.GetComponent<PlayerTag>();
            _eventBus?.Send(new ItemRemovedEvent(playerTag.PlayerId, removedItem, slotIndex));
        }

        return true;
    }

    /// <summary>
    /// Checks if the player's inventory is full
    /// </summary>
    public bool IsInventoryFull(Entity playerEntity)
    {
        if (playerEntity == null || !playerEntity.HasComponent<InventoryComponent>())
            return true;

        var inventory = playerEntity.GetComponent<InventoryComponent>();
        return inventory.ItemCount >= inventory.MaxSlots;
    }

    /// <summary>
    /// Gets the item at a specific slot (returns null if empty)
    /// </summary>
    public Entity GetItemAtSlot(Entity playerEntity, int slotIndex)
    {
        if (playerEntity == null || !playerEntity.HasComponent<InventoryComponent>())
            return null;

        var inventory = playerEntity.GetComponent<InventoryComponent>();

        if (slotIndex < 0 || slotIndex >= inventory.MaxSlots)
            return null;

        return inventory.Items[slotIndex];
    }

    /// <summary>
    /// Initializes a player's inventory based on their player type
    /// </summary>
    public void InitializePlayerInventory(Entity playerEntity)
    {
        if (playerEntity == null)
            return;

        if (!playerEntity.HasComponent<PlayerTypeComponent>())
        {
            Console.WriteLine("Warning: Cannot initialize inventory - player entity missing PlayerTypeComponent");
            return;
        }

        var playerType = playerEntity.GetComponent<PlayerTypeComponent>();
        var inventory = new InventoryComponent(playerType.InventorySlots);
        playerEntity.AddComponent(inventory);

        Console.WriteLine($"Initialized inventory for player with {playerType.InventorySlots} slots");
    }

    /// <summary>
    /// Gets the item at a specific slot in a container (returns null if empty)
    /// </summary>
    public Entity GetContainerItemAtSlot(Entity containerEntity, int slotIndex)
    {
        if (containerEntity == null || !containerEntity.HasComponent<ContainerComponent>())
            return null;

        var container = containerEntity.GetComponent<ContainerComponent>();

        if (slotIndex < 0 || slotIndex >= container.MaxItems)
            return null;

        return container.ContainedItems[slotIndex];
    }

    /// <summary>
    /// Attempts to add an item to the first available slot in a container
    /// </summary>
    public bool TryAddItemToContainer(Entity containerEntity, Entity itemEntity)
    {
        if (containerEntity == null || itemEntity == null)
            return false;

        if (!containerEntity.HasComponent<ContainerComponent>())
            return false;

        ref var container = ref containerEntity.GetComponent<ContainerComponent>();

        // Check if container is full
        if (container.ItemCount >= container.MaxItems)
            return false;

        // Find first empty slot
        for (int i = 0; i < container.MaxItems; i++)
        {
            if (container.ContainedItems[i] == null)
            {
                container.ContainedItems[i] = itemEntity;
                container.ItemCount++;
                Console.WriteLine($"[DEBUG] InventorySystem: Added item {itemEntity.Id} to container slot {i}");
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Attempts to remove an item from a specific slot in a container
    /// </summary>
    public bool TryRemoveItemFromContainer(Entity containerEntity, int slotIndex)
    {
        if (containerEntity == null || !containerEntity.HasComponent<ContainerComponent>())
            return false;

        ref var container = ref containerEntity.GetComponent<ContainerComponent>();

        if (slotIndex < 0 || slotIndex >= container.MaxItems)
            return false;

        if (container.ContainedItems[slotIndex] == null)
            return false;

        var removedItem = container.ContainedItems[slotIndex];
        container.ContainedItems[slotIndex] = null;
        container.ItemCount--;

        Console.WriteLine($"[DEBUG] InventorySystem: Removed item {removedItem.Id} from container slot {slotIndex}");
        return true;
    }

    /// <summary>
    /// Attempts to transfer an item from player inventory to container
    /// </summary>
    public bool TryTransferItemToContainer(Entity playerEntity, Entity containerEntity, int playerSlotIndex)
    {
        if (playerEntity == null || containerEntity == null)
            return false;

        // Get the item from player inventory
        var itemEntity = GetItemAtSlot(playerEntity, playerSlotIndex);
        if (itemEntity == null)
            return false;

        // Try to add to container
        if (!TryAddItemToContainer(containerEntity, itemEntity))
            return false;

        // Remove from player inventory
        if (!TryRemoveItem(playerEntity, playerSlotIndex))
        {
            // Rollback: try to remove the item from container (though this should not fail)
            TryRemoveItemFromContainer(containerEntity, FindItemInContainer(containerEntity, itemEntity));
            return false;
        }

        Console.WriteLine($"[DEBUG] InventorySystem: Successfully transferred item from player slot {playerSlotIndex} to container");
        return true;
    }

    /// <summary>
    /// Attempts to transfer an item from container to player inventory
    /// </summary>
    public bool TryTransferItemToPlayer(Entity containerEntity, Entity playerEntity, int containerSlotIndex)
    {
        if (containerEntity == null || playerEntity == null)
            return false;

        // Get the item from container
        var itemEntity = GetContainerItemAtSlot(containerEntity, containerSlotIndex);
        if (itemEntity == null)
            return false;

        // Try to add to player inventory
        if (!TryAddItem(playerEntity, itemEntity))
            return false;

        // Remove from container
        if (!TryRemoveItemFromContainer(containerEntity, containerSlotIndex))
        {
            // Rollback: try to remove the item from player inventory
            TryRemoveItem(playerEntity, FindItemInPlayerInventory(playerEntity, itemEntity));
            return false;
        }

        Console.WriteLine($"[DEBUG] InventorySystem: Successfully transferred item from container slot {containerSlotIndex} to player");
        return true;
    }

    /// <summary>
    /// Helper method to find an item's slot index in a container
    /// </summary>
    private int FindItemInContainer(Entity containerEntity, Entity itemEntity)
    {
        if (containerEntity == null || itemEntity == null || !containerEntity.HasComponent<ContainerComponent>())
            return -1;

        var container = containerEntity.GetComponent<ContainerComponent>();
        for (int i = 0; i < container.MaxItems; i++)
        {
            if (container.ContainedItems[i] == itemEntity)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Helper method to find an item's slot index in player inventory
    /// </summary>
    private int FindItemInPlayerInventory(Entity playerEntity, Entity itemEntity)
    {
        if (playerEntity == null || itemEntity == null || !playerEntity.HasComponent<InventoryComponent>())
            return -1;

        var inventory = playerEntity.GetComponent<InventoryComponent>();
        for (int i = 0; i < inventory.MaxSlots; i++)
        {
            if (inventory.Items[i] == itemEntity)
                return i;
        }
        return -1;
    }
}