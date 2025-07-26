# Phase 1: Core Networking Infrastructure - Implementation Guide

## Overview

Phase 1 establishes the foundational networking layer using LiteNetLib that seamlessly integrates with the existing ECS architecture. This phase focuses on creating a consistent, extensible networking foundation that respects current architectural patterns.

---

## Architectural Integration Strategy

### 🏗️ Consistency with Existing Architecture

The networking implementation follows these established patterns from the current codebase:

#### **Component Pattern Consistency**
- **Current Pattern**: Pure data structs (e.g., `TransformComponent`, `MovementComponent`)
- **Network Integration**: `NetworkComponent` follows same struct pattern
- **Message Design**: Network messages wrap existing component data directly

#### **System Pattern Consistency** 
- **Current Pattern**: Systems implement `IGameSystem` interface with lifecycle methods
- **Network Integration**: `NetworkSyncSystem`, `NetworkEventSystem` implement `IGameSystem`
- **Lifecycle Management**: Integrated with existing `SystemManager`

#### **Event Pattern Consistency**
- **Current Pattern**: `EventBus` for system communication with Subscribe/Send pattern
- **Network Integration**: `NetworkEventBus` extends (not replaces) existing EventBus
- **Event Flow**: Network events integrate seamlessly with existing event handlers

#### **Scene Pattern Consistency**
- **Current Pattern**: Scene-based state management with transition events
- **Network Integration**: Network state tied to scene lifecycle
- **State Management**: Network connections managed per scene transition

---

## 1.1 Project Structure Setup ✅ IMPLEMENTED

### Folder Organization
```
PrisonBreak.Multiplayer/        # Pure networking library
├── Core/
│   └── NetworkConfig.cs             # Network constants and enums ✅
└── Messages/
    └── NetworkMessage.cs            # Base LiteNetLib interfaces ✅

PrisonBreak/                     # Game integration
├── Managers/
│   └── NetworkManager.cs           # Central coordinator ✅
├── ECS/
│   └── Components.cs                # Contains NetworkComponent ✅
└── Core/
    └── Networking/
        └── ComponentMessages.cs    # Game-specific network messages ✅

Future Implementation (Phase 2+):
PrisonBreak.Multiplayer/
├── Core/
│   ├── NetworkClient.cs         # Client connection management ⏳
│   ├── NetworkServer.cs         # Server connection management ⏳  
│   └── NetworkEntityMapper.cs   # Entity ID mapping ⏳
└── Utilities/
    ├── NetworkSerializer.cs     # Serialization helpers ⏳
    └── ConnectionManager.cs     # Connection state management ⏳
```

**Architectural Note**: Clean separation of concerns - pure networking library vs. game integration. This eliminates circular dependencies and allows the networking library to be reused by other projects.

---

## 1.2 Core Networking Classes ✅ IMPLEMENTED

### NetworkManager.cs
**Location**: `PrisonBreak/Managers/NetworkManager.cs`  
**Namespace**: `PrisonBreak.Managers`  
**Purpose**: Central networking coordinator that integrates with existing game lifecycle

**Integration Points**:
- Implements `IGameSystem` for consistent lifecycle management
- Uses existing `EventBus` for network status events
- Integrates with `SceneManager` for network state transitions

**Key Responsibilities**:
```csharp
namespace PrisonBreak.Managers;

public class NetworkManager : IGameSystem
{
    private readonly EventBus _eventBus;           // Existing event system
    private readonly ComponentEntityManager _entityManager; // Existing entity system
    
    // Server/Client state management
    // Connection lifecycle
    // Integration with SystemManager
}
```

**Architectural Consistency**:
- Follows existing system initialization pattern
- Uses constructor dependency injection like other systems
- Integrates with existing error handling patterns

### NetworkClient.cs
**Purpose**: Client-side connection management with local discovery

**Integration Points**:
- Uses existing `InputManager` patterns for network input
- Integrates with current logging/error handling
- Follows existing async patterns from the codebase

**Key Features**:
- Local network discovery for LAN games
- Connection to remote hosts
- Message queuing and sending
- Reconnection handling

### NetworkServer.cs  
**Purpose**: Server-side connection and authority management

**Integration Points**:
- Uses existing entity authority patterns
- Integrates with current collision detection for validation
- Follows existing component modification patterns

**Key Features**:
- Client connection management
- Message broadcasting
- Authority validation
- Anti-cheat validation

