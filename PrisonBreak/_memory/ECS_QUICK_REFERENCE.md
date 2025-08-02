# ECS Quick Reference Guide

## 🚀 Quick Start

### Creating Entities
```csharp
// Get the entity manager
var entityManager = new ComponentEntityManager();

// Create a player with specific type (NEW)
var player = entityManager.CreatePlayer(position, PlayerIndex.One, PlayerType.Prisoner);
// or
var player = entityManager.CreatePlayer(position, PlayerIndex.One, PlayerType.Cop);

// Create a cop
var cop = entityManager.CreateCop(position, AIBehavior.Patrol);

// Create custom entity
var entity = entityManager.CreateEntity();
entity.AddComponent(new TransformComponent(position));
entity.AddComponent(new SpriteComponent(sprite));
```

### Querying Entities
```csharp
// Get all moving entities
var moving = entityManager.GetEntitiesWith<MovementComponent>();

// Get all renderable entities
var renderable = entityManager.GetEntitiesWith<TransformComponent, SpriteComponent>();

// Get all players with input
var players = entityManager.GetEntitiesWith<PlayerTag, PlayerInputComponent>();

// Get players by type (NEW)
var prisoners = entityManager.GetEntitiesWith<PlayerTypeComponent>()
    .Where(e => e.GetComponent<PlayerTypeComponent>().Type == PlayerType.Prisoner);

// Get menu items (NEW)
var menuItems = entityManager.GetEntitiesWith<MenuItemComponent, TransformComponent>();

// Get networked entities (MULTIPLAYER)
var networkEntities = entityManager.GetEntitiesWith<NetworkComponent>();

// Get entities that need transform sync
var transformSyncEntities = entityManager.GetEntitiesWith<NetworkComponent, TransformComponent>()
    .Where(e => e.GetComponent<NetworkComponent>().SyncTransform);

// Get entities owned by specific player
var playerOwnedEntities = entityManager.GetEntitiesWith<NetworkComponent>()
    .Where(e => e.GetComponent<NetworkComponent>().OwnerId == playerId);
```

### Modifying Components
```csharp
// Get component reference for direct modification
ref var transform = ref entity.GetComponent<TransformComponent>();
transform.Position += velocity;

// Check if entity has component
if (entity.HasComponent<CollisionComponent>())
{
    // Do collision logic
}
```

## 📦 Component Reference

### Core Components
| Component | Purpose | Key Fields |
|-----------|---------|------------|
| `TransformComponent` | Position/scale | `Vector2 Position`, `Vector2 Scale`, `float Rotation` |
| `SpriteComponent` | Visual | `AnimatedSprite Sprite`, `bool Visible`, `Color Tint` |
| `MovementComponent` | Physics | `Vector2 Velocity`, `float MaxSpeed`, `float Friction` |
| `CollisionComponent` | Collision | `RectangleCollider Collider`, `bool IsSolid` |

### Input Components
| Component | Purpose | Key Fields |
|-----------|---------|------------|
| `PlayerInputComponent` | Player control | `PlayerIndex PlayerIndex` |

### AI Components
| Component | Purpose | Key Fields |
|-----------|---------|------------|
| `AIComponent` | AI behavior | `AIBehavior Behavior`, state data |

### Tag Components
| Component | Purpose | Key Fields |
|-----------|---------|------------|
| `PlayerTag` | Mark as player | `int PlayerId` |
| `CopTag` | Mark as cop | `int CopId` |
| `DebugComponent` | Debug rendering | Debug flags |

### Game Features
| Component | Purpose | Key Fields |
|-----------|---------|------------|
| `PlayerTypeComponent` | Player classification | `PlayerType Type`, `float SpeedMultiplier`, `string AnimationName`, `int InventorySlots` |
| `InventoryComponent` | Item storage | `int MaxSlots`, `Entity[] Items`, `int ItemCount` |
| `ItemComponent` | Item properties | `string ItemName`, `string ItemType`, `bool IsStackable`, `int StackSize` |

### Networking Components (Multiplayer)
| Component | Purpose | Key Fields | Location |
|-----------|---------|------------|----------|
| `NetworkComponent` | Network synchronization | `int NetworkId`, `NetworkConfig.NetworkAuthority Authority`, `bool SyncTransform`, `bool SyncMovement`, `bool SyncInventory`, `int OwnerId` | `PrisonBreak.ECS` (Components.cs) |

