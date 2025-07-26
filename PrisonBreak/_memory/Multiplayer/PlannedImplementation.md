# Multiplayer Implementation Plan for Prison Break Game

## 🎯 Project Goal
Implement real-time multiplayer functionality where:
- One player can **host a lobby**
- Multiple players can **join and select characters** (Prisoner/Cop)
- Players see each other **moving in real-time**
- **Item interactions** (pickup/container) are **synchronized** across all clients

## 🏗️ Architecture Overview

### Current Architecture Strengths
The existing codebase has **excellent foundations** for multiplayer:

- **✅ Scene-Based Architecture**: Clean separation between lobby/gameplay states
- **✅ Pure ECS Components**: Data structures perfect for network serialization
- **✅ Event-Driven Systems**: EventBus ideal for network message handling
- **✅ Player Type System**: Character selection (Prisoner/Cop) already implemented
- **✅ Inventory System**: Real-time inventory updates with visual UI

### Proposed Network Architecture: **Host-as-Server**

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│     HOST        │    │    CLIENT 1     │    │    CLIENT 2     │
│  (Player + SVR) │◄──►│   (Player)      │    │   (Player)      │
│                 │    │                 │    │                 │
│ • Game Logic    │    │ • Input Only    │    │ • Input Only    │
│ • Authority     │◄──►│ • Render State  │◄──►│ • Render State  │
│ • State Sync    │    │ • Receive Sync  │    │ • Receive Sync  │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

**Key Design Decisions:**
- **Host Authority**: Host manages all game logic and world state
- **Client Input**: Clients send input, receive state updates
- **Reliable UDP**: LiteNetLib for fast, reliable networking
- **Component Serialization**: Leverage existing ECS data structures

## 📦 Phase 1: Network Foundation

### 1.1 Add Dependencies
```xml
<!-- PrisonBreak.csproj -->
<PackageReference Include="LiteNetLib" Version="1.1.0" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
```

### 1.2 Create Core Network Systems

#### **NetworkManager.cs** - Central network coordination
```csharp
public class NetworkManager
{
    private NetManager _netManager;
    private bool _isHost;
    private List<NetPeer> _connectedPeers;
    
    // Host: Create lobby
    public bool StartHost(int port = 9050);
    
    // Client: Join lobby
    public bool ConnectToHost(string ip, int port = 9050);
    
    // Send data to all clients (host only)
    public void BroadcastToClients<T>(T data, DeliveryMethod method);
    
    // Send data to host (client only)
    public void SendToHost<T>(T data, DeliveryMethod method);
}
```

#### **NetworkMessageSystem.cs** - Handle incoming network messages
```csharp
public class NetworkMessageSystem : IGameSystem
{
    // Process incoming network messages
    // Convert network data → ECS events
    // Handle player join/leave events
    public void Update(GameTime gameTime);
}
```

#### **Message Types**
```csharp
[Serializable]
public class PlayerJoinMessage
{
    public int PlayerId;
    public string PlayerName;
    public PlayerType SelectedType;
}

[Serializable]
public class EntityStateMessage
{
    public int EntityId;
    public TransformComponent Transform;
    public MovementComponent Movement;
    public PlayerTypeComponent PlayerType;
}

[Serializable]
public class InventoryActionMessage
{
    public int PlayerId;
    public InventoryActionType Action; // Pickup, Drop, Transfer
    public int ItemId;
    public int SlotIndex;
}
```

### 1.3 Integration with Current Architecture

**EventBus Integration:**
```csharp
// Network events flow through existing EventBus
EventBus.Subscribe<NetworkPlayerJoinEvent>(OnNetworkPlayerJoin);
EventBus.Subscribe<NetworkEntityUpdateEvent>(OnNetworkEntityUpdate);
EventBus.Subscribe<NetworkInventoryActionEvent>(OnNetworkInventoryAction);

// Existing game events trigger network messages
EventBus.Subscribe<PlayerInputEvent>(OnPlayerInput); // Send to host
EventBus.Subscribe<ItemPickupEvent>(OnItemPickup);   // Broadcast to clients
```

