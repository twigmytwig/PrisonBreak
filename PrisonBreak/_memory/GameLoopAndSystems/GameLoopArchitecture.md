# Game Loop and Systems Architecture

## Overview

The PrisonBreak game has been refactored from a monolithic architecture to a modular, systems-based architecture. This approach separates concerns, improves maintainability, and provides a scalable foundation for future development.

### Benefits of Systems Architecture

- **Separation of Concerns**: Each system handles a specific responsibility
- **Testability**: Systems can be unit tested independently
- **Scalability**: Easy to add new systems and features without affecting existing code
- **Performance**: Systems can be optimized individually and run in specific orders
- **Team Development**: Multiple developers can work on different systems simultaneously

### Before vs After Comparison

**Before (Monolithic):**
- Game1.cs: ~280 lines with mixed responsibilities
- Input, movement, collision, and rendering all in one Update() method
- Hardcoded constants scattered throughout
- Direct entity manipulation

**After (Systems-Based):**
- Game1.cs: ~120 lines focused on orchestration
- 4 specialized systems handling specific concerns
- Centralized configuration management
- Clean separation between data and logic

## Core Architecture Components

### 1. IGameSystem Interface

```csharp
public interface IGameSystem
{
    void Initialize();
    void Update(GameTime gameTime);
    void Draw(SpriteBatch spriteBatch);
    void Shutdown();
}
```

**Purpose**: Defines the contract that all game systems must implement, ensuring consistent lifecycle management.

**Methods**:
- `Initialize()`: Setup system resources and initial state
- `Update()`: Process system logic each frame
- `Draw()`: Handle rendering operations
- `Shutdown()`: Cleanup resources when system is destroyed

### 2. SystemManager

**Location**: `PrisonBreak/Managers/SystemManager.cs`

**Responsibilities**:
- Maintains a collection of all game systems
- Orchestrates system execution order
- Ensures proper initialization and shutdown
- Provides centralized system lifecycle management

**Key Features**:
- Systems are executed in the order they were added
- Automatic initialization of systems added after manager is initialized
- Clean shutdown of all systems

**Usage**:
```csharp
var systemManager = new SystemManager();
systemManager.AddSystem(new InputSystem());
systemManager.AddSystem(new MovementSystem());
systemManager.AddSystem(new CollisionSystem());
systemManager.AddSystem(new RenderSystem());
systemManager.Initialize();

// In game loop:
systemManager.Update(gameTime);
systemManager.Draw(spriteBatch);
```

### 3. EntityManager

**Location**: `PrisonBreak/Managers/EntityManager.cs`

**Responsibilities**:
- Manages entity creation and destruction
- Loads and manages texture atlas
- Provides centralized access to game entities
- Handles entity configuration and initialization

**Key Features**:
- Factory methods for creating configured entities
- Centralized texture atlas management
- Validation to ensure proper initialization order

## Game Systems

### 1. InputSystem

**Location**: `PrisonBreak/Systems/InputSystem.cs`

**Purpose**: Handles all player input and converts it to game commands.

**Input Sources**:
- Keyboard (WASD, Arrow keys, Space for speed boost)
- GamePad (Thumbsticks, D-Pad, A button for speed boost, vibration feedback)

**Output**: `MovementInput` struct containing:
- `Direction`: Normalized movement vector
- `SpeedBoost`: Boolean indicating if speed boost is active

**Key Features**:
- Separates input handling from movement logic
- Supports multiple input devices simultaneously
- Gamepad vibration feedback
- Analog thumbstick support with fallback to digital input

### 2. MovementSystem

**Location**: `PrisonBreak/Systems/MovementSystem.cs`

**Purpose**: Applies movement to entities based on input and their movement properties.

**Responsibilities**:
- Processes player movement based on InputSystem commands
- Handles enemy AI movement (cop patrolling)
- Applies speed modifiers and movement calculations
- Updates entity positions

**Configuration Used**:
- `GameConfig.BaseMovementSpeed`: Base movement speed for entities
- `GameConfig.SpeedBoostMultiplier`: Speed increase when boost is active

**Data Flow**:
1. Receives MovementInput from InputSystem
2. Calculates movement vector with speed modifiers
3. Updates entity positions
4. Handles cop autonomous movement

### 3. CollisionSystem

**Location**: `PrisonBreak/Systems/CollisionSystem.cs`

**Purpose**: Handles all collision detection and response in the game.

**Collision Types**:
- **Bounds Collision**: Keeps entities within room boundaries
- **Entity-to-Entity**: Detects collisions between player and enemies

