# Prison Break Game Refactoring Plan

## Current Architecture Analysis

### Problems Identified in Game1.cs

**Issue**: Monolithic design with 352 lines of mixed responsibilities
- **Why it's a problem**: Single file handling input, collision, rendering, and game logic makes code hard to maintain and test
- **Impact**: Adding new features requires modifying the main game loop, increasing bug risk

**Issue**: Manual object management and hardcoded values
- **Why it's a problem**: Direct manipulation of positions, scales (4.0f), collision ratios (0.5f) scattered throughout code
- **Impact**: Difficult to balance game mechanics, no consistency across objects

**Issue**: Duplicate collision detection code for prisoner and cop
- **Why it's a problem**: Same collision logic repeated with minor variations (lines 108-139 vs 144-181)
- **Impact**: Code duplication leads to maintenance issues and inconsistent behavior

**Issue**: Mixed coordinate systems and magic numbers
- **Why it's a problem**: Movement speed, scaling factors, collision boundaries hardcoded without central configuration
- **Impact**: Game balance changes require hunting through code for scattered values

### MonoGameLibrary Integration Issues

**Issue**: Underutilized GameObjectFactory
- **Current state**: GameObjectFactory.cs exists but only has empty `MyProperty` list
- **Why it's problematic**: Factory pattern intended for object creation not being used
- **Impact**: Missing centralized object creation and configuration

**Issue**: No proper entity abstraction
- **Current state**: Direct AnimatedSprite manipulation in Game1.cs
- **Why it's problematic**: No separation between visual representation and game logic
- **Impact**: Cannot easily add new entity types or behaviors

## Refactoring Strategy

### Phase 1: Entity-Component Architecture

**Step 1.1: Create Base Entity System**
```csharp
// New files to create:
// Entities/BaseEntity.cs
// Entities/Player.cs  
// Entities/Enemy.cs
// Components/TransformComponent.cs
// Components/SpriteComponent.cs
// Components/CollisionComponent.cs
// Components/MovementComponent.cs
```

**Why this approach**: 
- Separates data (components) from behavior (systems)
- Makes entities composable and reusable
- Follows existing MonoGameLibrary patterns

**Step 1.2: Implement Component System**
- Move position tracking to TransformComponent
- Move sprite rendering to SpriteComponent  
- Move collision detection to CollisionComponent
- Move input handling to MovementComponent

**Why each component**:
- **TransformComponent**: Centralizes position, rotation, scale data
- **SpriteComponent**: Handles visual representation separately from logic
- **CollisionComponent**: Reusable collision logic with configurable bounds
- **MovementComponent**: Input-to-movement translation with configurable speeds

### Phase 2: System Architecture

**Step 2.1: Create Game Systems**
```csharp
// New files to create:
// Systems/RenderSystem.cs
// Systems/MovementSystem.cs  
// Systems/CollisionSystem.cs
// Systems/InputSystem.cs
// Managers/EntityManager.cs
// Managers/SystemManager.cs
```

**Why systems approach**:
- **RenderSystem**: Handles all drawing operations, can batch similar operations
- **MovementSystem**: Processes all movement in one place, easier to add physics
- **CollisionSystem**: Centralized collision detection, can optimize with spatial partitioning
- **InputSystem**: Maps input to actions, easier to add input remapping

**Step 2.2: Implement Managers**
- **EntityManager**: Tracks all entities, handles creation/destruction
- **SystemManager**: Coordinates system execution order and dependencies

**Why managers**:
- **EntityManager**: Provides central entity registry, enables entity queries
- **SystemManager**: Ensures systems run in correct order (Input → Movement → Collision → Render)

### Phase 3: Configuration and Flexibility

**Step 3.1: Create Configuration System**
```csharp
// New files to create:
// Config/GameConfig.cs
// Config/EntityConfig.cs
// Data/prisoner-config.json
// Data/cop-config.json
```

**Why configuration files**:
- Game balance changes without recompilation
- Easy to create entity variants
- Version control for game balance iterations

**Step 3.2: Enhanced GameObjectFactory Usage**
```csharp
// Enhance existing:
// MonoGameLibrary/GameObjectHelpers/GameObjectFactory.cs
```

**Why enhance factory**:
- Centralized entity creation using configuration
- Consistent entity setup and initialization
- Easy to add new entity types

### Phase 4: Performance and Scalability

**Step 4.1: Object Pooling**
- Pool frequently created/destroyed entities
- Reuse collision detection objects
- Pool rendering batches

**Why object pooling**:
- Reduces garbage collection pressure
- Consistent performance with many entities
- Essential for mobile/console deployment

**Step 4.2: Scene Management**
```csharp
// New files to create:
// Scenes/BaseScene.cs
// Scenes/GameScene.cs
// Scenes/MenuScene.cs
// Managers/SceneManager.cs
```

**Why scene management**:
- Separate game states (menu, gameplay, pause)
- Easier to add levels and transitions
- Memory management for large games

## Implementation Priority

### High Priority (Immediate Benefits)
1. **Extract configuration constants** from Game1.cs
   - Immediate: Easier game balance iteration
   - Future: Foundation for data-driven design

2. **Create Player and Enemy entity classes**
   - Immediate: Cleaner Game1.cs, reduced duplication
   - Future: Easy to add new entity types

3. **Implement basic ComponentSystem for movement**
   - Immediate: Testable movement logic
   - Future: Foundation for complex behaviors

### Medium Priority (Architecture Benefits)
1. **Add CollisionSystem with spatial optimization**
   - Immediate: Better collision performance
   - Future: Supports many entities efficiently

2. **Implement EntityManager and basic factory usage**
   - Immediate: Centralized entity lifecycle
   - Future: Save/load game states

### Low Priority (Future Scalability)
1. **Full scene management system**
   - Future: Multiple levels, menus, transitions

2. **Advanced object pooling**
   - Future: Performance optimization for complex scenes

## File Structure After Refactoring

```
PrisonBreak/
├── _memory/
│   └── futurePlans.md
├── Components/
│   ├── TransformComponent.cs
│   ├── SpriteComponent.cs
│   ├── CollisionComponent.cs
│   └── MovementComponent.cs
├── Entities/
│   ├── BaseEntity.cs
│   ├── Player.cs
│   └── Enemy.cs
├── Systems/
│   ├── RenderSystem.cs
│   ├── MovementSystem.cs
│   ├── CollisionSystem.cs
│   └── InputSystem.cs
├── Managers/
│   ├── EntityManager.cs
│   ├── SystemManager.cs
│   └── SceneManager.cs
├── Config/
│   ├── GameConfig.cs
│   └── EntityConfig.cs
├── Data/
│   ├── prisoner-config.json
│   └── cop-config.json
├── Scenes/
│   ├── BaseScene.cs
│   └── GameScene.cs
└── Game1.cs (reduced to ~50 lines)
```

## Expected Benefits

### Short Term
- **Maintainability**: Easier to find and fix bugs
- **Testability**: Individual components can be unit tested
- **Readability**: Clear separation of concerns

### Long Term  
- **Scalability**: Easy to add new entity types and behaviors
- **Performance**: Optimized systems and object pooling
- **Flexibility**: Data-driven configuration enables rapid iteration
- **Team Development**: Multiple developers can work on different systems simultaneously

## Next Steps

1. Start with configuration extraction (lowest risk, immediate benefit)
2. Create basic entity classes to replace direct sprite manipulation
3. Implement one system at a time (start with MovementSystem)
4. Gradually migrate Game1.cs logic to appropriate systems
5. Add scene management once core systems are stable

This approach minimizes risk by allowing incremental migration while providing immediate benefits at each step.