### Networking Systems (Multiplayer)
| System | Purpose | Update Rate | Authority |
|--------|---------|-------------|-----------|
| `NetworkManager` | Core networking coordination | Event-driven | Host/Client |
| `NetworkSyncSystem` | Player position synchronization | 20Hz | Client (for own player) |
| `NetworkAISystem` | AI cop behavior synchronization | 10Hz | Host only |

### Network Messages
| Message Type | Purpose | Authority | Content |
|--------------|---------|-----------|---------|
| `TransformMessage` | Position/rotation sync | Client → Host → Clients | Position, Rotation, Scale |
| `AIStateMessage` | AI behavior sync | Host → Clients | Behavior, PatrolDirection, StateTimer, TargetPosition |
| `EntitySpawnMessage` | Entity creation sync | Host → Clients | EntityType, Position, NetworkID, RoomBounds |
| `CollisionMessage` | Collision result sync | Host → Clients | PlayerID, CopID, NewPosition, NewPatrolDirection |

### Menu/UI Components (NEW)
| Component | Purpose | Key Fields |
|-----------|---------|------------|
| `MenuItemComponent` | Menu buttons/UI | `bool IsSelected`, `int Width`, `int Height`, `Color BackgroundColor` |
| `TextComponent` | Text rendering | `string Text`, `Color Color`, `SpriteFont Font`, `TextAlignment Alignment` |

## ⚙️ System Reference

### System Execution Order

#### Gameplay Scene
```
1. ComponentInputSystem    - Process player input → events
2. ComponentMovementSystem - Apply movement from events + tile collision detection  
3. ComponentCollisionSystem - Detect/resolve entity collisions (player-cop)
4. InventorySystem         - Manage player inventories and item interactions
5. NetworkManager          - Handle multiplayer networking (if enabled)
6. NetworkSyncSystem       - Sync player positions (20Hz) 
7. NetworkAISystem         - Sync AI behavior and positions (10Hz)
8. ComponentRenderSystem   - Draw everything
```

#### Start Menu Scene (NEW)
```
1. MenuInputSystem - Process menu navigation input → events
2. MenuRenderSystem - Draw menu items and text
```

### Collision System Architecture

This project uses a **dual collision system**:

#### **Tile-Based Collision (Environment)**
- **Handled by**: `ComponentMovementSystem` 
- **Purpose**: Wall/environment collision detection
- **Method**: 2D boolean collision map for O(1) tile lookups
- **Features**: Smooth sliding, predictive collision, stuck recovery

#### **Entity-Based Collision (Gameplay)**  
- **Handled by**: `ComponentCollisionSystem`
- **Purpose**: Game logic collisions (player-cop, player-pickup)
- **Method**: Entity queries and rectangle intersection
- **Features**: Event-driven collision responses

### Key System Methods
```csharp
// Input System
public void Update(GameTime gameTime)
{
    // Reads input for entities with PlayerInputComponent
    // Sends PlayerInputEvent for movement
}

// Movement System  
public void Update(GameTime gameTime)
{
    // Listens to PlayerInputEvent
    // Updates MovementComponent velocity
    // Applies tile-based collision detection for players
    // Applies physics to all moving entities
}

// Tile-Based Collision Methods (in MovementSystem)
public void SetCollisionMap(Tilemap tilemap, Vector2 offset)
{
    // Creates 2D boolean collision map from tilemap
    // Maps tile IDs to solid/passable
}

private Vector2 GetSafePosition(Entity entity, Vector2 from, Vector2 to)
{
    // Predictive collision detection
    // Returns safe movement position without wall penetration
}

// Collision System
public void Update(GameTime gameTime)
{
    // Checks boundary collisions
    // Checks entity-to-entity collisions (player-cop)
    // Sends collision events
}

// Render System
public void Draw(SpriteBatch spriteBatch)
{
    // Draws all entities with Transform + Sprite
    // Handles depth sorting
    // Draws debug info
}
```

## 🎯 Common Patterns

### Entity Factory Pattern
```csharp
public Entity CreateProjectile(Vector2 position, Vector2 velocity)
{
    var entity = CreateEntity();
    entity.AddComponent(new TransformComponent(position, Vector2.One));
    entity.AddComponent(new SpriteComponent(projectileSprite));
    entity.AddComponent(new MovementComponent { Velocity = velocity });
    // No collision = passes through walls
    return entity;
}
```