**Key Features**:
- Player boundary constraint with collider offset calculations
- Cop boundary collision with physics-based reflection
- Player-cop collision detection with teleportation response
- Configurable collision bounds

**Collision Response**:
- Player: Position clamping to stay within bounds
- Cop: Velocity reflection when hitting boundaries
- Player-Cop: Cop teleports to random position when touched

### 4. RenderSystem

**Location**: `PrisonBreak/Systems/RenderSystem.cs`

**Purpose**: Handles all drawing operations and visual representation.

**Rendering Order**:
1. Tilemap (background)
2. Player sprite
3. Cop sprite
4. Debug information (collision boxes if debug mode enabled)

**Key Features**:
- Centralized sprite animation updates
- Debug visualization with colored collision boxes
- Proper resource management for debug textures
- Memory leak prevention (textures created once, not per frame)

**Debug Features**:
- Red collision box for player
- Blue collision box for cop
- Toggleable debug mode via entity configuration

## Configuration System

### GameConfig

**Location**: `PrisonBreak/Config/GameConfig.cs`

**Purpose**: Centralized game-wide constants and settings.

**Categories**:
- **Window Settings**: Title, dimensions, fullscreen mode
- **Movement**: Base speed, speed boost multiplier
- **Graphics**: Sprite scaling, collision ratios
- **Debug**: Colors, thickness values

**Example Constants**:
```csharp
public const float BaseMovementSpeed = 5.0f;
public const float SpeedBoostMultiplier = 1.5f;
public const float SpriteScale = 4.0f;
public static readonly Color BackgroundColor = Color.CornflowerBlue;
```

### EntityConfig

**Location**: `PrisonBreak/Config/EntityConfig.cs`

**Purpose**: Entity-specific configurations and settings.

**Configurations**:
- **Player**: Animation name, debug mode, scale
- **Cop**: Animation name, movement speed, scale
- **Tilemap**: Configuration file path, scale
- **TextureAtlas**: Configuration file path

**Benefits**:
- Easy to create entity variants
- Data-driven entity creation
- Consistent configuration across similar entities

## Execution Flow

### 1. Initialization Sequence

```
Game1.Initialize()
├── Create SystemManager and EntityManager
├── Create all game systems
├── Add systems to SystemManager (order matters!)
│   ├── InputSystem
│   ├── MovementSystem
│   ├── CollisionSystem
│   └── RenderSystem
└── SystemManager.Initialize() → calls Initialize() on all systems

Game1.LoadContent()
├── Load tilemap from configuration
├── Set tilemap scale
└── Pass tilemap to RenderSystem

First Update() call:
├── Initialize room bounds (requires GraphicsDevice)
├── Create entities via EntityManager
├── Wire up system dependencies
│   ├── MovementSystem ← InputSystem
│   ├── CollisionSystem ← entities + bounds
│   └── RenderSystem ← entities + tilemap
└── Mark initialization complete
```

### 2. Update Loop Order

Each frame, systems execute in this order:

```
Game1.Update()
├── Handle exit conditions (Back button, Escape key)
├── Initialize room bounds (first frame only)
└── SystemManager.Update()
    ├── InputSystem.Update() → reads input, generates MovementInput
    ├── MovementSystem.Update() → applies movement to entities
    ├── CollisionSystem.Update() → checks collisions, constrains positions
    └── RenderSystem.Update() → updates sprite animations
```

**Why This Order Matters**:
1. **Input first**: Capture user commands
2. **Movement second**: Apply movement based on input
3. **Collision third**: Correct positions after movement
4. **Render last**: Update animations (visual state)

### 3. Draw Sequence

```
Game1.Draw()
├── Clear background (GameConfig.BackgroundColor)
├── Begin SpriteBatch
├── SystemManager.Draw()
│   ├── InputSystem.Draw() → (no-op)
│   ├── MovementSystem.Draw() → (no-op)
│   ├── CollisionSystem.Draw() → (no-op)
│   └── RenderSystem.Draw() → draws all visual elements
│       ├── Tilemap
│       ├── Player sprite
│       ├── Cop sprite
│       └── Debug collision boxes (if enabled)
└── End SpriteBatch
```

### 4. System Dependencies

```
InputSystem (no dependencies)
    ↓
MovementSystem (depends on InputSystem)
    ↓
CollisionSystem (depends on entity positions)
    ↓
RenderSystem (depends on final entity states)
```

## Data Flow Diagrams

