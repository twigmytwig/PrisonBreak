# Prison Break Game

A 2D prison escape game built with MonoGame, featuring a modern **Scene-Based + Entity Component System (ECS)** architecture for high performance and multiplayer readiness.

## 🚀 Latest Major Update: Scene Management System (v2.0.0)

This project has evolved through **two major architectural improvements**:

1. **ECS Migration**: Refactored from inheritance-based entities to pure **Entity Component System (ECS)**
2. **Scene Architecture**: Added professional scene management with start menu and player type selection

The combined architecture provides:

- ✅ **Extreme Flexibility** - Mix any behaviors by combining components
- ✅ **High Performance** - Only process entities that need processing  
- ✅ **Perfect Scalability** - Easily add new components and systems
- ✅ **Multiplayer Ready** - Pure data components sync easily
- ✅ **Testable** - Systems can be tested independently
- ✅ **Maintainable** - Clear separation of data and logic
- ✅ **Professional UX** - Proper start menu with player type selection
- ✅ **Scene Management** - Clean separation between game states

## 🎮 Game Overview

Players control prisoners trying to escape from a prison while avoiding AI-controlled cops. The game features:

- **Professional Start Menu** - Player type selection before gameplay
- **Multi-player support** - Each player controls their own prisoner or cop
- **AI-driven enemies** - Cops with various behavior patterns  
- **Component-based entities** - Mix and match behaviors
- **Player type system** - Choose between Prisoner and Cop with distinct abilities
- **Scene-based architecture** - Clean separation between menu and gameplay
- **Scalable design** - Easy to add new entity types and game modes

## 🏗️ Architecture

### Scene Management + Entity Component System

The game uses a **hybrid Scene + ECS architecture** that combines the best of both patterns:

- **Scenes** provide high-level game state organization (Menu, Gameplay, Pause)
- **Within each scene**: Pure ECS architecture handles entities, components, and systems
- **Scene transitions** are event-driven and handle content loading automatically

### Entity Component System (ECS)

Within each scene, the game uses pure ECS architecture where:

- **Entities** are just containers with an ID
- **Components** are pure data structures (no logic)
- **Systems** contain all the game logic

```
SceneManager
├── StartMenuScene
│   ├── Menu Entities (MenuItemComponent, TextComponent)
│   ├── MenuInputSystem (navigation)
│   └── MenuRenderSystem (UI rendering)
└── GameplayScene  
    ├── Game Entities (Player, Cops, etc.)
    ├── ComponentInputSystem
    ├── ComponentMovementSystem
    ├── ComponentCollisionSystem
    └── ComponentRenderSystem

Entity (ID + Components)
├── Components (Pure Data):
│   ├── TransformComponent (position, rotation, scale)
│   ├── SpriteComponent (visual representation)
│   ├── MovementComponent (velocity, physics)
│   ├── CollisionComponent (bounds, collision data)
│   ├── PlayerInputComponent (input handling)
│   ├── PlayerTypeComponent (Prisoner/Cop classification)
│   ├── MenuItemComponent (UI elements)
│   ├── TextComponent (text rendering)
│   └── AIComponent (autonomous behavior)
└── Systems (Pure Logic):
    ├── ComponentInputSystem / MenuInputSystem
    ├── ComponentMovementSystem
    ├── ComponentCollisionSystem
    └── ComponentRenderSystem / MenuRenderSystem
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
| `PlayerTypeComponent` | Player role & attributes | `PlayerType Type`, `float SpeedMultiplier`, `string AnimationName` |
| `MenuItemComponent` | **NEW** UI elements | `bool IsSelected`, `Color BackgroundColor`, `int Width`, `int Height` |
| `TextComponent` | **NEW** Text rendering | `string Text`, `SpriteFont Font`, `Color Color`, `TextAlignment` |

### System Execution Order

#### StartMenuScene
1. **MenuInputSystem** - Menu navigation (arrows, enter, escape)
2. **MenuRenderSystem** - UI rendering with fonts

#### GameplayScene  
1. **ComponentInputSystem** - Captures input, sends events
2. **ComponentMovementSystem** - Processes movement events, applies physics  
3. **ComponentCollisionSystem** - Detects collisions, sends collision events
4. **ComponentRenderSystem** - Draws everything

## 👥 Player Type System

The game features a flexible player type system that differentiates between prisoners and cops:

### **Player Types**
- **Prisoners** - Human-controlled players trying to escape
  - Uses "prisoner-animation" sprite
  - Standard movement speed (1.0x multiplier)
  - Targeted by AI cops

- **Cops** - Can be AI or human-controlled
  - Uses "cop-animation" sprite  
  - Faster movement speed (1.2x multiplier)
  - AI cops automatically target prisoners

### **PlayerTypeComponent**
```csharp
public struct PlayerTypeComponent
{
    public PlayerType Type;           // Prisoner or Cop
    public float SpeedMultiplier;     // Speed modifier (cops are faster)
    public string AnimationName;      // Sprite animation to use
}
```

### **Creating Entities with Player Types**
```csharp
// Create a prisoner (human-controlled)
var prisoner = entityManager.CreatePlayer(position, PlayerIndex.One, PlayerType.Prisoner);

