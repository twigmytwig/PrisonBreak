# Inventory System Implementation Plan

## Overview
Comprehensive inventory system allowing players to collect, manage, and interact with items through chests and containers. Built using the existing ECS architecture with event-driven communication.

## Current Status
✅ **ItemComponent** - Defines item properties (name, type, stackable, stack size)  
✅ **InventoryComponent** - Player inventory with max slots and item array  
✅ **PlayerTypeComponent.InventorySlots** - Different capacities per player type (Prisoner: 3, Cop: 4)  
✅ **InventorySystem.cs** - COMPLETED with all core methods implemented and integrated into GameplayScene
✅ **InventoryUIRenderSystem.cs** - COMPLETED UI rendering system for inventory display
✅ **Inventory Events** - ItemAddedEvent, ItemRemovedEvent, InventoryFullEvent added to EventSystem
✅ **Player Inventory Initialization** - CreatePlayer() method automatically initializes inventory based on player type
✅ **UI Integration** - Basic inventory UI with visual slot representation integrated into gameplay  

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

### Phase 2: Interaction System

#### 2.1 Create InteractableComponent
Add to `Components.cs`:
```csharp
public struct InteractableComponent
{
    public string InteractionType;    // "chest", "pickup", "door"
    public float InteractionRange;    // Distance for interaction
    public bool IsActive;            // Can be interacted with
    public string DisplayText;       // "Press E to open chest"
}

public struct ContainerComponent
{
    public int MaxItems;
    public Entity[] ContainedItems;
    public int ItemCount;
    public string ContainerType;     // "chest", "locker", "crate"
}
```

#### 2.2 Extend ComponentInputSystem
**Add interaction input detection:**
```csharp
// In CheckKeyboardInput method
if (Keyboard.GetState().IsKeyDown(Keys.E) && !_previousKeyboard.IsKeyDown(Keys.E))
{
    _eventBus.Send(new InteractionInputEvent(entity.Id));
}

// In CheckGamePadInput method  
if (gamePadState.IsButtonDown(Buttons.X) && !_previousGamePad.IsButtonDown(Buttons.X))
{
    _eventBus.Send(new InteractionInputEvent(entity.Id));
}
```

#### 2.3 Create InteractionSystem
**New system file: `InteractionSystem.cs`**
```csharp
public class InteractionSystem : IGameSystem
{
    // Check proximity between players and interactables
    // Handle interaction input events
    // Trigger appropriate interaction (chest UI, item pickup)
    // Send InteractionEvent when valid interaction occurs
}
```

**Key methods:**
- `CheckInteractionProximity()` - Find nearby interactables
- `HandleInteractionInput()` - Process E key presses
- `TriggerInteraction()` - Open chest UI or pickup items

### Phase 3: Chest/Container System

#### 3.1 Create Container Entities
**Entity factory methods in ComponentEntityManager:**
```csharp
public Entity CreateChest(Vector2 position, string[] itemTypes)
{
    var chest = CreateEntity();
    chest.AddComponent(new TransformComponent(position));
    chest.AddComponent(new SpriteComponent(chestSprite));
    chest.AddComponent(new InteractableComponent 
    { 
        InteractionType = "chest",
        InteractionRange = 64f,
        DisplayText = "Press E to open chest"
    });
    chest.AddComponent(new ContainerComponent 
    { 
        MaxItems = 10,
        ContainedItems = new Entity[10],
        ContainerType = "chest"
    });
    
    // Populate with initial items
    PopulateContainer(chest, itemTypes);
    return chest;
}

public Entity CreateItem(string itemType, Vector2 position)
{
    var item = CreateEntity();
    item.AddComponent(new TransformComponent(position));
    item.AddComponent(new SpriteComponent(GetItemSprite(itemType)));
    item.AddComponent(new ItemComponent
    {
        ItemName = GetItemName(itemType),
        ItemType = itemType,
        IsStackable = GetStackableProperty(itemType),
        StackSize = GetStackSize(itemType)
    });
    
    // Items on ground can be picked up
    item.AddComponent(new InteractableComponent
    {
        InteractionType = "pickup",
        InteractionRange = 32f,
        DisplayText = $"Press E to pick up {GetItemName(itemType)}"
    });
    
    return item;
}
```

### Phase 4: Inventory UI System

#### 4.1 Create InventoryUIScene
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
5. 🚧 **Create InteractionSystem** - Handle E key and proximity detection
6. 🚧 **Add InteractableComponent** - Mark chests as interactable
7. 🚧 **Create basic chest entities** - Static containers for testing
8. 🚧 **Build InventoryUIScene** - Modal overlay for management
9. 🚧 **Integration testing** - Full pickup/drop/transfer workflow

## Key Design Principles
- **Event-driven** - Systems communicate via EventBus
- **Component composition** - Flexible entity definitions
- **Reuse existing patterns** - Follow input/menu/scene patterns
- **Separation of concerns** - Logic, rendering, and input are separate systems
- **Extensible** - Easy to add new item types and container types

## Future Enhancements
- Item durability and repair
- Crafting system using inventory items
- Item categories and filtering
- Inventory sorting and auto-organize
- Trading between players
- Item rarity and special properties
- Save/load inventory state