## 📦 Phase 2: Lobby System

### 2.1 Create LobbyScene
New scene for multiplayer lobby management:

```csharp
public class LobbyScene : Scene
{
    // Host: Display lobby code, connected players, start game
    // Client: Show connection status, player selection
    
    private LobbyState _lobbyState;
    private Dictionary<int, LobbyPlayer> _connectedPlayers;
    
    protected override void SetupSystems()
    {
        // Add lobby-specific systems:
        // - LobbyInputSystem (ready/start/character selection)
        // - LobbyRenderSystem (player list, lobby info)
        // - NetworkMessageSystem (handle join/leave)
    }
}
```

### 2.2 Lobby Flow
```
┌─────────────────┐
│  Start Menu     │
└─────┬───────────┘
      │
      ▼
┌─────────────────┐     ┌─────────────────┐
│ Host Game       │ OR  │ Join Game       │
│ • Create Lobby  │     │ • Enter IP      │
│ • Wait Players  │     │ • Connect       │
└─────┬───────────┘     └─────┬───────────┘
      │                       │
      ▼                       ▼
┌─────────────────────────────────────────┐
│          Lobby Scene                    │
│ • Player List & Character Selection     │
│ • Ready/Start Coordination              │
│ • Host Authority for Game Start         │
└─────┬───────────────────────────────────┘
      │
      ▼
┌─────────────────┐
│ Multiplayer     │
│ Gameplay Scene  │
└─────────────────┘
```

### 2.3 Character Selection Integration
**Leverage existing PlayerType system:**
```csharp
// In LobbyScene - each player selects character type
public void OnCharacterSelection(int playerId, PlayerType selectedType)
{
    _connectedPlayers[playerId].SelectedType = selectedType;
    
    // Broadcast character selection to all players
    var message = new PlayerCharacterUpdateMessage 
    { 
        PlayerId = playerId, 
        SelectedType = selectedType 
    };
    _networkManager.BroadcastToClients(message, DeliveryMethod.ReliableOrdered);
}
```

## 📦 Phase 3: Entity State Synchronization

### 3.1 Add Network Components

#### **NetworkComponent** - Mark entities for network sync
```csharp
public struct NetworkComponent
{
    public int NetworkId;        // Unique across all clients
    public bool IsOwned;         // Does this client own this entity?
    public float SyncInterval;   // How often to sync (seconds)
    public float LastSyncTime;   // When last synced
}
```

#### **AuthorityComponent** - Define ownership
```csharp
public struct AuthorityComponent
{
    public int OwnerId;          // Which player/client owns this entity
    public bool IsServerOwned;   // Host has authority
}
```

### 3.2 Create Network Sync Systems

#### **NetworkSyncSystem** - Periodic state synchronization
```csharp
public class NetworkSyncSystem : IGameSystem
{
    public void Update(GameTime gameTime)
    {
        if (_networkManager.IsHost)
        {
            SyncEntitiesToClients(gameTime);
        }
        else
        {
            // Clients: Apply received state updates
            ApplyNetworkUpdates();
        }
    }
    
    private void SyncEntitiesToClients(GameTime gameTime)
    {
        // Get all network entities that need syncing
        var networkEntities = _entityManager.GetEntitiesWith<NetworkComponent, TransformComponent>();
        
        foreach (var entity in networkEntities)
        {
            var network = entity.GetComponent<NetworkComponent>();
            
            // Check if entity needs sync based on interval
            if (gameTime.TotalGameTime.TotalSeconds - network.LastSyncTime >= network.SyncInterval)
            {
                // Create state message
                var stateMsg = new EntityStateMessage
                {
                    EntityId = network.NetworkId,
                    Transform = entity.GetComponent<TransformComponent>(),
                };
                
                // Add optional components if they exist
                if (entity.HasComponent<MovementComponent>())
                    stateMsg.Movement = entity.GetComponent<MovementComponent>();
                    
                _networkManager.BroadcastToClients(stateMsg, DeliveryMethod.Unreliable);
                
                // Update sync time
                network.LastSyncTime = (float)gameTime.TotalGameTime.TotalSeconds;
                entity.SetComponent(network);
            }
        }
    }
}
```

