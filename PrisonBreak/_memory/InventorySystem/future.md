# Inventory System Implementation Plan

## Overview

Comprehensive inventory system allowing players to collect, manage, and interact with items through chests and containers. Built using the existing ECS architecture with event-driven communication.

## Current Status

✅ **Phase 1: Core Inventory Functionality** - COMPLETED  
✅ **Phase 2: Interaction System** - COMPLETED  
🚧 **Phase 3: Chest/Container UI** - Partially Completed

### ✅ COMPLETED FEATURES:

✅ **ItemComponent** - Defines item properties (name, type, stackable, stack size)  
✅ **InventoryComponent** - Player inventory with max slots and item array  
✅ **InteractableComponent** - Mark entities as interactable (pickup, chest, door)  
✅ **ContainerComponent** - Chest/container storage with item arrays  
✅ **PlayerTypeComponent.InventorySlots** - Different capacities per player type (Prisoner: 3, Cop: 4)  
✅ **InventorySystem.cs** - COMPLETED with all core methods implemented and integrated into GameplayScene
✅ **InteractionSystem.cs** - COMPLETED proximity detection and item pickup handling
✅ **InventoryUIRenderSystem.cs** - COMPLETED UI rendering system for inventory display
✅ **ComponentInputSystem** - Extended with E key/X button detection for interactions
✅ **Inventory Events** - ItemAddedEvent, ItemRemovedEvent, InventoryFullEvent, InteractionInputEvent, InteractionEvent
✅ **Player Inventory Initialization** - CreatePlayer() method automatically initializes inventory based on player type
✅ **Entity Factory Methods** - CreateItemAtPosition(), CreateChest(), enhanced CreateItem()
✅ **Item Pickup System** - Full world→inventory transfer with separate entity creation
✅ **UI Integration** - Real-time inventory display with item pickup visual feedback
✅ **Chest Sprite Integration** - Added chest sprite to UI atlas configuration
✅ **ChestUIRenderSystem** - Complete chest overlay UI rendering system with modal interface
✅ **Chest UI Events** - ChestUIOpenEvent/CloseEvent for clean UI state management
✅ **OverlayAtlas System** - Separate texture atlas for large UI panels (48x48+)
✅ **Chest UI Input Handling** - ESC key and gamepad B button support for closing chest UI
✅ **UI Scaling System** - 4x scaling (48x48 → 192x192) for better visibility
✅ **Input State Management** - Proper key press detection prevents accidental menu transitions

## Implementation Phases

### ✅ Phase 1: Core Inventory Functionality - COMPLETED

#### ✅ 1.1 InventorySystem.cs - COMPLETED

**All methods implemented in `/ECS/Systems/InventorySystem.cs`:**

```csharp
✅ public bool TryAddItem(Entity playerEntity, Entity itemEntity)
✅ public bool TryRemoveItem(Entity playerEntity, int slotIndex)
✅ public bool IsInventoryFull(Entity playerEntity)
✅ public Entity GetItemAtSlot(Entity playerEntity, int slotIndex)
✅ public void InitializePlayerInventory(Entity playerEntity)
```

**✅ Event handling implemented:**

- ✅ Sends ItemAddedEvent when items are added
- ✅ Sends ItemRemovedEvent when items are removed
- ✅ Sends InventoryFullEvent when inventory is full
- Ready for future pickup/drop event subscriptions

#### ✅ 1.2 Inventory Events - COMPLETED

**Added to `EventSystem.cs`:**

```csharp
✅ ItemAddedEvent { int PlayerId, Entity ItemEntity, int SlotIndex }
✅ ItemRemovedEvent { int PlayerId, Entity ItemEntity, int SlotIndex }
✅ InventoryFullEvent { int PlayerId, Entity AttemptedItem }
```

**Still needed for Phase 2:**

```csharp
🚧 InteractionEvent { int PlayerId, Entity InteractableEntity, string InteractionType }
```

#### ✅ 1.3 Player Inventory Initialization - COMPLETED

**✅ ComponentEntityManager.CreatePlayer() updated:**