### NetworkConfig.cs
**Purpose**: Centralized network configuration

**Consistency Pattern**: Follows existing `GameConfig.cs` and `EntityConfig.cs` patterns

```csharp
public static class NetworkConfig
{
    // Network settings
    public const int DefaultPort = 7777;
    public const int MaxPlayers = 8;
    public const float NetworkTickRate = 20f; // Hz
    
    // Message types (enum like existing AIBehavior)
    public enum MessageType : byte
    {
        Transform,
        Movement, 
        Inventory,
        // etc.
    }
}
```

---

## 1.3 Message System Integration ✅ IMPLEMENTED

### NetworkMessage.cs
**Purpose**: Base interface for all network messages using LiteNetLib serialization

**Architectural Integration**:
- Implements `INetSerializable` from LiteNetLib
- Follows existing interface patterns from the project
- Uses existing component data structures directly

```csharp
public interface INetworkMessage : INetSerializable
{
    MessageType Type { get; }
    int EntityId { get; }
}

public abstract class NetworkMessage : INetworkMessage
{
    // Base implementation following existing abstract class patterns
}
```

### ComponentMessages.cs  
**Location**: `PrisonBreak/Core/Networking/ComponentMessages.cs`  
**Namespace**: `PrisonBreak.Core.Networking`  
**Purpose**: Network messages for each ECS component

**Consistency Strategy**: Direct mapping to existing components

```csharp
namespace PrisonBreak.Core.Networking;

// Transform network message wraps existing TransformComponent
public class TransformMessage : NetworkMessage
{
    public Vector2 Position;
    public float Rotation;
    public Vector2 Scale;
    
    public TransformMessage(int entityId, TransformComponent transform) 
        : base(NetworkConfig.MessageType.Transform, entityId)
    {
        Position = transform.Position;
        Rotation = transform.Rotation;
        Scale = transform.Scale;
    }
    
    public TransformComponent ToComponent()
    {
        return new TransformComponent(Position)
        {
            Rotation = Rotation,
            Scale = Scale
        };
    }
}

// Similar pattern for MovementMessage, PlayerInputMessage...
```

**Architectural Benefits**:
- No duplication of component data structures
- Automatic consistency with component changes
- Reuses existing component validation logic

### MessageHandler.cs
**Purpose**: Routes network messages to appropriate systems

**Integration Strategy**: Extends existing EventBus pattern

```csharp
public class MessageHandler
{
    private readonly EventBus _eventBus;  // Existing event system
    private readonly Dictionary<MessageType, Action<NetworkMessage>> _handlers;
    
    public void RouteMessage(NetworkMessage message)
    {
        // Convert network message to existing event types
        // Send through existing EventBus
        // Leverage existing event validation
    }
}
```

---

## 1.4 ECS Integration ✅ IMPLEMENTED

### NetworkComponent.cs
**Location**: `PrisonBreak/ECS/Components.cs` (integrated with other components)  
**Namespace**: `PrisonBreak.ECS`  
**Purpose**: Component marking entities for network synchronization

**Consistency Pattern**: Follows existing component struct pattern

```csharp
namespace PrisonBreak.ECS;

public struct NetworkComponent
{
    public int NetworkId;              // Network entity ID
    public NetworkConfig.NetworkAuthority Authority; // Who controls this entity
    public bool SyncTransform;         // Sync position/rotation
    public bool SyncMovement;          // Sync velocity/physics
    public bool SyncInventory;         // Sync inventory changes
    public float LastSyncTime;         // For interpolation
    public int OwnerId;                // Which player owns this entity
    
    public NetworkComponent(int networkId, NetworkConfig.NetworkAuthority authority, int ownerId = -1)
    {
        NetworkId = networkId;
        Authority = authority;
        SyncTransform = true;
        SyncMovement = true;
        SyncInventory = true;
        LastSyncTime = 0f;
        OwnerId = ownerId;
    }
}

// NetworkAuthority enum is in NetworkConfig.cs
```

**Integration Benefits**:
- Uses existing component query patterns: `GetEntitiesWith<NetworkComponent>()`
- Follows existing component modification patterns: `ref var network = ref entity.GetComponent<NetworkComponent>()`
- Compatible with existing component serialization

### NetworkEntityMapper.cs
**Purpose**: Maps local entity IDs to network IDs

**Integration Strategy**: Works with existing `ComponentEntityManager`

