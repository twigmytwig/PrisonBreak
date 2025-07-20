# Prison Break Game

A 2D prison escape game built with MonoGame, featuring a modern **Entity Component System (ECS)** architecture for high performance and multiplayer readiness.

## 🚀 Major Update: Component-Based Architecture

This project has been **completely refactored** from an inheritance-based entity system to a pure **Entity Component System (ECS)**. This architectural change provides:

- ✅ **Extreme Flexibility** - Mix any behaviors by combining components
- ✅ **High Performance** - Only process entities that need processing  
- ✅ **Perfect Scalability** - Easily add new components and systems
- ✅ **Multiplayer Ready** - Pure data components sync easily
- ✅ **Testable** - Systems can be tested independently
- ✅ **Maintainable** - Clear separation of data and logic

## 🎮 Game Overview

Players control prisoners trying to escape from a prison while avoiding AI-controlled cops. The game features:

- **Multi-player support** - Each player controls their own prisoner
- **AI-driven enemies** - Cops with various behavior patterns
- **Component-based entities** - Mix and match behaviors
- **Scalable architecture** - Easy to add new entity types

## 🏗️ Architecture

### Entity Component System (ECS)

The game uses a pure ECS architecture where:

- **Entities** are just containers with an ID
- **Components** are pure data structures (no logic)
- **Systems** contain all the game logic

```
Entity (ID + Components)
├── Components (Pure Data):
│   ├── TransformComponent (position, rotation, scale)
│   ├── SpriteComponent (visual representation)
│   ├── MovementComponent (velocity, physics)
│   ├── CollisionComponent (bounds, collision data)
│   ├── PlayerInputComponent (input handling)
│   └── AIComponent (autonomous behavior)
└── Systems (Pure Logic):
    ├── ComponentInputSystem
    ├── ComponentMovementSystem
    ├── ComponentCollisionSystem
    └── ComponentRenderSystem
```

### Core Components

| Component | Purpose | Data |
|-----------|---------|------|
| `TransformComponent` | Position, rotation, scale | `Vector2 Position`, `float Rotation`, `Vector2 Scale` |
| `SpriteComponent` | Visual representation | `AnimatedSprite`, `bool Visible`, `Color Tint` |
| `MovementComponent` | Physics and movement | `Vector2 Velocity`, `float MaxSpeed`, `float Friction` |
| `CollisionComponent` | Collision detection | `RectangleCollider`, `bool IsSolid`, `string Layer` |
| `PlayerInputComponent` | Input handling | `PlayerIndex`, input state |
| `AIComponent` | AI behaviors | `AIBehavior`, state machine data |

### System Execution Order

Systems execute in a specific order each frame:

1. **ComponentInputSystem** - Captures input, sends events
2. **ComponentMovementSystem** - Processes movement events, applies physics  
3. **ComponentCollisionSystem** - Detects collisions, sends collision events
4. **ComponentRenderSystem** - Draws everything

## 🗂️ Project Structure

```
PrisonBreak/
├── Config/
│   ├── EntityConfig.cs      # Entity configuration data
│   └── GameConfig.cs        # Game-wide configuration
├── Core/
│   ├── Graphics/            # Sprite, animation, tilemap systems
│   ├── Input/              # Input management
│   ├── Math/               # Math utilities
│   └── Physics/            # Collision detection
├── ECS/
│   ├── Entity.cs           # Entity container
│   ├── Components.cs       # All component definitions
│   ├── ComponentEntityManager.cs  # Entity management and queries
│   ├── EventSystem.cs      # Event bus for system communication
│   └── Systems/
│       ├── AnimationSystem.cs
│       ├── ComponentInputSystem.cs
│       ├── ComponentMovementSystem.cs
│       ├── ComponentCollisionSystem.cs
│       └── ComponentRenderSystem.cs
├── Game/
│   ├── Game1.cs           # Main game class (now ~100 lines!)
│   └── Program.cs         # Entry point
├── Managers/
│   └── SystemManager.cs   # Coordinates system lifecycle
├── Content/               # Game assets
│   └── images/
└── _memory/              # Documentation and planning
```

## 🔧 Building and Running

### Prerequisites

- .NET 8.0 SDK
- MonoGame Framework

### Build Instructions

```bash
# Clone the repository
git clone <repository-url>
cd prisonbreak

# Restore dependencies
dotnet restore PrisonBreak/

# Build the project
dotnet build PrisonBreak/

# Run the game
dotnet run --project PrisonBreak/
```

## 🎯 Creating New Entity Types

The ECS architecture makes it trivial to create new entity types by combining components:

### Moving Pickup
```csharp
var pickup = entityManager.CreateEntity();
pickup.AddComponent(new TransformComponent(position));
pickup.AddComponent(new SpriteComponent(pickupSprite));
pickup.AddComponent(new MovementComponent(25f)); // Slow movement
pickup.AddComponent(new AIComponent(AIBehavior.Wander));
// No collision = passes through walls!
```

### Player-Controlled Cop (Multiplayer)
```csharp
var playerCop = entityManager.CreateEntity();
playerCop.AddComponent(new TransformComponent(position));
playerCop.AddComponent(new SpriteComponent(copSprite));
playerCop.AddComponent(new MovementComponent(100f));
playerCop.AddComponent(new CollisionComponent(collider));
playerCop.AddComponent(new PlayerInputComponent(PlayerIndex.Two)); // Player 2
playerCop.AddComponent(new CopTag(playerCop.Id));
```