- ✅ Automatically adds InventoryComponent with correct MaxSlots from PlayerTypeComponent
- ✅ Items array initialized with proper capacity (3 for Prisoner, 4 for Cop)
- ✅ Integration tested and working

#### ✅ 1.4 Basic Inventory UI - COMPLETED

**✅ InventoryUIRenderSystem.cs implemented:**

- ✅ Visual slot grid rendering based on player inventory capacity
- ✅ Empty slot rendering with placeholder appearance
- ✅ Item icon rendering for occupied slots
- ✅ Real-time inventory state display during gameplay
- ✅ UI atlas support for consistent visual design
- ✅ Proper positioning and spacing of inventory elements

### ✅ Phase 2: Interaction System - COMPLETED

#### ✅ 2.1 InteractableComponent & ContainerComponent - COMPLETED

**✅ Added to `Components.cs`:**

```csharp
✅ public struct InteractableComponent
{
    public string InteractionType;    // "chest", "pickup", "door"
    public float InteractionRange;    // Distance for interaction
    public bool IsActive;            // Can be interacted with
    public string DisplayText;       // "Press E to open chest"
}

✅ public struct ContainerComponent
{
    public int MaxItems;
    public Entity[] ContainedItems;
    public int ItemCount;
    public string ContainerType;     // "chest", "locker", "crate"
}
```

#### ✅ 2.2 ComponentInputSystem Extended - COMPLETED

**✅ Interaction input detection implemented:**

```csharp
✅ E key detection (keyboard) with proper press/release handling
✅ X button detection (gamepad) with proper press/release handling
✅ InteractionInputEvent sent to InteractionSystem
✅ Previous input state tracking to prevent key holding
```

#### ✅ 2.3 InteractionSystem - COMPLETED

**✅ New system file: `/ECS/Systems/InteractionSystem.cs`**

```csharp
✅ public class InteractionSystem : IGameSystem
{
    ✅ OnInteractionInput() - Handle E/X button presses
    ✅ FindNearestInteractable() - Proximity detection within interaction range
    ✅ ProcessInteraction() - Route to appropriate interaction handler
    ✅ HandleItemPickup() - Add items to inventory & remove from world
    ✅ HandleChestInteraction() - Debug placeholder for Phase 3
}
```

**✅ Key features implemented:**

- ✅ Proximity detection between players and interactables (64px range for items)
- ✅ Event-driven interaction processing via InteractionInputEvent
- ✅ Item pickup with proper world→inventory entity transfer
- ✅ Separate inventory entities to prevent visibility conflicts
- ✅ Debug logging for troubleshooting interaction issues

#### ✅ 2.4 Entity Factory Methods - COMPLETED

**✅ Enhanced ComponentEntityManager with factory methods:**

```csharp
✅ public Entity CreateItemAtPosition(string itemId, Vector2 position)
{
    // Creates world-placed items for pickup
    // Uses ItemDatabase for item properties
    // 2x scale for better visibility (32x32 visual size)
    // 64px interaction range for easier pickup
    // InteractableComponent with "pickup" type
}

✅ public Entity CreateChest(Vector2 position, string[] itemIds)
{
    // Creates interactable chest entities
    // Loads chest sprite from UI atlas
    // ContainerComponent with 10 item slots
    // InteractableComponent with "chest" type
    // Optional initial items population
}

✅ Enhanced CreateItem(string itemId) - uses ItemDatabase and UI atlas
✅ CreateKey() - convenience method for key creation
```

#### ✅ 2.5 GameplayScene Integration - COMPLETED

**✅ InteractionSystem added to system pipeline:**

```csharp
✅ InteractionSystem initialization and setup
✅ System added to GameplayScene execution order
✅ Test items and chests created in InitializeGame()
✅ Debug logging for troubleshooting
```

### ✅ Phase 3: Chest/Container UI - COMPLETED

#### ✅ 3.1 Chest UI Overlay System - COMPLETED

**✅ ChestUIRenderSystem.cs implemented:**