### Component Query Pattern
```csharp
public void UpdateHealthBars()
{
    // Only process entities that have both health and position
    var entities = entityManager.GetEntitiesWith<HealthComponent, TransformComponent>();
    
    foreach (var entity in entities)
    {
        var health = entity.GetComponent<HealthComponent>();
        var transform = entity.GetComponent<TransformComponent>();
        
        // Draw health bar above entity
        DrawHealthBar(transform.Position, health.Percentage);
    }
}
```

### Event Communication Pattern
```csharp
// System A sends event
_eventBus.Send(new PlayerCopCollisionEvent 
{ 
    PlayerId = player.Id, 
    CopId = cop.Id 
});

// System B handles event
_eventBus.Subscribe<PlayerCopCollisionEvent>(OnPlayerCopCollision);

private void OnPlayerCopCollision(PlayerCopCollisionEvent evt)
{
    // Handle the collision
    var player = entityManager.GetEntity(evt.PlayerId);
    // Game over logic
}
```

### Dynamic Component Modification
```csharp
// Make entity temporarily invulnerable
entity.AddComponent(new InvulnerabilityComponent { Duration = 3.0f });

// Make entity start glowing
ref var sprite = ref entity.GetComponent<SpriteComponent>();
sprite.Tint = Color.Yellow;

// Make pickup start moving when touched
entity.AddComponent(new MovementComponent(100f));
entity.AddComponent(new AIComponent(AIBehavior.Flee));
```

### Inventory Management Pattern
```csharp
// Create an item
var item = entityManager.CreateEntity();
item.AddComponent(new ItemComponent 
{
    ItemName = "Key",
    ItemType = "tool",
    IsStackable = false,
    StackSize = 1
});

// Add item to player inventory
var inventorySystem = systemManager.GetSystem<InventorySystem>();
bool added = inventorySystem.TryAddItem(playerEntity, item);

if (!added)
{
    // Handle inventory full - maybe drop on ground
    Console.WriteLine("Inventory is full!");
}

// Remove item from specific slot
bool removed = inventorySystem.TryRemoveItem(playerEntity, slotIndex: 0);

// Check what's in a slot
Entity itemInSlot = inventorySystem.GetItemAtSlot(playerEntity, slotIndex: 1);
if (itemInSlot != null && itemInSlot.HasComponent<ItemComponent>())
{
    var itemData = itemInSlot.GetComponent<ItemComponent>();
    Console.WriteLine($"Slot 1 contains: {itemData.ItemName}");
}
```

### Networking Pattern (Multiplayer) ✅ IMPLEMENTED
```csharp
using PrisonBreak.Managers;
using PrisonBreak.Multiplayer.Core;
using PrisonBreak.Core.Networking;

// Create networked player entity
var player = entityManager.CreatePlayer(position, PlayerIndex.One, PlayerType.Prisoner);
player.AddComponent(new NetworkComponent(networkId: 1, NetworkConfig.NetworkAuthority.Client, ownerId: playerId));

// Create networked AI cop (host authority)
var aiCop = entityManager.CreateCop(position, AIBehavior.Patrol);
aiCop.AddComponent(new CopTag(1001)); // Deterministic network ID

// Send entity spawn to clients (host only)
if (networkManager.CurrentGameMode == NetworkConfig.GameMode.LocalHost)
{
    var spawnMessage = new EntitySpawnMessage(1001, "cop", position, roomBounds, "Patrol");
    networkManager.SendEntitySpawn(spawnMessage);
}

// AI Synchronization (✅ IMPLEMENTED):
// - NetworkAISystem syncs AI behavior at 10Hz from host to clients
// - AIStateMessage contains behavior, patrol direction, targets
// - Only AI cops synced (player cops excluded via filtering)
// - Collision events networked with authoritative result broadcasting

// Collision Networking (✅ IMPLEMENTED):
// - Host processes all collisions authoritatively
// - CollisionMessage broadcasts teleportation results
// - Clients apply collision results from host
// - Prevents collision desync between clients
```

## 🔧 System Manager Usage

