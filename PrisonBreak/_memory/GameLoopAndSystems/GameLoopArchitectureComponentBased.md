# Component-Based Game Loop Architecture

## Overview

The PrisonBreak game has been successfully converted from an inheritance-based entity system to a pure **Entity Component System (ECS)** architecture. This document explains the new component-based approach, how it works, and why it's superior for complex and multiplayer games.

## What Changed: Before vs After

### Before (Inheritance-Based)
```
GameEntity (base class)
├── Player : GameEntity
└── Enemy : GameEntity
    └── Cop : Enemy
```

**Problems:**
- Rigid hierarchy
- All entities had ALL functionality (position, sprite, collision)
- Hard to create entities with unique combinations of behaviors
- Difficult to optimize (had to process ALL entities for ANY operation)

### After (Component-Based)
```
Entity (just an ID + component storage)
├── Components (pure data):
│   ├── TransformComponent (position, rotation, scale)
│   ├── SpriteComponent (visual representation)
│   ├── MovementComponent (velocity, physics)
│   ├── CollisionComponent (bounds, collision data)
│   ├── PlayerInputComponent (input handling)
│   ├── AIComponent (autonomous behavior)
│   └── DebugComponent (debug visualization)
└── Systems (pure logic):
    ├── ComponentInputSystem
    ├── ComponentMovementSystem
    ├── ComponentCollisionSystem
    └── ComponentRenderSystem
```

**Benefits:**
- **Flexible:** Mix any components to create any entity type
- **Performant:** Only process entities that have relevant components
- **Scalable:** Easy to add new components and systems
- **Multiplayer-ready:** Pure data components are easy to synchronize

## Core Architecture

### 1. Entity Class (`ECS/Entity.cs`)

The Entity is now just a container for components:

```csharp
public class Entity
{
    public int Id { get; }
    private Dictionary<Type, object> _components = new();
    
    public T AddComponent<T>(T component);
    public ref T GetComponent<T>() where T : struct;
    public bool HasComponent<T>();
    public void RemoveComponent<T>();
}
```

**Key Features:**
- **Lightweight:** Only stores an ID and component dictionary
- **Type-safe:** Generic methods ensure component type safety
- **Fast access:** Dictionary lookup by component type
- **Ref returns:** `ref T GetComponent<T>()` allows direct modification without copying

### 2. Pure Data Components (`ECS/Components.cs`)

All components are **pure data structures** with no logic:

```csharp
// Transform - position, rotation, scale
public struct TransformComponent
{
    public Vector2 Position;
    public float Rotation;
    public Vector2 Scale;
}

// Visual representation
public struct SpriteComponent
{
    public AnimatedSprite Sprite;
    public bool Visible;
    public Color Tint;
}

// Movement and physics
public struct MovementComponent
{
    public Vector2 Velocity;
    public float MaxSpeed;
    public float Acceleration;
    public float Friction;
}

// Collision detection
public struct CollisionComponent
{
    public RectangleCollider Collider;
    public bool IsSolid;
    public string Layer;
}
```

**Why Structs:**
- **Performance:** Value types stored directly, better cache locality
- **Immutability:** Harder to accidentally share references
- **Serialization:** Easy to serialize for networking/save games
- **Memory:** More compact than classes

### 3. Tag Components

Special components that just mark entity types:

```csharp
public struct PlayerTag { public int PlayerId; }
public struct CopTag { public int CopId; }
```

**Purpose:**
- **Query filtering:** Find all players or all cops
- **System targeting:** Systems can target specific entity types
- **Event handling:** Send events to specific entity categories

### 4. Event System (`ECS/EventSystem.cs`)

Components **never communicate directly**. Instead, they use events:

```csharp
public class EventBus
{
    public void Subscribe<T>(Action<T> handler);
    public void Send<T>(T eventData);
    public void Unsubscribe<T>(Action<T> handler);
}

// Game events
public struct PlayerInputEvent
{
    public int EntityId;
    public Vector2 MovementDirection;
    public bool SpeedBoost;
}

public struct PlayerCopCollisionEvent
{
    public int PlayerId;
    public int CopId;
    public Vector2 CollisionPosition;
}
```

**Benefits:**
- **Decoupling:** Systems don't know about each other
- **Extensibility:** Easy to add new event handlers
- **Debugging:** Can log all events for debugging
- **Networking:** Events can be synchronized across network

## Component Systems

### 1. ComponentInputSystem (`ECS/ComponentInputSystem.cs`)

**Responsibility:** Convert player input to events

```csharp
public void Update(GameTime gameTime)
{
    var playerEntities = _entityManager.GetEntitiesWith<PlayerInputComponent, TransformComponent>();
    
    foreach (var entity in playerEntities)
    {
        // Read input for this player
        var movementDirection = ReadInput();
        
        // Send input event (instead of directly modifying entity)
        _eventBus.Send(new PlayerInputEvent(entity.Id, movementDirection, speedBoost));
    }
}
```

**Key Features:**
- **Query-based:** Only processes entities with PlayerInputComponent
- **Event-driven:** Sends events instead of direct manipulation
- **Multi-player ready:** Each player has their own PlayerInputComponent

### 2. ComponentMovementSystem (`ECS/ComponentMovementSystem.cs`)

**Responsibility:** Handle movement for ALL entities with MovementComponent

```csharp
public void Update(GameTime gameTime)
{
    // Handle player input events
    var inputEvent = _eventBus.GetPlayerInputEvents();
    foreach (var evt in inputEvent)
    {
        var entity = _entityManager.GetEntity(evt.EntityId);
        ref var movement = ref entity.GetComponent<MovementComponent>();
        movement.Velocity = evt.MovementDirection * speed;
    }
    
    // Apply movement to ALL moving entities (players AND AI)
    var movingEntities = _entityManager.GetEntitiesWith<MovementComponent, TransformComponent>();
    foreach (var entity in movingEntities)
    {
        ref var movement = ref entity.GetComponent<MovementComponent>();
        ref var transform = ref entity.GetComponent<TransformComponent>();
        
        transform.Position += movement.Velocity * deltaTime;
        movement.Velocity *= movement.Friction;
    }
}
```

**Key Features:**
- **Unified processing:** Same system handles player AND AI movement
- **Event subscription:** Listens to PlayerInputEvent for player movement
- **Physics integration:** Applies velocity, friction, etc.

### 3. ComponentCollisionSystem (`ECS/ComponentCollisionSystem.cs`)

**Responsibility:** Detect and respond to all collisions

```csharp
public void Update(GameTime gameTime)
{
    // Boundary collisions
    var boundedEntities = _entityManager.GetEntitiesWith<BoundsConstraintComponent, TransformComponent, CollisionComponent>();
    foreach (var entity in boundedEntities)
    {
        // Check and resolve boundary collisions
        // Send BoundaryCollisionEvent if collision occurred
    }
    
    // Entity-to-entity collisions
    var players = _entityManager.GetEntitiesWith<PlayerTag, CollisionComponent>();
    var cops = _entityManager.GetEntitiesWith<CopTag, CollisionComponent>();
    
    foreach (var player in players)
    {
        foreach (var cop in cops)
        {
            if (CollisionDetected(player, cop))
            {
                _eventBus.Send(new PlayerCopCollisionEvent(player.Id, cop.Id, collisionPoint));
            }
        }
    }
}
```

**Key Features:**
- **Separation of concerns:** Only handles collision detection/response
- **Event-driven responses:** Sends collision events instead of direct handling
- **Query optimization:** Only checks entities that CAN collide

### 4. ComponentRenderSystem (`ECS/ComponentRenderSystem.cs`)

**Responsibility:** Draw all visual entities