- ✅ Modal chest overlay rendering with 4x scaling (48x48 → 192x192)
- ✅ Event-driven UI state management (ChestUIOpenEvent/CloseEvent)
- ✅ OverlayAtlas system for large UI panels separate from UIAtlas
- ✅ Semi-transparent overlay background for visual clarity
- ✅ Integration with existing GameplayScene and interaction system

**✅ Enhanced Input Handling:**

- ✅ ESC key and gamepad B button support for closing chest UI
- ✅ Input state tracking prevents rapid-fire input processing
- ✅ Clean separation between chest UI input and menu navigation
- ✅ Proper key press detection (not hold) for responsive UI

**✅ Content Pipeline Integration:**

- ✅ overlay-atlas-definition.xml configuration
- ✅ PrisonBreakChestOverlay.png (48x48 chest overlay sprite)
- ✅ EntityConfig.OverlayAtlas configuration
- ✅ MGCB content build integration

#### ✅ 3.2 Inventory Transfer Interface - COMPLETED

**✅ Full inventory management interface implemented:**

- ✅ Player and chest inventory slot display with visual scaling
- ✅ Real-time inventory contents rendering with item icons
- ✅ Selected slot highlighting (yellow tint for active slot)
- ✅ Arrow key/D-pad navigation between slots and inventories
- ✅ Enter/A button item transfer functionality
- ✅ Event-driven slot selection with InventorySlotSelectedEvent
- ✅ Container management methods in InventorySystem
- ✅ Accurate interaction detection with sprite center calculation

**✅ New Systems and Components Added:**

- ✅ `ItemTransferEvent` and `InventorySlotSelectedEvent` in EventSystem
- ✅ Enhanced InventorySystem with container methods: `TryTransferItemToContainer`, `TryTransferItemToPlayer`, `GetContainerItemAtSlot`
- ✅ ChestUIRenderSystem slot selection state tracking and visual highlighting
- ✅ GameplayScene chest UI input handling with navigation and transfer controls
- ✅ InteractionSystem sprite center position calculation for accurate detection

**✅ Input Controls:**

- ✅ Arrow Keys/D-Pad: Navigate between inventory slots (Left/Right) and inventories (Up/Down)
- ✅ Enter/A Button: Transfer selected item between player and chest
- ✅ ESC/B Button: Close chest UI (existing functionality)

### Phase 4: Inventory UI System

#### 4.1 Create InventoryUIScene **DECIDED AGAINST**

**New scene: `InventoryUIScene.cs`**

- Modal overlay similar to StartMenuScene
- Shows player inventory slots
- Shows chest contents when interacting with chests
- Handles item transfer between inventory and chest

**UI Elements:**

- Player inventory grid (3 or 4 slots based on player type)
- Chest inventory grid (when chest is open)
- Item icons and names
- "Drop Item" and "Take Item" buttons
- Close inventory button

#### 4.2 Inventory Rendering Components

Add to `Components.cs`:

```csharp
public struct InventorySlotComponent
{
    public int SlotIndex;
    public bool IsPlayerInventory;    // vs chest inventory
    public Entity ContainedItem;      // null if empty
    public bool IsSelected;
}

public struct InventoryUIComponent
{
    public Entity TargetPlayer;
    public Entity OpenContainer;      // null if just showing player inventory
    public int SelectedSlot;
    public bool IsPlayerSlotSelected; // vs container slot
}
```

#### 4.3 InventoryRenderSystem

**New system: `InventoryRenderSystem.cs`**

- Renders inventory slots as grid
- Shows item icons in slots
- Highlights selected slots
- Draws item tooltips
- Handles drag-and-drop visual feedback

### Phase 5: Integration Points

#### 5.1 Scene Management

**Extend SceneManager to handle inventory overlay:**

- Add `SceneType.InventoryUI`
- Allow inventory to overlay gameplay scene
- Pause gameplay when inventory is open

#### 5.2 Input Integration

**Modified input flow:**

1. ComponentInputSystem detects E key press
2. InteractionSystem checks nearby interactables
3. If chest found, triggers scene transition to InventoryUIScene
4. InventoryUIScene handles item management input
5. On close, returns to gameplay scene

#### 5.3 Collision Integration

