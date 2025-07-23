using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PrisonBreak.Systems;

namespace PrisonBreak.ECS.Systems;

public class InventoryUIRenderSystem : IGameSystem
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
        // Subscribe to inventory events to update UI in real-time
        _eventBus?.Subscribe<ItemAddedEvent>(OnItemAdded);
        _eventBus?.Subscribe<ItemRemovedEvent>(OnItemRemoved);
    }

    public void Update(GameTime gameTime)
    {
        // No frame-by-frame logic needed for UI rendering
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        // Get all inventory slot UI entities
        var slotEntities = _entityManager.GetEntitiesWith<InventorySlotUIComponent, TransformComponent, SpriteComponent>().ToList();

        foreach (var slotEntity in slotEntities)
        {
            var slotUI = slotEntity.GetComponent<InventorySlotUIComponent>();

            // Skip invisible slots
            if (!slotUI.IsVisible)
                continue;

            var transform = slotEntity.GetComponent<TransformComponent>();
            var sprite = slotEntity.GetComponent<SpriteComponent>();

            // Draw slot background (your 16x16 inventory slot image)
            if (sprite.Sprite != null && sprite.Visible && sprite.Sprite.CurrentRegion != null)
            {
                sprite.Sprite.CurrentRegion.Draw(spriteBatch, transform.Position, sprite.Tint,
                    transform.Rotation, Vector2.Zero, transform.Scale, SpriteEffects.None, 0f);
            }

            // Draw item sprite on top if slot contains an item
            if (slotUI.ContainedItem != null && slotUI.ContainedItem.HasComponent<SpriteComponent>())
            {
                var itemSprite = slotUI.ContainedItem.GetComponent<SpriteComponent>();
                if (itemSprite.Sprite != null && itemSprite.Visible && itemSprite.Sprite.CurrentRegion != null)
                {
                    // Draw item sprite centered in the slot
                    itemSprite.Sprite.CurrentRegion.Draw(spriteBatch, transform.Position, itemSprite.Tint,
                        0f, Vector2.Zero, transform.Scale, SpriteEffects.None, 0f);
                }
            }
        }
    }

    public void Shutdown()
    {
        // Unsubscribe from events
        _eventBus?.Unsubscribe<ItemAddedEvent>(OnItemAdded);
        _eventBus?.Unsubscribe<ItemRemovedEvent>(OnItemRemoved);
    }

    private void OnItemAdded(ItemAddedEvent evt)
    {
        Console.WriteLine($"[DEBUG] InventoryUI: ItemAddedEvent received - PlayerId: {evt.PlayerId}, SlotIndex: {evt.SlotIndex}, ItemEntity: {evt.ItemEntity?.Id}");
        
        // Find the UI slot entity for this player and slot index
        var slotEntities = _entityManager.GetEntitiesWith<InventorySlotUIComponent>();
        Console.WriteLine($"[DEBUG] InventoryUI: Found {slotEntities.Count()} slot UI entities");
        
        var targetSlot = slotEntities.FirstOrDefault(entity =>
        {
            var slotUI = entity.GetComponent<InventorySlotUIComponent>();
            return slotUI.PlayerId == evt.PlayerId && slotUI.SlotIndex == evt.SlotIndex;
        });

        if (targetSlot != null)
        {
            Console.WriteLine($"[DEBUG] InventoryUI: Found target slot {targetSlot.Id} for player {evt.PlayerId}, slot {evt.SlotIndex}");
            // Update the slot UI to show the new item
            ref var slotUI = ref targetSlot.GetComponent<InventorySlotUIComponent>();
            slotUI.ContainedItem = evt.ItemEntity;
            Console.WriteLine($"[DEBUG] InventoryUI: Updated slot UI with item {evt.ItemEntity?.Id}");
        }
        else
        {
            Console.WriteLine($"[DEBUG] InventoryUI: Could not find target slot for player {evt.PlayerId}, slot {evt.SlotIndex}");
        }
    }

    private void OnItemRemoved(ItemRemovedEvent evt)
    {
        // Find the UI slot entity for this player and slot index
        var slotEntities = _entityManager.GetEntitiesWith<InventorySlotUIComponent>();
        var targetSlot = slotEntities.FirstOrDefault(entity =>
        {
            var slotUI = entity.GetComponent<InventorySlotUIComponent>();
            return slotUI.PlayerId == evt.PlayerId && slotUI.SlotIndex == evt.SlotIndex;
        });

        if (targetSlot != null)
        {
            // Clear the item from the slot UI
            ref var slotUI = ref targetSlot.GetComponent<InventorySlotUIComponent>();
            slotUI.ContainedItem = null;
        }
    }
}