```csharp
public class NetworkEntityMapper
{
    private readonly ComponentEntityManager _entityManager; // Existing entity system
    private readonly Dictionary<int, int> _localToNetwork;
    private readonly Dictionary<int, int> _networkToLocal;
    
    public Entity GetLocalEntity(int networkId)
    {
        // Uses existing entity retrieval patterns
        return _entityManager.GetEntity(_networkToLocal[networkId]);
    }
    
    public void RegisterNetworkEntity(Entity localEntity, int networkId)
    {
        // Adds NetworkComponent using existing patterns
        localEntity.AddComponent(new NetworkComponent(networkId, NetworkAuthority.Server));
    }
}
```

---

## 1.5 System Integration ⏳ PARTIALLY IMPLEMENTED

### NetworkSyncSystem.cs ⏳ FUTURE
**Location**: `PrisonBreak/ECS/Systems/NetworkSyncSystem.cs` (Phase 2)  
**Purpose**: Core network synchronization system

**Integration Pattern**: Implements existing `IGameSystem` interface

```csharp
namespace PrisonBreak.ECS.Systems;

public class NetworkSyncSystem : IGameSystem
{
    private readonly ComponentEntityManager _entityManager; // Existing
    private readonly EventBus _eventBus;                   // Existing
    private readonly NetworkManager _networkManager;        // From Managers namespace
    
    public void Initialize()
    {
        // Subscribe to existing events
        _eventBus.Subscribe<PlayerInputEvent>(OnPlayerInput);
        _eventBus.Subscribe<EntityCollisionEvent>(OnCollision);
    }
    
    public void Update(GameTime gameTime)
    {
        // Use existing component query patterns
        var networkEntities = _entityManager.GetEntitiesWith<NetworkComponent, TransformComponent>();
        
        foreach (var entity in networkEntities)
        {
            // Use existing component access patterns
            ref var networkComp = ref entity.GetComponent<NetworkComponent>();
            ref var transform = ref entity.GetComponent<TransformComponent>();
            
            // Network synchronization logic
        }
    }
    
    public void Draw(SpriteBatch spriteBatch) { /* No rendering */ }
    public void Shutdown() { /* Cleanup */ }
}
```

**SystemManager Integration**:
```csharp
// In GameplayScene.cs - NetworkManager is added to system manager
systemManager.AddSystem(new NetworkManager(eventBus, entityManager));
```

### NetworkEventSystem.cs
**Purpose**: Distributes events across network

**Integration Strategy**: Extends existing event patterns

```csharp
public class NetworkEventSystem : IGameSystem
{
    private readonly EventBus _localEventBus;    // Existing local events
    private readonly NetworkEventBus _networkEventBus; // New network events
    
    public void Initialize()
    {
        // Bridge local events to network when needed
        _localEventBus.Subscribe<PlayerInputEvent>(BridgeToNetwork);
        _localEventBus.Subscribe<InventoryChangedEvent>(BridgeToNetwork);
    }
    
    private void BridgeToNetwork<T>(T eventData) where T : struct
    {
        // Determine if event should be networked
        // Send via network if appropriate
        // Maintain existing event handler compatibility
    }
}
```

---

## 1.6 Event System Extension

### NetworkEventBus.cs
**Purpose**: Extends existing EventBus for network distribution

**Integration Strategy**: Composition over inheritance to maintain compatibility

```csharp
public class NetworkEventBus
{
    private readonly EventBus _localEventBus;     // Existing event bus
    private readonly NetworkManager _networkManager; // Network layer
    
    public NetworkEventBus(EventBus localEventBus, NetworkManager networkManager)
    {
        _localEventBus = localEventBus;
        _networkManager = networkManager;
    }
    
    // Delegate to existing EventBus for local events
    public void Subscribe<T>(Action<T> handler) => _localEventBus.Subscribe(handler);
    public void Unsubscribe<T>(Action<T> handler) => _localEventBus.Unsubscribe(handler);
    
    // New method for network events
    public void SendNetworked<T>(T eventData) where T : struct
    {
        // Send locally using existing system
        _localEventBus.Send(eventData);
        
        // Also send via network if appropriate
        if (ShouldNetworkEvent<T>())
        {
            _networkManager.BroadcastEvent(eventData);
        }
    }
}
```

**Migration Strategy**: Existing code continues to work unchanged, new network features are opt-in

---