### Invisible Ghost Enemy
```csharp
var ghost = entityManager.CreateEntity();
ghost.AddComponent(new TransformComponent(position));
// No sprite = invisible!
ghost.AddComponent(new MovementComponent(50f));
// No collision = passes through everything!
ghost.AddComponent(new AIComponent(AIBehavior.Chase));
ghost.AddComponent(new CopTag(ghost.Id));
```

## 🎮 Controls

- **WASD** - Player 1 movement
- **Arrow Keys** - Player 2 movement (if available)
- **Gamepad** - Full gamepad support for multiple players

## 🔄 Migration from Inheritance Architecture

This project previously used an inheritance-based entity system. The migration provides:

| Feature | Before (Inheritance) | After (Components) |
|---------|---------------------|-------------------|
| **Memory Usage** | Higher (all entities have all fields) | Lower (only needed components) |
| **Update Performance** | Slower (check all entities) | Faster (query only relevant) |
| **Flexibility** | Low (fixed hierarchy) | High (mix any components) |
| **Scalability** | Poor (becomes unwieldy) | Excellent (linear scaling) |

## 📚 Documentation

- [`GameLoopArchitectureComponentBased.md`](PrisonBreak/_memory/GameLoopAndSystems/GameLoopArchitectureComponentBased.md) - Detailed ECS architecture explanation
- [`futurePlans.md`](PrisonBreak/_memory/futurePlans.md) - Original refactoring plan and rationale

## 🧱 Advanced Tile-Based Collision System

This project features an **efficient tile-based collision system** that provides smooth wall collision and high performance:

### **🎯 Key Features**
- **Tile-based collision map** - O(1) collision detection vs O(n) entity checks
- **Smooth wall sliding** - Natural movement along wall edges without getting stuck
- **Adjacent wall support** - Perfect handling of connected wall segments
- **Performance scaling** - Handles hundreds of walls without FPS degradation
- **Predictive collision** - Prevents clipping by checking movement before execution

### **🔧 How It Works**
1. **Collision Map Generation** - Creates 2D boolean array from tilemap data
2. **Grid-Based Detection** - Converts world positions to tile coordinates for instant lookup
3. **Swept Movement** - Tests movement path in small steps to find exact collision points
4. **Smart Sliding** - Projects remaining movement along wall surfaces
5. **Stuck Recovery** - Automatically escapes if player gets trapped in walls

### **⚙️ Adding Collidable Tiles**

To add new solid tile types, update the `solidTileIds` array in `ComponentMovementSystem.SetCollisionMap()`:

```csharp
// In ComponentMovementSystem.cs
int[] solidTileIds = { 2, 3, 4, 5 }; // Add new tile IDs here
// 02 = prison bars, 03 = walls, 04 = tables, 05 = doors, etc.
```

Then update your tilemap XML file to use the new tile IDs:
```xml
<Tiles>
    00 00 04 04 04 00 00  <!-- 04 = tables -->
    00 00 03 02 03 00 00  <!-- 03 = walls, 02 = prison bars -->
    00 00 05 00 05 00 00  <!-- 05 = doors -->
</Tiles>
```

### **📈 Performance Comparison**

| Feature | Old System (Entity-based) | New System (Tile-based) |
|---------|---------------------------|-------------------------|
| **Collision Detection** | O(n) per entity per wall | O(1) tile lookup |
| **Memory Usage** | High (entity per tile) | Low (2D boolean array) |
| **Adjacent Walls** | Buggy collision confusion | Perfect seamless handling |
| **Scalability** | Degrades with wall count | Constant performance |
| **Wall Sliding** | Harsh, gets stuck | Smooth, natural movement |

## 🚀 Performance Benefits

The ECS architecture provides significant performance improvements:

- **Query-based processing** - Only process entities with relevant components
- **Cache-friendly memory layout** - Components stored contiguously
- **Reduced type checking** - Direct component access without casting
- **Batch operations** - Process similar operations together
- **Tile-based collision** - O(1) collision detection for environment

## 🌐 Multiplayer Ready

The pure data components make networking simple:

```csharp
// Serialize entity state for network
public NetworkEntityData SerializeEntity(Entity entity)
{
    return new NetworkEntityData
    {
        EntityId = entity.Id,
        Transform = entity.GetComponent<TransformComponent>(),
        Movement = entity.GetComponent<MovementComponent>(),
    };
}
```

## 🔍 Development Tools

### Entity Inspector
Debug any entity to see its components:
```csharp
entity.DebugComponents(); // Prints all components
```

### System Performance Profiling
Built-in performance tracking for each system.

### Component Statistics
Monitor entity composition in real-time.

## 🤝 Contributing

The modular ECS architecture makes it easy to contribute:

1. **Add new components** - Create pure data structures
2. **Add new systems** - Implement `IGameSystem` interface
3. **Create entity types** - Combine existing components
4. **Add events** - Use the event bus for system communication

## 📄 License

[Add your license information here]

---

**Note**: This architecture represents a complete rewrite focusing on performance, scalability, and maintainability. The game is now ready to scale to any complexity level and supports multiplayer networking out of the box.