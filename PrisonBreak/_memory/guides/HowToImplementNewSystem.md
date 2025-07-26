# How to Implement New Systems in PrisonBreak

This guide provides step-by-step instructions for implementing different types of systems in the PrisonBreak game application.

## 📋 Table of Contents
- [ECS Systems](#ecs-systems)
- [Scenes](#scenes)
- [Components](#components)
- [Event-Driven Features](#event-driven-features)
- [UI Systems](#ui-systems)
- [Testing Your Implementation](#testing-your-implementation)

---

## 🔧 ECS Systems

### Overview
ECS Systems handle specific gameplay logic by processing entities with relevant components. All systems implement `IGameSystem` and follow the Entity-Component-System pattern.

### Step 1: Create the System Class
Create a new file in `PrisonBreak/ECS/Systems/` following the naming pattern `[YourFeature]System.cs`:

```csharp
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PrisonBreak.Systems;

namespace PrisonBreak.ECS.Systems;

public class YourFeatureSystem : IGameSystem
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
        // Set up event subscriptions, initial state
        _eventBus.Subscribe<YourEvent>(OnYourEvent);
    }
    
    public void Update(GameTime gameTime)
    {
        if (_entityManager == null) return;
        
        // Process entities with required components
        var entities = _entityManager.GetEntitiesWith<RequiredComponent>();
        
        foreach (var entity in entities)
        {
            // Your system logic here
            ref var component = ref entity.GetComponent<RequiredComponent>();
            // Modify component data
        }
    }
    
    public void Draw(SpriteBatch spriteBatch)
    {
        // Only implement if system needs to draw
        // Most systems don't draw - rendering is handled by ComponentRenderSystem
    }
    
    public void Shutdown()
    {
        // Clean up resources, unsubscribe from events
        _eventBus?.Unsubscribe<YourEvent>(OnYourEvent);
    }
    
    private void OnYourEvent(YourEvent evt)
    {
        // Handle event
    }
}
```

### Step 2: Add Required Components
If your system needs new components, add them to `PrisonBreak/ECS/Components.cs`:

```csharp
public struct YourFeatureComponent
{
    public float SomeValue;
    public bool IsActive;
    // Add your component fields
    
    public YourFeatureComponent(float someValue)
    {
        SomeValue = someValue;
        IsActive = true;
    }
}
```

### Step 3: Register System in Scene
Add your system to the appropriate scene's `SetupSystems()` method:

```csharp
// In GameplayScene.cs or your target scene
protected override void SetupSystems()
{
    // Add in correct execution order
    SystemManager.AddSystem(new ComponentInputSystem(EntityManager, EventBus));
    SystemManager.AddSystem(new YourFeatureSystem(EntityManager, EventBus)); // Add here
    SystemManager.AddSystem(new ComponentRenderSystem(EntityManager));
}
```

### Step 4: System Execution Order
Consider where your system fits in the execution pipeline:
1. **Input Systems** - Process user input → events
2. **Logic Systems** - Process game logic, AI, physics
3. **Collision Systems** - Handle collisions and interactions
4. **Render Systems** - Draw everything

---

## 🎬 Scenes

### Overview
Scenes manage different game states (Menu, Gameplay, Pause). Each scene has its own systems, entities, and lifecycle.

### Step 1: Create Scene Class
Create a new file in `PrisonBreak/Scenes/` following the pattern `[YourScene]Scene.cs`:

```csharp
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using PrisonBreak.ECS;
using PrisonBreak.ECS.Systems;

namespace PrisonBreak.Scenes;

public class YourSceneScene : Scene
{
    public YourSceneScene(EventBus eventBus) : base("YourScene", eventBus)
    {
    }
    
    protected override void SetupSystems()
    {
        // Add systems in execution order
        SystemManager.AddSystem(new ComponentInputSystem(EntityManager, EventBus));
        SystemManager.AddSystem(new YourFeatureSystem(EntityManager, EventBus));
        SystemManager.AddSystem(new ComponentRenderSystem(EntityManager));
        
        // Initialize all systems
        SystemManager.Initialize();
    }
    
    protected override void LoadSceneContent()
    {
        // Load scene-specific assets
        var texture = Content.Load<Texture2D>("your_texture");
        
        // Create initial entities
        CreateInitialEntities();
    }
    
    public override void OnEnter()
    {
        // Called when scene becomes active
        // Reset state, start music, etc.
    }
    
    public override void OnExit()
    {
        // Called when leaving scene
        // Pause music, save state, etc.
    }
    
    private void CreateInitialEntities()
    {
        // Create entities specific to this scene
        var entity = EntityManager.CreateEntity();
        entity.AddComponent(new TransformComponent(Vector2.Zero));
        // Add more components as needed
    }
}
```

### Step 2: Add Scene to SceneTypes
Add your scene to `PrisonBreak/Scenes/SceneTypes.cs`:

```csharp
public enum SceneType
{
    StartMenu,
    Gameplay,
    YourScene // Add your scene here
}
```

### Step 3: Register Scene in SceneManager
Update `PrisonBreak/Scenes/SceneManager.cs` to include your scene:

```csharp
private void InitializeScenes()
{
    _scenes[SceneType.StartMenu] = new StartMenuScene(_eventBus);
    _scenes[SceneType.Gameplay] = new GameplayScene(_eventBus);
    _scenes[SceneType.YourScene] = new YourSceneScene(_eventBus); // Add here
}
```

### Step 4: Scene Transitions
Add scene transition logic where needed:

```csharp
// In your system or event handler
_eventBus.Send(new SceneChangeEvent(SceneType.YourScene));
```

---

## 🧩 Components

### Overview
Components are data containers that define entity properties. They should be lightweight structs with minimal logic.

### Step 1: Define Component
Add to `PrisonBreak/ECS/Components.cs`:

```csharp
/// <summary>
/// Component description - what this component represents
/// </summary>
public struct YourComponent
{
    public float Value;
    public bool IsEnabled;
    public Vector2 Direction;
    
    public YourComponent(float value)
    {
        Value = value;
        IsEnabled = true;
        Direction = Vector2.Zero;
    }
}
```

### Step 2: Add Factory Methods (Optional)
If creating complex entities, add factory methods to `ComponentEntityManager`:

```csharp
// In ComponentEntityManager.cs
public Entity CreateYourEntity(Vector2 position, float value)
{
    var entity = CreateEntity();
    entity.AddComponent(new TransformComponent(position));
    entity.AddComponent(new YourComponent(value));
    entity.AddComponent(new SpriteComponent(/* sprite */));
    return entity;
}
```

### Component Design Guidelines
- **Keep components simple** - just data, no logic
- **Use structs** for better performance
- **Provide constructors** for easy initialization
- **Document purpose** with XML comments
- **Group related data** but avoid monolithic components

---

## 📡 Event-Driven Features

### Overview
Events enable loose coupling between systems. Use events for cross-system communication.

### Step 1: Define Event
Add to `PrisonBreak/ECS/EventSystem.cs`:

```csharp
public struct YourEvent
{
    public int EntityId;
    public float Value;
    public bool Success;
    
    public YourEvent(int entityId, float value, bool success = true)
    {
        EntityId = entityId;
        Value = value;
        Success = success;
    }
}
```

### Step 2: Send Events
In your system's Update method:

```csharp
public void Update(GameTime gameTime)
{
    // When something happens, send an event
    if (conditionMet)
    {
        _eventBus.Send(new YourEvent(entity.Id, someValue));
    }
}
```

### Step 3: Subscribe to Events
In system's Initialize method:

```csharp
public void Initialize()
{
    _eventBus.Subscribe<YourEvent>(OnYourEvent);
}

private void OnYourEvent(YourEvent evt)
{
    // Handle the event
    var entity = _entityManager.GetEntity(evt.EntityId);
    if (entity != null)
    {
        // Process the event
    }
}
```

### Step 4: Unsubscribe (Important!)
In system's Shutdown method:

```csharp
public void Shutdown()
{
    _eventBus?.Unsubscribe<YourEvent>(OnYourEvent);
}
```

---

## 🖥️ UI Systems

### Overview
UI systems handle rendering and interaction with user interface elements.

### Step 1: Create UI Render System
```csharp
public class YourUIRenderSystem : IGameSystem
{
    private ComponentEntityManager _entityManager;
    private SpriteFont _font;
    
    public void Initialize()
    {
        // Load UI assets
    }
    
    public void Update(GameTime gameTime)
    {
        // Update UI state if needed
    }
    
    public void Draw(SpriteBatch spriteBatch)
    {
        // Draw UI elements
        var uiEntities = _entityManager.GetEntitiesWith<UIComponent, TransformComponent>();
        
        foreach (var entity in uiEntities)
        {
            var ui = entity.GetComponent<UIComponent>();
            var transform = entity.GetComponent<TransformComponent>();
            
            // Draw UI element
            spriteBatch.DrawString(_font, ui.Text, transform.Position, ui.Color);
        }
    }
    
    public void Shutdown() { }
}
```

### Step 2: Add UI Components
```csharp
public struct UIComponent
{
    public string Text;
    public Color Color;
    public bool IsVisible;
    
    public UIComponent(string text, Color color)
    {
        Text = text;
        Color = color;
        IsVisible = true;
    }
}
```

### Step 3: Handle UI Input
Create a separate input system for UI or extend existing input systems:

```csharp
public class UIInputSystem : IGameSystem
{
    public void Update(GameTime gameTime)
    {
        var mouse = Mouse.GetState();
        var uiElements = _entityManager.GetEntitiesWith<UIComponent, TransformComponent>();
        
        foreach (var element in uiElements)
        {
            var transform = element.GetComponent<TransformComponent>();
            var bounds = new Rectangle((int)transform.Position.X, (int)transform.Position.Y, 100, 50);
            
            if (bounds.Contains(mouse.Position) && mouse.LeftButton == ButtonState.Pressed)
            {
                _eventBus.Send(new UIClickEvent(element.Id));
            }
        }
    }
}
```

---

## 🧪 Testing Your Implementation

### Step 1: Basic Functionality Test
1. **Build the project** - Ensure no compilation errors
2. **Run the game** - Verify it starts without crashes
3. **Check system registration** - Ensure your system appears in the scene

### Step 2: Component Testing
```csharp
// Test entity creation with your component
var testEntity = entityManager.CreateEntity();
testEntity.AddComponent(new YourComponent(100f));

// Verify component exists
if (testEntity.HasComponent<YourComponent>())
{
    var component = testEntity.GetComponent<YourComponent>();
    System.Console.WriteLine($"Component value: {component.Value}");
}
```

### Step 3: System Integration Test
1. **Add debug output** to your system's Update method
2. **Create test entities** in your scene
3. **Verify system processes entities** correctly
4. **Check event flow** if using events

### Step 4: Performance Testing
- **Monitor frame rate** with your system active
- **Check entity count** - use `entityManager.GetEntityCount()`
- **Profile system execution time** if needed

### Debug Tips
```csharp
// Add to your system for debugging
public void Update(GameTime gameTime)
{
    var entities = _entityManager.GetEntitiesWith<YourComponent>();
    System.Console.WriteLine($"Processing {entities.Count()} entities with YourComponent");
    
    foreach (var entity in entities)
    {
        System.Console.WriteLine($"Entity {entity.Id} - Component data: {entity.GetComponent<YourComponent>().Value}");
    }
}
```

---

## 📚 Reference Architecture

### System Execution Order (Gameplay Scene)
```
1. ComponentInputSystem       - Process input → events
2. ComponentMovementSystem    - Apply movement + tile collision
3. ComponentCollisionSystem   - Entity collision detection
4. InventorySystem           - Inventory management
5. YourCustomSystem          - Your system here
6. ComponentRenderSystem     - Draw everything
```

### Common Patterns
- **Query entities** with `GetEntitiesWith<Component1, Component2>()`
- **Modify components** with `ref var comp = ref entity.GetComponent<T>()`
- **Send events** for cross-system communication
- **Subscribe/unsubscribe** to events in Initialize/Shutdown
- **Use factory methods** for complex entity creation

### File Structure
```
PrisonBreak/
├── ECS/
│   ├── Systems/
│   │   └── YourSystem.cs
│   ├── Components.cs        (Add components here)
│   └── EventSystem.cs       (Add events here)
├── Scenes/
│   ├── YourScene.cs
│   ├── SceneTypes.cs        (Add scene enum)
│   └── SceneManager.cs      (Register scene)
└── Systems/
    └── IGameSystem.cs       (Interface all systems implement)
```

This architecture provides flexibility while maintaining performance and organization. Follow these patterns for consistent, maintainable code.