```csharp
public void Draw(SpriteBatch spriteBatch)
{
    // Get all entities that can be rendered
    var renderableEntities = _entityManager.GetEntitiesWith<TransformComponent, SpriteComponent>()
        .Where(e => e.GetComponent<SpriteComponent>().Visible)
        .OrderBy(e => e.GetComponent<TransformComponent>().Position.Y); // Depth sorting
    
    foreach (var entity in renderableEntities)
    {
        var transform = entity.GetComponent<TransformComponent>();
        var sprite = entity.GetComponent<SpriteComponent>();
        
        sprite.Sprite.Draw(spriteBatch, transform.Position);
    }
    
    // Draw debug information
    var debugEntities = _entityManager.GetEntitiesWith<DebugComponent, CollisionComponent>();
    foreach (var entity in debugEntities)
    {
        DrawCollisionBounds(entity);
    }
}
```

**Key Features:**
- **Query-based rendering:** Only draws entities with both Transform and Sprite
- **Automatic sorting:** Depth sorting by Y position
- **Debug support:** Conditional debug drawing

## SystemManager (`Managers/SystemManager.cs`)

The SystemManager coordinates all ECS systems and provides a unified lifecycle:

```csharp
public class SystemManager
{
    private readonly List<IGameSystem> _systems;
    private bool _initialized;

    public void AddSystem(IGameSystem system);
    public void Initialize();
    public void Update(GameTime gameTime);
    public void Draw(SpriteBatch spriteBatch);
    public void Shutdown();
}
```

**Key Features:**
- **Unified lifecycle:** All systems follow the same Initialize → Update → Draw → Shutdown pattern
- **Automatic initialization:** Systems added after initialization are automatically initialized
- **Ordered execution:** Systems execute in the order they were added
- **Clean shutdown:** Proper cleanup of all systems and resources

**System Interface (`Systems/IGameSystem.cs`):**
```csharp
public interface IGameSystem
{
    void Initialize();
    void Update(GameTime gameTime);
    void Draw(SpriteBatch spriteBatch);
    void Shutdown();
}
```

**Usage Pattern:**
```csharp
// Game setup
var systemManager = new SystemManager();
systemManager.AddSystem(new ComponentInputSystem(entityManager, eventBus));
systemManager.AddSystem(new ComponentMovementSystem(entityManager, eventBus));
systemManager.AddSystem(new ComponentCollisionSystem(entityManager, eventBus));
systemManager.AddSystem(new ComponentRenderSystem(entityManager));
systemManager.Initialize();

// Game loop
public override void Update(GameTime gameTime)
{
    systemManager.Update(gameTime); // Runs all systems in order
}

public override void Draw(GameTime gameTime)
{
    systemManager.Draw(spriteBatch); // Handles all rendering
}
```

## ComponentEntityManager (`ECS/ComponentEntityManager.cs`)

The new entity manager handles component-based entities:

```csharp
public class ComponentEntityManager
{
    // Entity creation
    public Entity CreateEntity();
    public Entity CreatePlayer(Vector2 position, PlayerIndex playerIndex);
    public Entity CreateCop(Vector2 position, AIBehavior behavior);
    
    // Entity queries (THE MAGIC!)
    public IEnumerable<Entity> GetEntitiesWith<T1>();
    public IEnumerable<Entity> GetEntitiesWith<T1, T2>();
    public IEnumerable<Entity> GetEntitiesWith<T1, T2, T3>();
    
    // Utility methods
    public Entity FindFirstPlayerEntity();
    public IEnumerable<Entity> FindAllCopEntities();
}
```

**Factory Methods:**
```csharp
public Entity CreatePlayer(Vector2 position, PlayerIndex playerIndex)
{
    var entity = CreateEntity();
    
    entity.AddComponent(new TransformComponent(position, EntityConfig.Player.Scale));
    entity.AddComponent(new SpriteComponent(playerSprite));
    entity.AddComponent(new MovementComponent(GameConfig.BaseMovementSpeed));
    entity.AddComponent(new CollisionComponent(collider));
    entity.AddComponent(new PlayerInputComponent(playerIndex));
    entity.AddComponent(new PlayerTag(entity.Id));
    
    return entity;
}
```