### Setup
```csharp
var systemManager = new SystemManager();

// Add systems in correct order
systemManager.AddSystem(new ComponentInputSystem(entityManager, eventBus));
systemManager.AddSystem(new ComponentMovementSystem(entityManager, eventBus));
systemManager.AddSystem(new ComponentCollisionSystem(entityManager, eventBus));
systemManager.AddSystem(new InventorySystem(entityManager, eventBus));
systemManager.AddSystem(new NetworkManager(eventBus, entityManager)); // ✅ IMPLEMENTED - From PrisonBreak.Managers
systemManager.AddSystem(new ComponentRenderSystem(entityManager));

// Initialize all systems
systemManager.Initialize();
```

### Game Loop
```csharp
protected override void Update(GameTime gameTime)
{
    systemManager.Update(gameTime); // Runs all systems in order
}

protected override void Draw(GameTime gameTime)
{
    _spriteBatch.Begin();
    systemManager.Draw(_spriteBatch); // Handles all rendering
    _spriteBatch.End();
}
```

## 🐛 Debugging Tips

### Entity Inspector
```csharp
// Print all components on entity
foreach (var componentType in entity.GetComponentTypes())
{
    Console.WriteLine($"  - {componentType.Name}");
}
```

### Component Counting
```csharp
var total = entityManager.GetEntityCount();
var renderable = entityManager.GetEntitiesWith<SpriteComponent>().Count();
var moving = entityManager.GetEntitiesWith<MovementComponent>().Count();

Console.WriteLine($"Total: {total}, Renderable: {renderable}, Moving: {moving}");
```

### Performance Profiling
```csharp
var stopwatch = Stopwatch.StartNew();
inputSystem.Update(gameTime);
Console.WriteLine($"Input: {stopwatch.ElapsedMilliseconds}ms");

stopwatch.Restart();
movementSystem.Update(gameTime);
Console.WriteLine($"Movement: {stopwatch.ElapsedMilliseconds}ms");
```

## ⚡ Performance Tips

1. **Cache component queries** when possible
2. **Use `ref` returns** to avoid copying structs
3. **Batch similar operations** in systems
4. **Remove unused components** to keep queries fast
5. **Profile individual systems** to find bottlenecks

## 🧱 Tile-Based Collision Setup

### Adding Collision Map to Movement System
```csharp
// In Game1.cs InitializeGame()
_movementSystem.SetCollisionMap(_tilemap, Vector2.Zero);
```

### Defining Solid Tiles
```csharp
// In ComponentMovementSystem.SetCollisionMap()
int[] solidTileIds = { 2, 3, 4, 5 }; // Add new solid tile IDs
// 02 = prison bars, 03 = walls, 04 = tables, 05 = doors
```

### Tilemap Configuration Example
```xml
<!-- In tilemap-definition.xml -->
<Tiles>
    00 00 03 03 03 00 00  <!-- 03 = solid walls -->
    00 00 03 02 03 00 00  <!-- 02 = prison bars -->
    00 00 04 00 04 00 00  <!-- 04 = tables -->
    00 00 00 00 00 00 00  <!-- 00 = empty space -->
</Tiles>
```

### Performance Benefits
- **O(1) collision detection** instead of O(n) entity checks
- **No individual wall entities** needed - reduces memory usage
- **Smooth wall sliding** - natural movement along connected walls
- **Scalable** - performance doesn't degrade with wall count

## 🎮 Entity Recipes

### Fast Moving Player
```csharp
var speedster = entityManager.CreateEntity();
speedster.AddComponent(new TransformComponent(position));
speedster.AddComponent(new SpriteComponent(sprite));
speedster.AddComponent(new MovementComponent(200f)); // Fast!
speedster.AddComponent(new PlayerInputComponent(PlayerIndex.One));
speedster.AddComponent(new PlayerTag(speedster.Id));
// Wall collision handled automatically by tile-based system
```

### Immovable Wall (Legacy - Use Tile System Instead)
```csharp
// OLD APPROACH - No longer needed
// Use tile-based collision system instead for better performance
```

### Invisible Trigger Zone
```csharp
var trigger = entityManager.CreateEntity();
trigger.AddComponent(new TransformComponent(position));
// No SpriteComponent = invisible
trigger.AddComponent(new CollisionComponent(collider, isSolid: false));
trigger.AddComponent(new TriggerComponent("exit_zone"));
```

This ECS system provides infinite flexibility while maintaining high performance with efficient tile-based collision!