## 1.7 Implementation Priority Order

### Step 1: Core Infrastructure (Day 1-2) ✅ COMPLETE
1. **NetworkConfig.cs** - Basic constants and enums ✅
2. **NetworkComponent.cs** - ECS integration component ✅
3. **NetworkMessage.cs** - Base message interface ✅
4. **NetworkManager.cs** - Basic connection management ✅

### Step 2: Message System (Day 2-3) ✅ COMPLETE  
1. **ComponentMessages.cs** - Transform and Movement messages ✅
2. **MessageHandler.cs** - Basic routing ⏳ (TODO in NetworkManager)
3. **NetworkSerializer.cs** - Serialization utilities ⏳ (TODO)

### Step 3: Entity Integration (Day 3-4)
1. **NetworkEntityMapper.cs** - Entity ID mapping
2. **NetworkSyncSystem.cs** - Basic sync system
3. **Integration testing** with existing components

### Step 4: Event Integration (Day 4-5)
1. **NetworkEventBus.cs** - Event system extension
2. **NetworkEventSystem.cs** - Event distribution
3. **End-to-end testing** with existing event handlers

---

## 1.8 Testing Strategy

### Unit Testing Approach
- Test network components using existing component patterns
- Mock existing dependencies (`EventBus`, `ComponentEntityManager`)
- Validate message serialization/deserialization

### Integration Testing Approach  
- Test with existing game systems running
- Validate compatibility with current event flows
- Ensure no performance regression in single-player

### Local Multiplayer Testing
- Host/client on same machine
- Test with existing player input systems
- Validate component synchronization

---

## 1.9 Architectural Benefits

### Maintains Existing Patterns
- **Component-based**: Network features are additive components
- **Event-driven**: Network events integrate with existing event flow
- **System-based**: Network systems follow existing lifecycle patterns
- **Scene-based**: Network state managed per scene

### Future-Proof Design
- **Extensible**: Easy to add new component messages
- **Scalable**: Foundation supports lobby, game sync, and advanced features
- **Maintainable**: Follows established code organization
- **Testable**: Each layer can be tested independently

### Developer Experience
- **Familiar Patterns**: Uses existing architectural concepts
- **Minimal Learning Curve**: Network APIs mirror existing APIs
- **Incremental Adoption**: Can enable networking per-component
- **Debug-Friendly**: Network state visible through existing debug tools

---

## 1.10 Next Phase Preparation

This Phase 1 implementation provides the foundation for:

- **Phase 2**: Lobby system can use `NetworkEventBus` for player state
- **Phase 3**: Game synchronization can use `ComponentMessages` for real-time updates  
- **Phase 4**: Advanced features can extend existing message and event patterns
- **Phase 5**: Testing and optimization can leverage existing performance patterns

The architecture ensures that each subsequent phase builds naturally on established patterns while maintaining full compatibility with the existing single-player game.

---

## Implementation Notes (Added During Development)

### P2P Design Choice
Phase 1 implements **peer-to-peer (P2P) networking** rather than dedicated servers:
- **LocalHost**: One player acts as both server and client
- **Client**: Other players connect to the LocalHost player
- **Benefits**: Simpler to implement, test, and deploy for Phase 1
- **Future**: Dedicated server support can be added by extending `GameMode` enum and NetworkManager logic

### Design Decisions Made
1. **Concrete MessageType Property**: Used concrete implementation rather than abstract for simplicity
2. **Event Subscription Pattern**: NetworkManager always subscribes to events and filters by game mode
3. **Component Integration**: NetworkComponent only added to entities that need network sync (not all entities)
4. **LiteNetLib Integration**: Direct implementation of INetSerializable with custom base class
5. **Architecture Restructure**: Separated pure networking library from game integration to eliminate circular dependencies

### Architecture Restructure (Post-Implementation)
The initial design placed all networking code in `PrisonBreak.Multiplayer` project, but this created circular dependency issues since game integration classes needed access to ECS components and systems.

**Solution**: Clean separation of concerns
- **Pure Networking Library** (`PrisonBreak.Multiplayer`): Only LiteNetLib integration, no game dependencies
- **Game Integration** (`PrisonBreak`): All game-specific networking code that needs ECS access

**Benefits**:
- Eliminates circular dependencies
- Makes networking library reusable by other projects  
- Cleaner architecture with obvious separation of concerns
- Both projects build successfully without complex workarounds