### Input to Movement Pipeline

```
User Input → InputSystem → MovementInput → MovementSystem → Entity Position Updates
    ↓
Keyboard/GamePad → Direction Vector + Speed Boost → Movement Calculations → New Positions
```

### Entity Creation Flow

```
Game1.LoadContent() → EntityManager.Initialize() → TextureAtlas Loading
    ↓
Game1.InitializeRoomBounds() → EntityManager.CreatePlayer/CreateCop() → Entity Creation
    ↓
System Wiring → Systems receive entity references → Game Loop Ready
```

### Collision Detection Flow

```
MovementSystem → New Entity Positions → CollisionSystem → Collision Response
    ↓                                      ↓
Player/Cop Updates → Bounds Checking → Position Correction
                  → Entity Collision → Special Responses (teleport, etc.)
```

## Performance Considerations

### Memory Management

- **Debug textures**: Created once in RenderSystem.Initialize(), not every frame
- **Entity pooling**: Future enhancement for frequently created/destroyed objects
- **Texture atlas**: Single atlas loads all sprites, reducing texture swaps

### Update Efficiency

- **System order**: Optimized to minimize redundant calculations
- **Early exits**: Systems check for null entities before processing
- **Configuration caching**: Constants stored in static classes, no runtime lookups

### Rendering Optimization

- **Batch rendering**: All sprites drawn in single SpriteBatch session
- **State management**: SpriteBatch Begin/End handled centrally in Game1
- **Debug rendering**: Only active when debug mode enabled

## Extension Points

### Adding New Systems

1. Create class implementing `IGameSystem`
2. Add to SystemManager in desired order
3. Wire up dependencies in Game1.InitializeRoomBounds()

Example:
```csharp
public class AudioSystem : IGameSystem
{
    public void Initialize() { /* Load sounds */ }
    public void Update(GameTime gameTime) { /* Update audio */ }
    public void Draw(SpriteBatch spriteBatch) { /* No-op */ }
    public void Shutdown() { /* Cleanup audio */ }
}

// In Game1.Initialize():
_audioSystem = new AudioSystem();
_systemManager.AddSystem(_audioSystem);
```

### Adding New Entity Types

1. Add configuration to EntityConfig
2. Add creation method to EntityManager
3. Update relevant systems to handle new entity type

Example:
```csharp
// In EntityConfig:
public static class PowerUp
{
    public const string AnimationName = "powerup-animation";
    public static readonly Vector2 Scale = new(2.0f, 2.0f);
}

// In EntityManager:
public void CreatePowerUp(Vector2 position)
{
    var sprite = _atlas.CreateAnimatedSprite(EntityConfig.PowerUp.AnimationName);
    _powerUps.Add(new PowerUp(position, sprite, EntityConfig.PowerUp.Scale));
}
```

### Modifying Game Behavior

1. **Movement changes**: Modify GameConfig constants or MovementSystem logic
2. **Visual changes**: Update EntityConfig or RenderSystem
3. **Input changes**: Modify InputSystem key mappings or add new input types
4. **Collision changes**: Update CollisionSystem response logic

## Best Practices

### System Design

- **Single Responsibility**: Each system handles one specific concern
- **Loose Coupling**: Systems communicate through data structures, not direct calls
- **Configuration Driven**: Use config classes instead of hardcoded values
- **Error Handling**: Add null checks and validation in system methods

### Performance

- **Minimize Allocations**: Reuse data structures when possible
- **Cache Lookups**: Store frequently accessed values
- **Profile Systems**: Measure individual system performance
- **Optimize Hot Paths**: Focus on code that runs every frame

### Maintainability

- **Clear Naming**: Use descriptive names for systems and methods
- **Documentation**: Comment complex logic and system interactions
- **Configuration**: Externalize values that might need tuning
- **Testing**: Design systems to be testable in isolation

## Future Enhancements

### Scene Management

- Create Scene hierarchy for menus, gameplay, pause screens
- Scene-specific system loading and unloading
- Smooth transitions between scenes

### Advanced Entity System

- Component-based entities for more flexibility
- Entity queries and filtering
- Entity templates and prefabs

### Optimization

- Object pooling for frequently created entities
- Spatial partitioning for collision detection
- Multi-threading for independent systems
- Asset streaming for larger games

### Developer Tools

- Runtime system debugging
- Performance profiling tools
- Entity inspection and modification
- Configuration hot-reloading

This architecture provides a solid foundation that can grow from a simple prototype to a complex game while maintaining clean, organized, and performant code.