#### **NetworkInputSystem** - Handle client input
```csharp
public class NetworkInputSystem : IGameSystem
{
    public void Update(GameTime gameTime)
    {
        if (_networkManager.IsHost)
        {
            // Host: Process received input from clients
            ProcessClientInputs();
        }
        else
        {
            // Client: Send input to host
            SendInputToHost();
        }
    }
    
    private void SendInputToHost()
    {
        // Get local player input
        var localPlayer = GetLocalPlayerEntity();
        if (localPlayer?.HasComponent<PlayerInputComponent>() == true)
        {
            var input = localPlayer.GetComponent<PlayerInputComponent>();
            
            var inputMsg = new PlayerInputMessage
            {
                PlayerId = GetLocalPlayerId(),
                MovementInput = input.MovementInput,
                ActionPressed = input.ActionPressed,
                Timestamp = DateTime.UtcNow.Ticks
            };
            
            _networkManager.SendToHost(inputMsg, DeliveryMethod.ReliableOrdered);
        }
    }
}
```

### 3.3 Integration with Existing Systems

**Modify ComponentInputSystem:**
```csharp
// ComponentInputSystem.cs - Updated for multiplayer
public void Update(GameTime gameTime)
{
    if (_networkManager.IsHost)
    {
        // Host: Process input for local player + received network input
        ProcessLocalPlayerInput();
        ProcessNetworkPlayerInputs();
    }
    else
    {
        // Client: Only process local input (send to NetworkInputSystem)
        ProcessLocalPlayerInput();
    }
}
```

**Modify ComponentMovementSystem:**
```csharp
// ComponentMovementSystem.cs - Authority check
public void Update(GameTime gameTime)
{
    var movingEntities = _entityManager.GetEntitiesWith<TransformComponent, MovementComponent>();
    
    foreach (var entity in movingEntities)
    {
        // Only process movement if we have authority
        if (HasMovementAuthority(entity))
        {
            // Existing movement logic...
            ApplyMovement(entity, gameTime);
        }
    }
}

private bool HasMovementAuthority(Entity entity)
{
    if (_networkManager.IsHost) return true; // Host has authority over everything
    
    // Client: Only process own entities
    return entity.HasComponent<AuthorityComponent>() && 
           entity.GetComponent<AuthorityComponent>().OwnerId == _networkManager.LocalPlayerId;
}
```

## 📦 Phase 4: Inventory Synchronization

### 4.1 Network Inventory Events
```csharp
[Serializable]
public class NetworkInventoryEvent
{
    public InventoryEventType Type; // Add, Remove, Transfer
    public int PlayerId;
    public int ItemId;
    public int SlotIndex;
    public int ContainerId; // For chest transfers
}
```

### 4.2 Modify Existing InventorySystem
```csharp
// InventorySystem.cs - Add network authority checks
public bool TryAddItem(Entity player, Entity item)
{
    // Authority check: Only host can modify inventories
    if (!_networkManager.IsHost) return false;
    
    // Existing inventory logic...
    bool success = PerformAddItem(player, item);
    
    if (success)
    {
        // Broadcast inventory change to all clients
        var networkEvent = new NetworkInventoryEvent
        {
            Type = InventoryEventType.Add,
            PlayerId = GetPlayerId(player),
            ItemId = item.Id,
            SlotIndex = GetSlotIndex(player, item)
        };
        
        _networkManager.BroadcastToClients(networkEvent, DeliveryMethod.ReliableOrdered);
    }
    
    return success;
}
```

### 4.3 Client Inventory Updates
```csharp
// NetworkMessageSystem.cs - Handle inventory sync
private void OnNetworkInventoryEvent(NetworkInventoryEvent evt)
{
    if (_networkManager.IsHost) return; // Host doesn't process its own events
    
    var player = GetPlayerById(evt.PlayerId);
    if (player == null) return;
    
    switch (evt.Type)
    {
        case InventoryEventType.Add:
            var item = GetItemById(evt.ItemId);
            ApplyInventoryAdd(player, item, evt.SlotIndex);
            break;
            
        case InventoryEventType.Remove:
            ApplyInventoryRemove(player, evt.SlotIndex);
            break;
            
        case InventoryEventType.Transfer:
            var container = GetEntityById(evt.ContainerId);
            ApplyInventoryTransfer(player, container, evt.SlotIndex);
            break;
    }
}
```