**Query System (The Magic!):**
```csharp
// Get all entities that can move
var movingEntities = entityManager.GetEntitiesWith<MovementComponent>();

// Get all entities that can be rendered
var renderableEntities = entityManager.GetEntitiesWith<TransformComponent, SpriteComponent>();

// Get all players with input
var inputPlayers = entityManager.GetEntitiesWith<PlayerTag, PlayerInputComponent>();
```

## System Execution Order

The SystemManager ensures systems run in the **critical** correct order:

```
SystemManager.Update(gameTime):
1. ComponentInputSystem    -> Captures input, sends events
2. ComponentMovementSystem -> Processes movement events, applies physics  
3. ComponentCollisionSystem -> Detects collisions, sends collision events

SystemManager.Draw(spriteBatch):
4. ComponentRenderSystem   -> Draws everything
```

**Why This Order:**
1. **Input first:** Must capture user commands before anything else
2. **Movement second:** Apply movement based on input and AI
3. **Collision third:** Resolve any problems caused by movement
4. **Render last:** Draw the final state after all logic is complete

**SystemManager Benefits:**
- **Guaranteed order:** Systems always execute in the correct sequence
- **Centralized control:** Single point to manage system lifecycle
- **Error isolation:** If one system fails, others can continue
- **Performance tracking:** Easy to profile individual system performance

## Creating New Entity Types

With components, creating new entity types is **extremely easy**:

### Moving Pickup
```csharp
var pickup = entityManager.CreateEntity();
pickup.AddComponent(new TransformComponent(position));
pickup.AddComponent(new SpriteComponent(pickupSprite));
pickup.AddComponent(new MovementComponent(25f)); // Slow movement
pickup.AddComponent(new AIComponent(AIBehavior.Wander));
// No collision component = passes through walls!
// No input component = controlled by AI
```

### Stationary Guard Tower
```csharp
var tower = entityManager.CreateEntity();
tower.AddComponent(new TransformComponent(position));
tower.AddComponent(new SpriteComponent(towerSprite));
tower.AddComponent(new CollisionComponent(collider));
// No movement component = can't move
// No AI or input = just sits there
```

### Player-Controlled Cop (Multiplayer)
```csharp
var playerCop = entityManager.CreateEntity();
playerCop.AddComponent(new TransformComponent(position));
playerCop.AddComponent(new SpriteComponent(copSprite));
playerCop.AddComponent(new MovementComponent(100f));
playerCop.AddComponent(new CollisionComponent(collider));
playerCop.AddComponent(new PlayerInputComponent(PlayerIndex.Two)); // Player 2 controls
playerCop.AddComponent(new CopTag(playerCop.Id));
// Has input component = player controlled
// Has cop tag = behaves like cop in collisions
```

### Invisible Ghost Enemy
```csharp
var ghost = entityManager.CreateEntity();
ghost.AddComponent(new TransformComponent(position));
// No sprite component = invisible!
ghost.AddComponent(new MovementComponent(50f));
// No collision component = passes through everything!
ghost.AddComponent(new AIComponent(AIBehavior.Chase));
ghost.AddComponent(new CopTag(ghost.Id));
```

## Performance Benefits

### Memory Layout
```csharp
// Old way - each entity has everything (waste memory)
class Player { Vector2 pos; Sprite sprite; Collision collision; AI ai; /* etc */ }
class Pickup { Vector2 pos; Sprite sprite; Collision collision; AI ai; /* etc */ }

// New way - entities only have what they need
Entity player = [TransformComponent, SpriteComponent, MovementComponent, PlayerInputComponent]
Entity pickup = [TransformComponent, SpriteComponent, MovementComponent, AIComponent]
Entity tower = [TransformComponent, SpriteComponent, CollisionComponent]
```

