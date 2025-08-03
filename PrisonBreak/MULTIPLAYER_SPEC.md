# Prison Break Multiplayer System Specification

## Overview

The Prison Break multiplayer system is a production-ready, authoritative multiplayer implementation built using **LiteNetLib** for networking and seamlessly integrated with the existing **Entity Component System (ECS)** architecture. This specification provides comprehensive documentation for understanding, maintaining, and extending the multiplayer system.

## Table of Contents

1. [Core Architecture](#core-architecture)
2. [Network Infrastructure](#network-infrastructure)
3. [Message System](#message-system)
4. [Authority Model](#authority-model)
5. [State Synchronization](#state-synchronization)
6. [ECS Integration](#ecs-integration)
7. [Lobby System](#lobby-system)
8. [Gameplay Synchronization](#gameplay-synchronization)
9. [Inventory System](#inventory-system)
10. [Performance Characteristics](#performance-characteristics)
11. [Error Handling](#error-handling)
12. [Development Guide](#development-guide)
13. [Troubleshooting](#troubleshooting)

---

## Core Architecture

### Design Philosophy

The multiplayer system follows these core principles:

- **Authoritative Host Model**: The host maintains game state authority to prevent cheating and ensure consistency
- **ECS Integration**: Seamless integration with existing Entity Component System without breaking single-player functionality
- **Event-Driven Design**: Leverages the existing `EventBus` system for clean communication between systems
- **State Synchronization**: Uses complete state transmission instead of operation replay for reliability
- **Clean Separation**: Multiplayer code is additive and doesn't break existing single-player functionality

### Project Structure

```
PrisonBreak.Multiplayer/           # Pure networking library
├── Core/
│   ├── NetworkConfig.cs           # Network constants and enums
│   ├── NetworkClient.cs           # Client connection management
│   └── NetworkServer.cs           # Server connection management
└── Messages/
    └── NetworkMessage.cs          # Base LiteNetLib interfaces

PrisonBreak/                       # Game integration
├── Managers/
│   └── NetworkManager.cs          # Main networking coordinator
├── Core/Networking/
│   ├── ComponentMessages.cs      # ECS component network messages
│   └── InteractionMessages.cs    # Interaction-specific messages
├── ECS/Systems/
│   ├── NetworkSyncSystem.cs      # Player position synchronization
│   ├── NetworkAISystem.cs        # AI behavior synchronization
│   └── NetworkInventorySystem.cs # Inventory networking system
├── Scenes/
│   └── MultiplayerLobbyScene.cs  # Complete lobby implementation
└── ECS/Components.cs              # Enhanced with NetworkComponent
```

---

## Network Infrastructure

### LiteNetLib Integration

The system uses **LiteNetLib** as the underlying networking library, providing:

- **UDP-based networking** with optional reliability
- **Built-in serialization** for network messages
- **Connection management** with automatic discovery
- **Cross-platform support** for Windows, macOS, and Linux

### Connection Flow

```
1. Host starts server in MultiplayerLobbyScene
2. Client connects via IP address or local discovery
3. NetworkManager establishes bidirectional communication
4. Game state synchronization begins upon lobby exit
5. Real-time updates maintain state consistency
```

### NetworkManager

The `NetworkManager` serves as the central coordinator for all networking operations:

```csharp
public class NetworkManager : IDisposable
{
    // Core networking
    public bool IsHost { get; private set; }
    public bool IsConnected { get; private set; }
    
    // Host operations
    public void StartHost(int port)
    public void StopHost()
    
    // Client operations  
    public void ConnectToHost(string address, int port)
    public void Disconnect()
    
    // Message handling
    public void SendMessage<T>(T message) where T : class, INetworkMessage
    public void BroadcastMessage<T>(T message) where T : class, INetworkMessage
    
    // Game integration
    public void SetEntityManager(ComponentEntityManager entityManager)
    public void Update(GameTime gameTime)
}
```

---

## Message System

### Message Types

The system uses specialized message types for different game systems:

#### Core Messages
- **`TransformMessage`**: Player position and rotation updates (20Hz)
- **`PlayerInputMessage`**: Input state synchronization
- **`EntitySpawnMessage`**: Network entity creation

#### AI System Messages
- **`AIStateMessage`**: AI cop behavior and position sync (10Hz)

#### Interaction Messages
- **`InteractionRequestMessage`**: Client → Host item pickup requests
- **`ItemPickupMessage`**: Host → Clients authoritative pickup results
- **`ChestInteractionMessage`**: Complete chest inventory state synchronization

#### Collision Messages
- **`CollisionMessage`**: Authoritative collision result broadcasting

### Message Serialization

All messages implement `INetworkMessage` and use LiteNetLib's built-in serialization:

```csharp
public class TransformMessage : INetworkMessage
{
    public int NetworkId { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Rotation { get; set; }
    
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(NetworkId);
        writer.Put(X);
        writer.Put(Y);
        writer.Put(Rotation);
    }
    
    public void Deserialize(NetDataReader reader)
    {
        NetworkId = reader.GetInt();
        X = reader.GetFloat();
        Y = reader.GetFloat();
        Rotation = reader.GetFloat();
    }
}
```

### Message Routing

The `NetworkManager` handles message routing based on host/client role:

- **Host**: Receives client messages, processes authoritatively, broadcasts results
- **Client**: Sends requests to host, applies authoritative responses
- **Broadcast**: Host distributes state updates to all connected clients

---

## Authority Model

### Host Authority

The **host maintains authority** over all critical game operations:

- **Item Pickups**: Host validates pickup requests and prevents duplication
- **Inventory Transfers**: Host processes chest operations and broadcasts complete state
- **AI Behavior**: Host controls AI movement and broadcasts to clients
- **Collision Detection**: Host handles player-cop collisions authoritatively
- **Entity Spawning**: Host creates network entities and assigns IDs

### Client Responsibilities

Clients handle:

- **Input Collection**: Gather local player input
- **State Application**: Apply authoritative updates from host
- **Prediction**: Local input prediction for responsiveness
- **UI Updates**: Visual representation of synchronized state

### Authority Validation

```csharp
// Example: Host-side pickup validation
public bool ProcessItemPickup(int playerId, int itemNetworkId)
{
    // Validate player exists and is in range
    var player = FindPlayerById(playerId);
    var item = FindItemById(itemNetworkId);
    
    if (player == null || item == null) return false;
    if (Vector2.Distance(player.Position, item.Position) > PICKUP_RANGE) return false;
    
    // Check inventory space
    var inventory = player.GetComponent<InventoryComponent>();
    if (inventory.ItemCount >= inventory.MaxSlots) return false;
    
    // Authorize pickup
    return true;
}
```

---

## State Synchronization

### Synchronization Patterns

The system uses **complete state synchronization** for reliability:

#### Position Updates (20Hz)
```csharp
// Continuous position broadcasting for smooth movement
public void BroadcastTransform(Entity playerEntity)
{
    var transform = playerEntity.GetComponent<TransformComponent>();
    var networkComp = playerEntity.GetComponent<NetworkComponent>();
    
    var message = new TransformMessage
    {
        NetworkId = networkComp.NetworkId,
        X = transform.Position.X,
        Y = transform.Position.Y,
        Rotation = transform.Rotation
    };
    
    BroadcastMessage(message);
}
```

#### Inventory State Sync
```csharp
// Complete inventory arrays instead of operation deltas
public void SerializeInventoryStates(Entity player, Entity chest, ChestInteractionMessage message)
{
    if (player.HasComponent<InventoryComponent>())
    {
        var playerInventory = player.GetComponent<InventoryComponent>();
        message.PlayerInventoryItems = new string[playerInventory.MaxSlots];
        
        for (int i = 0; i < playerInventory.MaxSlots; i++)
        {
            var item = playerInventory.Items[i];
            message.PlayerInventoryItems[i] = item?.GetComponent<ItemComponent>()?.ItemId;
        }
    }
}
```

### Update Frequencies

- **Player Positions**: 20Hz for smooth movement
- **AI States**: 10Hz for performance balance
- **Inventory Operations**: Event-driven (immediate)
- **Collision Events**: Event-driven (immediate)

---

## ECS Integration

### NetworkComponent

Entities requiring network synchronization use the `NetworkComponent`:

```csharp
public struct NetworkComponent
{
    public int NetworkId;              // Unique network identifier
    public NetworkAuthority Authority; // Client/Server authority designation
    public bool SyncTransform;         // Position synchronization flag
    public bool SyncMovement;          // Movement synchronization flag
    public bool SyncInventory;         // Inventory synchronization flag
    public int OwnerId;                // Entity ownership identifier
}
```

### System Integration

Network systems integrate seamlessly with existing ECS systems:

```csharp
// NetworkSyncSystem works alongside existing systems
public class NetworkSyncSystem : ComponentSystem
{
    private NetworkManager _networkManager;
    
    public override void Update(GameTime gameTime)
    {
        if (!_networkManager.IsConnected) return;
        
        // Broadcast local player state
        var localPlayers = GetEntitiesWith<NetworkComponent, TransformComponent, PlayerInputComponent>();
        foreach (var player in localPlayers)
        {
            if (_networkManager.IsHost || IsLocalPlayer(player))
            {
                BroadcastTransform(player);
            }
        }
        
        // Apply remote updates (handled via message callbacks)
    }
}
```

### Event System Integration

The multiplayer system leverages the existing `EventBus` for clean integration:

```csharp
// Events fire normally, UI systems respond automatically
EventBus.Publish(new ItemAddedEvent 
{ 
    PlayerEntity = playerEntity, 
    ItemEntity = itemEntity, 
    SlotIndex = slotIndex 
});

// InventoryUIRenderSystem receives and handles the event
public void OnItemAdded(ItemAddedEvent evt)
{
    var slotUI = _inventorySlots[evt.SlotIndex];
    slotUI.ContainedItem = evt.ItemEntity;
    // Visual update happens automatically
}
```

---

## Lobby System

### MultiplayerLobbyScene

The lobby provides a complete multiplayer setup interface:

```csharp
public class MultiplayerLobbyScene : Scene
{
    // Core lobby functionality
    private void HandleHostGame()        // Start hosting a game
    private void HandleJoinGame()        // Join an existing game
    private void HandleCharacterSelection() // Player type selection
    private void HandleReadySystem()     // Ready-up state management
    private void HandleGameStart()       // Transition to gameplay
}
```

### Lobby Flow

1. **Host Game**: Create server and wait for connections
2. **Join Game**: Connect to host via IP address
3. **Character Selection**: Choose Prisoner or Cop independently
4. **Ready Up**: All players indicate readiness
5. **Game Start**: Host initiates transition to `GameplayScene`

### Character Selection

Players can independently select character types:

- **Prisoner**: 3 inventory slots, faster movement
- **Cop**: 4 inventory slots, standard movement
- **Visual Feedback**: Real-time character type display
- **Host Authority**: Host controls when game starts

---

## Gameplay Synchronization

### Player Movement

```csharp
// 20Hz position synchronization for smooth movement
public class NetworkSyncSystem : ComponentSystem
{
    private float _syncTimer = 0f;
    private const float SYNC_INTERVAL = 1f / 20f; // 20Hz
    
    public override void Update(GameTime gameTime)
    {
        _syncTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        
        if (_syncTimer >= SYNC_INTERVAL)
        {
            BroadcastPlayerPositions();
            _syncTimer = 0f;
        }
    }
}
```

### AI Synchronization

```csharp
// 10Hz AI state synchronization
public class NetworkAISystem : ComponentSystem
{
    public void BroadcastAIState(Entity aiEntity)
    {
        var transform = aiEntity.GetComponent<TransformComponent>();
        var ai = aiEntity.GetComponent<AIComponent>();
        var network = aiEntity.GetComponent<NetworkComponent>();
        
        var message = new AIStateMessage
        {
            NetworkId = network.NetworkId,
            Position = transform.Position,
            State = ai.CurrentState,
            PatrolIndex = ai.PatrolPointIndex,
            IsPatrolling = ai.IsPatrolling
        };
        
        _networkManager.BroadcastMessage(message);
    }
}
```

### Collision Handling

```csharp
// Authoritative collision processing
public void ProcessPlayerCopCollision(Entity player, Entity cop)
{
    if (!_networkManager.IsHost) return; // Only host processes
    
    // Reset cop to spawn position
    var copTransform = cop.GetComponent<TransformComponent>();
    copTransform.Position = cop.GetComponent<AIComponent>().SpawnPosition;
    
    // Broadcast collision result
    var message = new CollisionMessage
    {
        PlayerNetworkId = player.GetComponent<NetworkComponent>().NetworkId,
        CopNetworkId = cop.GetComponent<NetworkComponent>().NetworkId,
        CopNewPosition = copTransform.Position
    };
    
    _networkManager.BroadcastMessage(message);
}
```

---

## Inventory System

### Authoritative Item Pickups

The inventory system prevents item duplication through host authority:

```csharp
// Client request → Host validation → Broadcast result
public void RequestItemPickup(Entity itemEntity)
{
    var message = new InteractionRequestMessage
    {
        PlayerId = GetLocalPlayerId(),
        TargetNetworkId = itemEntity.GetComponent<NetworkComponent>().NetworkId,
        InteractionType = "pickup",
        PlayerPosition = GetLocalPlayerPosition()
    };
    
    _networkManager.SendMessage(message); // Client → Host
}

// Host processes and responds
public void ProcessInteractionRequest(InteractionRequestMessage message)
{
    // Validate request
    bool success = ValidatePickupRequest(message);
    
    // Create response
    var response = new ItemPickupMessage
    {
        PlayerId = message.PlayerId,
        Success = success,
        // ... other fields
    };
    
    _networkManager.BroadcastMessage(response); // Host → All clients
}
```

### Chest Transfer System

Chest operations use complete state synchronization:

```csharp
public class ChestInteractionMessage : INetworkMessage
{
    public string PlayerId { get; set; }
    public int ChestNetworkId { get; set; }
    public string Action { get; set; }        // "transfer_to_chest", "transfer_to_player"
    public int SlotIndex { get; set; }
    public bool Success { get; set; }
    
    // Complete state arrays for reliability
    public string[] PlayerInventoryItems { get; set; }  // Full player inventory
    public string[] ChestInventoryItems { get; set; }   // Full chest inventory
}
```

### State Application

```csharp
// Client applies complete inventory state from host
public void ApplyInventoryStates(ChestInteractionMessage message)
{
    var player = FindPlayerById(message.PlayerId);
    var chest = FindEntityByNetworkId(message.ChestNetworkId);
    
    // Apply player inventory state
    if (player != null && message.PlayerInventoryItems != null)
    {
        var inventory = player.GetComponent<InventoryComponent>();
        
        for (int i = 0; i < inventory.MaxSlots; i++)
        {
            // Remove old item
            if (inventory.Items[i] != null)
            {
                EventBus.Publish(new ItemRemovedEvent { PlayerEntity = player, SlotIndex = i });
                inventory.Items[i] = null;
                inventory.ItemCount--;
            }
            
            // Add new item if specified
            if (i < message.PlayerInventoryItems.Length && !string.IsNullOrEmpty(message.PlayerInventoryItems[i]))
            {
                var newItem = _entityManager.CreateItem(message.PlayerInventoryItems[i]);
                inventory.Items[i] = newItem;
                inventory.ItemCount++;
                EventBus.Publish(new ItemAddedEvent { PlayerEntity = player, ItemEntity = newItem, SlotIndex = i });
            }
        }
    }
}
```

---

## Performance Characteristics

### Network Performance

- **Bandwidth Usage**: < 1KB/s per player for typical gameplay
- **Update Rates**: 20Hz positions, 10Hz AI, event-driven interactions
- **Latency Targets**: < 100ms LAN, < 200ms internet
- **Player Capacity**: Tested with 2-4 players, scalable to 8+

### Memory Management

```csharp
// Efficient entity ID mapping
private Dictionary<int, Entity> _networkEntities = new Dictionary<int, Entity>();
private int _nextNetworkId = 1000; // Starting range for network entities

// Component pooling for frequent messages
private readonly Queue<TransformMessage> _transformMessagePool = new Queue<TransformMessage>();
```

### Optimization Strategies

1. **Message Batching**: Group multiple updates into single packets
2. **Adaptive Updates**: Reduce frequency for distant/static entities
3. **Component Filtering**: Only sync necessary components
4. **State Compression**: Use efficient serialization for large states

---

## Error Handling

### Connection Management

```csharp
public void HandleConnectionError(NetPeer peer, SocketError error)
{
    Logger.Error($"Connection error with {peer.EndPoint}: {error}");
    
    if (IsHost)
    {
        RemovePlayer(peer);
        NotifyClientsOfDisconnection(peer);
    }
    else
    {
        ReturnToLobby("Connection lost to host");
    }
}
```

### State Validation

```csharp
public bool ValidateTransformMessage(TransformMessage message)
{
    // Sanity checks for position updates
    if (Math.Abs(message.X) > MAX_WORLD_COORDINATE) return false;
    if (Math.Abs(message.Y) > MAX_WORLD_COORDINATE) return false;
    if (message.NetworkId <= 0) return false;
    
    return true;
}
```

### Graceful Degradation

- **Timeout Handling**: Automatic disconnection after timeout
- **Partial Message Recovery**: Skip invalid messages without crashing
- **State Restoration**: Host can resend full state to recovering clients
- **Fallback to Single-Player**: Seamless transition when multiplayer fails

---

## Development Guide

### Adding New Networked Features

1. **Define Message Type**:
```csharp
public class NewFeatureMessage : INetworkMessage
{
    public int NetworkId { get; set; }
    // ... feature-specific fields
    
    public void Serialize(NetDataWriter writer) { /* ... */ }
    public void Deserialize(NetDataReader reader) { /* ... */ }
}
```

2. **Add Message Handling**:
```csharp
// In NetworkManager.DeserializeGameMessage()
case "NewFeatureMessage":
    var newFeatureMsg = new NewFeatureMessage();
    newFeatureMsg.Deserialize(reader);
    HandleNewFeatureMessage(newFeatureMsg);
    break;
```

3. **Implement Authority Logic**:
```csharp
// Host-side processing
public void ProcessNewFeature(NewFeatureMessage message)
{
    // Validate and process
    bool success = ValidateNewFeature(message);
    
    // Broadcast result
    if (success)
    {
        BroadcastMessage(message);
    }
}
```

4. **Integrate with ECS**:
```csharp
// Create corresponding system
public class NetworkNewFeatureSystem : ComponentSystem
{
    public override void Update(GameTime gameTime)
    {
        // Handle networked feature updates
    }
}
```

### Testing Multiplayer Features

1. **Local Testing**: Use `localhost` connections for basic functionality
2. **Network Simulation**: Add artificial latency and packet loss
3. **Multi-Instance Testing**: Run multiple game instances simultaneously
4. **Edge Case Testing**: Disconnections, timeouts, invalid messages

### Debugging Network Issues

```csharp
// Enable network debugging
public static class NetworkDebug
{
    public static bool LogMessages = false;
    public static bool LogConnections = true;
    public static bool LogErrors = true;
    
    public static void LogMessage<T>(T message, string direction) where T : INetworkMessage
    {
        if (LogMessages)
        {
            Console.WriteLine($"[NET {direction}] {typeof(T).Name}: {JsonConvert.SerializeObject(message)}");
        }
    }
}
```

---

## Troubleshooting

### Common Issues

#### "Network ID conflicts"
- **Cause**: Multiple entities assigned same network ID
- **Solution**: Use proper ID ranges (players: 1000+, items: 2000+, AI: 1001-1002)

#### "Client can't pick up items"
- **Cause**: Missing message deserialization
- **Solution**: Add new message types to `DeserializeGameMessage()` switch

#### "Inventory UI not updating"
- **Cause**: Event firing missing after state changes
- **Solution**: Ensure `ItemAddedEvent`/`ItemRemovedEvent` fire after inventory modifications

#### "Host operations bypass network system"
- **Cause**: Host taking shortcut paths
- **Solution**: Route host operations through same network systems as clients

### Diagnostic Tools

```csharp
// Network entity inspection
public void InspectNetworkEntities()
{
    foreach (var kvp in _networkEntities)
    {
        var entity = kvp.Value;
        var networkComp = entity.GetComponent<NetworkComponent>();
        
        Console.WriteLine($"Entity {kvp.Key}: Authority={networkComp.Authority}, Owner={networkComp.OwnerId}");
    }
}

// Message frequency monitoring
private Dictionary<Type, int> _messageFrequency = new Dictionary<Type, int>();

public void TrackMessage<T>(T message) where T : INetworkMessage
{
    var type = typeof(T);
    _messageFrequency[type] = _messageFrequency.GetValueOrDefault(type, 0) + 1;
}
```

### Performance Monitoring

```csharp
// Network performance metrics
public class NetworkMetrics
{
    public float AverageLatency { get; private set; }
    public int MessagesPerSecond { get; private set; }
    public float PacketLoss { get; private set; }
    public int ConnectedPlayers { get; private set; }
    
    public void UpdateMetrics()
    {
        // Calculate and update metrics
    }
}
```

---

## Conclusion

The Prison Break multiplayer system provides a robust, production-ready foundation for real-time multiplayer gaming. Its authoritative architecture ensures security and consistency, while the ECS integration maintains clean separation of concerns. The complete state synchronization approach prioritizes reliability over bandwidth optimization, making it ideal for small-to-medium scale multiplayer games.

Key strengths:
- **Authoritative host model** prevents cheating
- **Complete ECS integration** without breaking existing systems
- **Event-driven architecture** for clean component communication
- **State synchronization** for maximum reliability
- **Comprehensive error handling** for production stability

This specification serves as both a reference for understanding the current implementation and a guide for future development and maintenance.