## 🔗 Integration Points with Current Architecture

### Scene System Integration
```csharp
// SceneManager.cs - Add multiplayer scenes
public enum SceneType 
{
    StartMenu,
    Lobby,      // NEW: Multiplayer lobby
    Gameplay,
    // ... existing scenes
}

// Scene transitions handle network state
private void OnSceneTransition(SceneTransitionEvent evt)
{
    if (evt.ToScene == SceneType.Lobby)
    {
        // Initialize network manager
        InitializeNetworking();
    }
    else if (evt.ToScene == SceneType.Gameplay && _networkManager.IsConnected)
    {
        // Start multiplayer gameplay
        InitializeMultiplayerGameplay();
    }
}
```

### EventBus Integration
```csharp
// All network events flow through existing EventBus
public class NetworkEventBridge
{
    public void BridgeNetworkToGame()
    {
        // Network → Game Events
        _networkManager.OnPlayerJoined += (player) => 
            _eventBus.Send(new PlayerJoinedEvent(player));
            
        _networkManager.OnEntityStateReceived += (state) => 
            _eventBus.Send(new NetworkEntityUpdateEvent(state));
    }
    
    public void BridgeGameToNetwork()
    {
        // Game → Network Events
        _eventBus.Subscribe<ItemPickupEvent>((evt) => {
            if (_networkManager.IsHost)
                _networkManager.BroadcastInventoryUpdate(evt);
        });
    }
}
```

### Component System Compatibility
```csharp
// Existing components work unchanged - just add network metadata
public void CreateNetworkPlayer(Vector2 position, PlayerIndex index, PlayerType type, int networkId)
{
    // Use existing CreatePlayer method
    var player = _entityManager.CreatePlayer(position, index, type);
    
    // Add network components
    player.AddComponent(new NetworkComponent 
    { 
        NetworkId = networkId, 
        SyncInterval = 0.05f // 20 FPS sync
    });
    
    player.AddComponent(new AuthorityComponent 
    { 
        OwnerId = GetPlayerIdFromNetworkId(networkId),
        IsServerOwned = false
    });
    
    return player;
}
```

## 🚀 Implementation Benefits

### Leverages Existing Strengths
- **✅ ECS Architecture**: Components serialize naturally for network
- **✅ Event System**: Perfect for network message handling
- **✅ Scene System**: Clean lobby/gameplay separation
- **✅ Player Types**: Character selection already implemented
- **✅ Inventory System**: Already has real-time UI updates

### Performance Characteristics
- **Low Latency**: UDP with reliability where needed
- **Efficient Sync**: Only changed components transmitted
- **Scalable**: Host-as-server model supports 2-8 players easily
- **Bandwidth Efficient**: Component-level granularity

### Development Timeline
- **Phase 1** (Network Foundation): 2-3 days
- **Phase 2** (Lobby System): 2-3 days  
- **Phase 3** (Entity Sync): 3-4 days
- **Phase 4** (Inventory Sync): 2-3 days
- **Testing & Polish**: 2-3 days

**Total Estimated Time: 11-16 days**

## 🎯 Success Criteria
- [x] **Lobby Creation**: Host can create lobby, display join code/IP
- [x] **Player Joining**: Multiple clients can connect and see each other
- [x] **Character Selection**: Each player picks Prisoner/Cop in lobby
- [x] **Real-time Movement**: Players see each other moving smoothly
- [x] **Inventory Sync**: Item pickups/drops reflect for all players
- [x] **Container Sync**: Chest interactions update for everyone
- [x] **Connection Handling**: Graceful join/leave/disconnect

This implementation plan builds directly on your existing solid architecture while adding the multiplayer functionality you need. The ECS foundation makes this much simpler than it would be with a traditional game architecture!