### Query Performance
```csharp
// Old way - check every entity for every operation
foreach (var entity in allEntities)
{
    if (entity is MovingEntity movingEntity)
    {
        movingEntity.Update(); // Check type every time
    }
}

// New way - only process relevant entities
var movingEntities = entityManager.GetEntitiesWith<MovementComponent>();
foreach (var entity in movingEntities) // Already filtered!
{
    ref var movement = ref entity.GetComponent<MovementComponent>();
    // Direct access, no type checking
}
```

### Cache Friendliness
```csharp
// Components are stored contiguously in memory
// When you iterate through MovementComponents, you get:
// [MovementComp1][MovementComp2][MovementComp3][MovementComp4]...
// Instead of jumping around memory for each entity
```

## Multiplayer Advantages

### Easy Synchronization
```csharp
// Serialize entity state
public NetworkEntityData SerializeEntity(Entity entity)
{
    return new NetworkEntityData
    {
        EntityId = entity.Id,
        Transform = entity.GetComponent<TransformComponent>(),
        Movement = entity.GetComponent<MovementComponent>(),
        // Only send components that matter
    };
}

// Deserialize entity state
public void DeserializeEntity(NetworkEntityData data)
{
    var entity = entityManager.GetEntity(data.EntityId);
    entity.GetComponent<TransformComponent>() = data.Transform;
    entity.GetComponent<MovementComponent>() = data.Movement;
}
```

### Deterministic Simulation
```csharp
// Pure functions = same input always produces same output
public static Vector2 ApplyMovement(Vector2 position, Vector2 velocity, float deltaTime)
{
    return position + velocity * deltaTime; // Always deterministic
}

// Easy to replay/rollback for lag compensation
```

### Client-Server Architecture
```csharp
// Server: Has ALL components and systems
var serverEntity = CreateEntity();
serverEntity.AddComponent(new TransformComponent());
serverEntity.AddComponent(new MovementComponent());
serverEntity.AddComponent(new AIComponent()); // Server-side AI
serverEntity.AddComponent(new HealthComponent()); // Server-side health

// Client: Only has components needed for display
var clientEntity = CreateEntity();
clientEntity.AddComponent(new TransformComponent()); // Synced from server
clientEntity.AddComponent(new SpriteComponent()); // Client-side only
// No AI or health - server authoritative
```

## Advanced Features

### Component Dependencies
```csharp
// Some components depend on others
public struct CollisionComponent
{
    // Requires TransformComponent to work
    public void UpdateColliderPosition(TransformComponent transform)
    {
        // Update collision bounds based on position
    }
}

// Systems can enforce dependencies
var collidableEntities = entityManager.GetEntitiesWith<CollisionComponent, TransformComponent>();
// Will only return entities that have BOTH components
```

### Dynamic Component Addition/Removal
```csharp
// Make an entity invulnerable temporarily
entity.AddComponent(new InvulnerabilityComponent { Duration = 3.0f });

// Remove when time expires
if (invulnerable.Duration <= 0)
{
    entity.RemoveComponent<InvulnerabilityComponent>();
}

// Make a pickup start moving when touched
pickup.AddComponent(new MovementComponent(200f));
pickup.AddComponent(new AIComponent(AIBehavior.Flee));
```

### Component Composition Patterns
```csharp
// Health system that works with ANY entity
entity.AddComponent(new HealthComponent { MaxHealth = 100, CurrentHealth = 100 });

// Damage system
var damageableEntities = entityManager.GetEntitiesWith<HealthComponent>();
foreach (var entity in damageableEntities)
{
    ref var health = ref entity.GetComponent<HealthComponent>();
    if (health.CurrentHealth <= 0)
    {
        // Could remove sprite to make invisible
        entity.RemoveComponent<SpriteComponent>();
        // Could add death animation
        entity.AddComponent(new DeathAnimationComponent());
        // Could make non-solid
        entity.RemoveComponent<CollisionComponent>();
    }
}
```