**Use existing collision system:**

- InteractionSystem queries entities with InteractableComponent
- Check distance between player and interactables
- Only allow interaction if within range

## File Structure

```
/ECS/
  Components.cs                    // Add InteractableComponent, ContainerComponent, InventorySlotComponent
  EventSystem.cs                   // Add inventory and interaction events

/ECS/Systems/
  InventorySystem.cs              // Core inventory management (existing, needs implementation)
  InteractionSystem.cs            // New - proximity and interaction handling
  InventoryRenderSystem.cs        // New - UI rendering for inventory
  ComponentInputSystem.cs         // Modified - add E key detection

/Scenes/
  InventoryUIScene.cs             // New - modal inventory management UI
  SceneManager.cs                 // Modified - add InventoryUI scene type

/ComponentEntityManager.cs        // Add CreateChest(), CreateItem(), InitializePlayerInventory()
```

## Implementation Order

1. ✅ **Complete InventorySystem.cs** - Core functionality implemented
2. ✅ **Add inventory events** - Communication backbone added to EventSystem
3. ✅ **Initialize player inventories** - CreatePlayer() method updated
4. ✅ **Add InventoryUIRenderSystem** - Basic UI visual representation completed
5. ✅ **Create InteractionSystem** - Handle E key and proximity detection - COMPLETED
6. ✅ **Add InteractableComponent** - Mark chests as interactable - COMPLETED
7. ✅ **Create basic chest entities** - Static containers for testing - COMPLETED
8. ✅ **Entity factory methods** - CreateItemAtPosition, CreateChest - COMPLETED
9. ✅ **Item pickup functionality** - Full world→inventory transfer - COMPLETED
10. ✅ **Build Chest UI Overlay System** - Modal overlay with proper input handling - COMPLETED
11. ✅ **Build Inventory Transfer Interface** - Item management within chest overlay - COMPLETED
12. ✅ **Integration testing** - Full pickup/drop/transfer workflow for chests - COMPLETED
13. ✅ **Fix interaction detection** - Accurate sprite center positioning for scaled entities - COMPLETED

## Key Design Principles

- **Event-driven** - Systems communicate via EventBus
- **Component composition** - Flexible entity definitions
- **Reuse existing patterns** - Follow input/menu/scene patterns
- **Separation of concerns** - Logic, rendering, and input are separate systems
- **Extensible** - Easy to add new item types and container types
- **Clean entity management** - Separate world items from inventory items

## ✅ Implementation Complete - Summary

### 🎯 **Current Status: FULLY FUNCTIONAL**

The chest inventory system is now **100% complete** and provides a full-featured inventory management experience:

**✅ Core Features:**

- **Item Pickup**: Walk near items, press E to add to inventory
- **Chest Interaction**: Press E on chests to open inventory overlay
- **Item Transfer**: Navigate with arrows, press Enter to move items
- **Visual Feedback**: Clear slot highlighting and real-time inventory display
- **Accurate Detection**: Fixed interaction zones align with visual sprites

**✅ Input Controls:**

- **E Key**: Open chest / Pick up items
- **Arrow Keys**: Navigate inventory slots and switch between inventories
- **Enter**: Transfer selected item between player and chest
- **ESC**: Close chest interface

**✅ Technical Achievement:**

- **Event-Driven Architecture**: Clean separation of concerns with EventBus
- **Scalable Entity System**: Supports multiple container types and inventory sizes
- **Performance Optimized**: Removed debug output, cleaned unused variables
- **Mobile-Ready**: Full gamepad support alongside keyboard controls

### 🚀 Ready for Production

The inventory system is production-ready and provides the foundation for expanded gameplay mechanics.

## Future Enhancements

**Next Priority Suggestions:**

- **High Priority**: Proper entity destruction system for cleanup
- **Gameplay**: Item durability and repair mechanics
- **Advanced**: Crafting system using inventory items
- **UX**: Item categories and filtering
- **Polish**: Inventory sorting and auto-organize
- **Multiplayer**: Trading between players
- **RPG**: Item rarity and special properties
- **Persistence**: Save/load inventory state