// Create a cop (can be human-controlled for multiplayer)
var humanCop = entityManager.CreatePlayer(position, PlayerIndex.Two, PlayerType.Cop);

// Create an AI cop
var aiCop = entityManager.CreateCop(position, AIBehavior.Chase);
```

### **AI Behavior**
- **Intelligent Targeting**: Cop AIs automatically find and chase the nearest prisoner
- **Type-Aware**: Cops ignore other cops and only pursue prisoners
- **Speed Advantage**: Cops move 20% faster than prisoners for balanced gameplay

### **Future Expansion**
The system is designed for easy expansion:
- Additional player types (guards, special prisoners, etc.)
- Custom animations per type
- Type-specific abilities and permissions
- Inventory restrictions based on player type

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
│   ├── Components.cs       # All component definitions (includes new UI components)
│   ├── ComponentEntityManager.cs  # Entity management and queries
│   ├── EventSystem.cs      # Event bus for system communication
│   └── Systems/
│       ├── AnimationSystem.cs
│       ├── ComponentInputSystem.cs
│       ├── ComponentMovementSystem.cs
│       ├── ComponentCollisionSystem.cs
│       ├── ComponentRenderSystem.cs
│       ├── MenuInputSystem.cs       # NEW: Menu navigation
│       └── MenuRenderSystem.cs      # NEW: UI rendering
├── Scenes/                          # NEW: Scene management
│   ├── Scene.cs            # Abstract base scene
│   ├── SceneManager.cs     # Scene lifecycle and transitions
│   ├── StartMenuScene.cs   # Player type selection menu
│   └── GameplayScene.cs    # Wrapped game logic
├── Game/
│   ├── Game1.cs           # Main game class (now ~75 lines!)
│   └── Program.cs         # Entry point
├── Managers/
│   └── SystemManager.cs   # Coordinates system lifecycle
├── Content/               # Game assets
│   ├── MinecraftFont.spritefont    # NEW: Font descriptor
│   ├── fonts/minecraft/Minecraft.ttf # NEW: Font file
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
// Easy way using the factory method
var playerCop = entityManager.CreatePlayer(position, PlayerIndex.Two, PlayerType.Cop);

// Manual way for custom behavior
var customCop = entityManager.CreateEntity();
customCop.AddComponent(new TransformComponent(position));
customCop.AddComponent(new PlayerTypeComponent(PlayerType.Cop));
customCop.AddComponent(new PlayerInputComponent(PlayerIndex.Two));
customCop.AddComponent(new MovementComponent(100f));
// PlayerTypeComponent automatically provides sprite animation and speed multiplier
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

### Start Menu
- **Up/Down Arrows** - Navigate menu options
- **Left/Right Arrows** - Change player type when "Start Game" is selected
- **Enter** - Confirm selection / Start game
- **ESC** - Exit game

### Gameplay
- **WASD** - Player 1 movement
- **Arrow Keys** - Player 2 movement (if available)
- **ESC** - Return to start menu
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

- [`CHANGELOG.md`](PrisonBreak/CHANGELOG.md) - **NEW** Complete v2.0.0 release notes
- [`GameLoopArchitectureComponentBased.md`](PrisonBreak/_memory/GameLoopAndSystems/GameLoopArchitectureComponentBased.md) - **UPDATED** ECS + Scene architecture explanation
- [`futurePlans.md`](PrisonBreak/_memory/futurePlans.md) - **UPDATED** Architecture documentation and implementation notes
- [`ECS_QUICK_REFERENCE.md`](PrisonBreak/_memory/ECS_QUICK_REFERENCE.md) - **UPDATED** Component and system reference with new UI components

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