## Debugging and Tools

### Entity Inspector
```csharp
public void DebugEntity(Entity entity)
{
    Console.WriteLine($"Entity {entity.Id}:");
    foreach (var componentType in entity.GetComponentTypes())
    {
        Console.WriteLine($"  - {componentType.Name}");
    }
}
```

### System Performance Profiling
```csharp
public void Update(GameTime gameTime)
{
    var stopwatch = Stopwatch.StartNew();
    
    _inputSystem.Update(gameTime);
    LogTime("Input", stopwatch.ElapsedMilliseconds);
    
    _movementSystem.Update(gameTime);
    LogTime("Movement", stopwatch.ElapsedMilliseconds);
    
    _collisionSystem.Update(gameTime);
    LogTime("Collision", stopwatch.ElapsedMilliseconds);
}
```

### Component Statistics
```csharp
public void LogEntityStats()
{
    var totalEntities = entityManager.GetEntityCount();
    var renderableEntities = entityManager.GetEntitiesWith<SpriteComponent>().Count();
    var movingEntities = entityManager.GetEntitiesWith<MovementComponent>().Count();
    var collidingEntities = entityManager.GetEntitiesWith<CollisionComponent>().Count();
    
    Console.WriteLine($"Total: {totalEntities}, Renderable: {renderableEntities}, Moving: {movingEntities}, Colliding: {collidingEntities}");
}
```

## Migration Notes

### Switching Between Architectures

The project now supports both architectures:

```csharp
// Use old inheritance-based architecture
using var game = new PrisonBreak.Game1();

// Use new component-based architecture  
using var game = new PrisonBreak.ComponentGame1();
```

### Performance Comparison

| Feature | Inheritance | Components |
|---------|-------------|------------|
| **Memory Usage** | Higher (all entities have all fields) | Lower (only needed components) |
| **Update Performance** | Slower (check all entities) | Faster (query only relevant) |
| **Cache Performance** | Poor (scattered memory) | Good (contiguous components) |
| **Flexibility** | Low (fixed hierarchy) | High (mix any components) |
| **Code Complexity** | Simple (familiar OOP) | Medium (new concepts) |
| **Scalability** | Poor (becomes unwieldy) | Excellent (linear scaling) |

## Best Practices

### Component Design
1. **Keep components as pure data** - no methods, just fields
2. **Use structs for components** - better performance and semantics
3. **Single responsibility** - each component does one thing
4. **Composition over inheritance** - build entities by combining components

### System Design
1. **Systems own the logic** - components are just data
2. **Use events for communication** - don't access other systems directly
3. **Query-based processing** - only process entities you need
4. **Deterministic order** - system execution order matters

### Performance
1. **Batch similar operations** - process all movement together, all rendering together
2. **Minimize component queries** - cache results when possible
3. **Profile system performance** - measure which systems are slow
4. **Use ref returns** - avoid copying structs when modifying

### Debugging
1. **Log entity creation/destruction** - track entity lifecycle
2. **Visualize component dependencies** - understand system interactions
3. **Profile component queries** - optimize hot paths
4. **Add debug components** - make debugging easy

## Conclusion

The component-based architecture provides:

✅ **Extreme Flexibility** - Mix any behaviors by combining components  
✅ **High Performance** - Only process entities that need processing  
✅ **Perfect Scalability** - Easily add new components and systems  
✅ **Multiplayer Ready** - Pure data components sync easily  
✅ **Testable** - Systems can be tested independently  
✅ **Maintainable** - Clear separation of data and logic  

This architecture will serve your game well as it grows in complexity, supports multiplayer, and requires high performance. The investment in learning ECS concepts pays off hugely in the long term.

Your game is now future-proof and ready to scale to any complexity level! 🚀