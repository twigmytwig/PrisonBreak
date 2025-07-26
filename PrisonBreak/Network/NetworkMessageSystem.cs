using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using PrisonBreak.ECS;
using PrisonBreak.Systems;

namespace PrisonBreak.Network;

/// <summary>
/// ECS System that handles network message processing and entity state synchronization
/// Integrates with the existing game architecture through EventBus communication
/// </summary>
public class NetworkMessageSystem : IGameSystem
{
    private ComponentEntityManager _entityManager;
    private EventBus _eventBus;
    private NetworkManager _networkManager;
    
    // Network entity tracking
    private readonly Dictionary<int, Entity> _networkEntities = new(); // NetworkId -> Entity
    private readonly Dictionary<int, int> _entityToNetworkId = new(); // EntityId -> NetworkId
    private int _nextNetworkId = 1;
    
    // Sync timing
    private float _syncInterval = 0.05f; // 20 FPS network sync
    private float _lastSyncTime = 0f;
    
    public bool IsInitialized { get; private set; }

    public void Initialize()
    {
        if (IsInitialized)
        {
            Console.WriteLine("[NetworkMessageSystem] Already initialized");
            return;
        }

        IsInitialized = true;
        Console.WriteLine("[NetworkMessageSystem] Initialized");
    }

    public void SetEntityManager(ComponentEntityManager entityManager)
    {
        _entityManager = entityManager;
    }

    public void SetEventBus(EventBus eventBus)
    {
        _eventBus = eventBus;
        
        if (_eventBus != null)
        {
            // Subscribe to network events from EventBus
            _eventBus.Subscribe<NetworkEntityUpdateEvent>(OnNetworkEntityUpdate);
            _eventBus.Subscribe<NetworkPlayerJoinEvent>(OnNetworkPlayerJoin);
            _eventBus.Subscribe<NetworkPlayerLeaveEvent>(OnNetworkPlayerLeave);
            _eventBus.Subscribe<NetworkInventoryActionEvent>(OnNetworkInventoryAction);
            
            // Subscribe to game events that should trigger network messages
            _eventBus.Subscribe<PlayerInputEvent>(OnPlayerInput);
            _eventBus.Subscribe<ItemAddedEvent>(OnItemAdded);
            _eventBus.Subscribe<ItemRemovedEvent>(OnItemRemoved);
            _eventBus.Subscribe<EntitySpawnEvent>(OnEntitySpawn);
        }
    }

    public void SetNetworkManager(NetworkManager networkManager)
    {
        _networkManager = networkManager;
        
        if (_networkManager != null)
        {
            Console.WriteLine($"[NetworkMessageSystem] Network manager set. IsHost: {_networkManager.IsHost}");
        }
    }

    public void Update(GameTime gameTime)
    {
        if (!IsInitialized || _networkManager == null || !_networkManager.IsConnected)
            return;

        // Update network manager to process incoming messages
        _networkManager.Update();

        // Host: Sync entity states to clients periodically
        if (_networkManager.IsHost)
        {
            float currentTime = (float)gameTime.TotalGameTime.TotalSeconds;
            if (currentTime - _lastSyncTime >= _syncInterval)
            {
                SyncEntitiesToClients();
                _lastSyncTime = currentTime;
            }
        }
    }

    public void Draw(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
    {
        // Network system doesn't render anything
    }

    public void Shutdown()
    {
        Dispose();
    }

    // Network Entity Management
    
    public void RegisterNetworkEntity(Entity entity)
    {
        if (entity.HasComponent<NetworkComponent>())
        {
            var networkComp = entity.GetComponent<NetworkComponent>();
            _networkEntities[networkComp.NetworkId] = entity;
            _entityToNetworkId[entity.Id] = networkComp.NetworkId;
            
            Console.WriteLine($"[NetworkMessageSystem] Registered network entity {entity.Id} with NetworkId {networkComp.NetworkId}");
        }
    }

    public void UnregisterNetworkEntity(Entity entity)
    {
        if (_entityToNetworkId.TryGetValue(entity.Id, out int networkId))
        {
            _networkEntities.Remove(networkId);
            _entityToNetworkId.Remove(entity.Id);
            
            Console.WriteLine($"[NetworkMessageSystem] Unregistered network entity {entity.Id}");
        }
    }

    public int AssignNetworkId(Entity entity)
    {
        int networkId = _nextNetworkId++;
        _networkEntities[networkId] = entity;
        _entityToNetworkId[entity.Id] = networkId;
        return networkId;
    }

    // Host: Entity State Synchronization
    
    private void SyncEntitiesToClients()
    {
        if (_entityManager == null) return;

        // Get all entities that need network synchronization
        var networkSyncEntities = _entityManager.GetEntitiesWith<NetworkComponent, TransformComponent>();

        foreach (var entity in networkSyncEntities)
        {
            var networkComp = entity.GetComponent<NetworkComponent>();
            
            // Check if entity needs sync based on interval
            var currentTime = Environment.TickCount / 1000f;
            if (currentTime - networkComp.LastSyncTime >= networkComp.SyncInterval)
            {
                SyncEntityToClients(entity);
                
                // Update sync time
                ref var networkCompRef = ref entity.GetComponent<NetworkComponent>();
                networkCompRef.LastSyncTime = currentTime;
            }
        }
    }

    private void SyncEntityToClients(Entity entity)
    {
        try
        {
            var networkComp = entity.GetComponent<NetworkComponent>();
            var transform = entity.GetComponent<TransformComponent>();
            
            var stateMessage = new EntityStateMessage
            {
                EntityId = entity.Id,
                NetworkId = networkComp.NetworkId,
                Position = transform.Position,
                Rotation = transform.Rotation,
                Scale = transform.Scale
            };

            // Add optional components if they exist
            if (entity.HasComponent<MovementComponent>())
            {
                var movement = entity.GetComponent<MovementComponent>();
                stateMessage.Velocity = movement.Velocity;
                stateMessage.MaxSpeed = movement.MaxSpeed;
            }

            if (entity.HasComponent<PlayerTypeComponent>())
            {
                var playerType = entity.GetComponent<PlayerTypeComponent>();
                stateMessage.PlayerType = playerType.Type;
                stateMessage.AnimationName = playerType.AnimationName;
            }

            _networkManager.BroadcastToClients(stateMessage, LiteNetLib.DeliveryMethod.Unreliable);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NetworkMessageSystem] Error syncing entity {entity.Id}: {ex.Message}");
        }
    }

    // Event Handlers: Network → Game
    
    private void OnNetworkEntityUpdate(NetworkEntityUpdateEvent evt)
    {
        if (_networkManager?.IsHost == true) return; // Host doesn't process its own updates

        try
        {
            var stateData = evt.StateData;
            
            // Find or create the networked entity
            if (!_networkEntities.TryGetValue(stateData.NetworkId, out var entity))
            {
                // Entity doesn't exist locally, we might need to create it
                // For now, just log and skip
                Console.WriteLine($"[NetworkMessageSystem] Received update for unknown network entity {stateData.NetworkId}");
                return;
            }

            // Apply transform updates
            if (entity.HasComponent<TransformComponent>())
            {
                ref var transform = ref entity.GetComponent<TransformComponent>();
                transform.Position = stateData.Position;
                transform.Rotation = stateData.Rotation;
                transform.Scale = stateData.Scale;
            }

            // Apply movement updates if available
            if (stateData.Velocity.HasValue && entity.HasComponent<MovementComponent>())
            {
                ref var movement = ref entity.GetComponent<MovementComponent>();
                movement.Velocity = stateData.Velocity.Value;
                if (stateData.MaxSpeed.HasValue)
                    movement.MaxSpeed = stateData.MaxSpeed.Value;
            }

            // Apply player type updates if available
            if (stateData.PlayerType.HasValue && entity.HasComponent<PlayerTypeComponent>())
            {
                ref var playerType = ref entity.GetComponent<PlayerTypeComponent>();
                playerType.Type = stateData.PlayerType.Value;
                if (!string.IsNullOrEmpty(stateData.AnimationName))
                    playerType.AnimationName = stateData.AnimationName;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NetworkMessageSystem] Error applying network entity update: {ex.Message}");
        }
    }

    private void OnNetworkPlayerJoin(NetworkPlayerJoinEvent evt)
    {
        Console.WriteLine($"[NetworkMessageSystem] Processing player join: {evt.PlayerName} (ID: {evt.PlayerId})");
        
        if (_networkManager?.IsHost == true)
        {
            // Host: Create network entity for the new player
            // This would be handled by the lobby/gameplay scene
            // For now, just log
            Console.WriteLine($"[NetworkMessageSystem] Host should create entity for player {evt.PlayerId}");
        }
        else
        {
            // Client: Log that a player joined
            Console.WriteLine($"[NetworkMessageSystem] Player {evt.PlayerName} joined the game");
        }
    }

    private void OnNetworkPlayerLeave(NetworkPlayerLeaveEvent evt)
    {
        Console.WriteLine($"[NetworkMessageSystem] Processing player leave: ID {evt.PlayerId}, Reason: {evt.Reason}");
        
        // Find and remove the player's entities
        var playerEntities = _entityManager.GetEntitiesWith<NetworkComponent>()
            .Where(e => e.HasComponent<AuthorityComponent>() && 
                       e.GetComponent<AuthorityComponent>().OwnerId == evt.PlayerId)
            .ToList();

        foreach (var entity in playerEntities)
        {
            UnregisterNetworkEntity(entity);
            _entityManager.DestroyEntity(entity.Id);
        }
    }

    private void OnNetworkInventoryAction(NetworkInventoryActionEvent evt)
    {
        if (_networkManager?.IsHost == true) return; // Host processes inventory locally

        var actionData = evt.ActionData;
        Console.WriteLine($"[NetworkMessageSystem] Processing inventory action: {actionData.Action} for player {actionData.PlayerId}");
        
        // Apply inventory changes received from host
        // This would integrate with the existing InventorySystem
        // For now, just log the action
    }

    // Event Handlers: Game → Network
    
    private void OnPlayerInput(PlayerInputEvent evt)
    {
        if (_networkManager?.IsHost == true) return; // Host processes input locally
        if (_networkManager?.IsConnected != true) return;

        // Client: Send input to host
        var inputMessage = new PlayerInputMessage
        {
            PlayerId = _networkManager.LocalPlayerId,
            MovementInput = evt.MovementDirection,
            ActionPressed = evt.SpeedBoost, // Using SpeedBoost as action for now
            InteractionPressed = false // TODO: Add interaction input to PlayerInputEvent
        };

        _networkManager.SendToHost(inputMessage, LiteNetLib.DeliveryMethod.ReliableOrdered);
    }

    private void OnItemAdded(ItemAddedEvent evt)
    {
        if (_networkManager?.IsHost != true) return; // Only host broadcasts inventory changes
        if (_networkManager?.IsConnected != true) return;

        // Host: Broadcast inventory action to all clients
        var inventoryMessage = new InventoryActionMessage
        {
            PlayerId = evt.PlayerId,
            Action = InventoryActionType.Pickup,
            ItemId = evt.ItemEntity?.Id ?? -1,
            SlotIndex = evt.SlotIndex
        };

        _networkManager.BroadcastToClients(inventoryMessage, LiteNetLib.DeliveryMethod.ReliableOrdered);
        Console.WriteLine($"[NetworkMessageSystem] Broadcasted item added: Player {evt.PlayerId} added item {evt.ItemEntity?.Id}");
    }

    private void OnItemRemoved(ItemRemovedEvent evt)
    {
        if (_networkManager?.IsHost != true) return; // Only host broadcasts inventory changes
        if (_networkManager?.IsConnected != true) return;

        // Host: Broadcast inventory action to all clients
        var inventoryMessage = new InventoryActionMessage
        {
            PlayerId = evt.PlayerId,
            Action = InventoryActionType.Drop,
            ItemId = evt.ItemEntity?.Id ?? -1,
            SlotIndex = evt.SlotIndex
        };

        _networkManager.BroadcastToClients(inventoryMessage, LiteNetLib.DeliveryMethod.ReliableOrdered);
        Console.WriteLine($"[NetworkMessageSystem] Broadcasted item removed: Player {evt.PlayerId} removed item {evt.ItemEntity?.Id}");
    }

    private void OnEntitySpawn(EntitySpawnEvent evt)
    {
        if (_networkManager?.IsHost != true) return; // Only host manages entity spawning
        if (_networkManager?.IsConnected != true) return;

        // Check if this entity should be networked
        var entity = _entityManager.GetEntity(evt.EntityId);
        if (entity?.HasComponent<NetworkComponent>() == true)
        {
            RegisterNetworkEntity(entity);
            Console.WriteLine($"[NetworkMessageSystem] Registered spawned network entity {evt.EntityId}");
        }
    }

    // Cleanup
    
    public void Dispose()
    {
        if (_eventBus != null)
        {
            _eventBus.Unsubscribe<NetworkEntityUpdateEvent>(OnNetworkEntityUpdate);
            _eventBus.Unsubscribe<NetworkPlayerJoinEvent>(OnNetworkPlayerJoin);
            _eventBus.Unsubscribe<NetworkPlayerLeaveEvent>(OnNetworkPlayerLeave);
            _eventBus.Unsubscribe<NetworkInventoryActionEvent>(OnNetworkInventoryAction);
            _eventBus.Unsubscribe<PlayerInputEvent>(OnPlayerInput);
            _eventBus.Unsubscribe<ItemAddedEvent>(OnItemAdded);
            _eventBus.Unsubscribe<ItemRemovedEvent>(OnItemRemoved);
            _eventBus.Unsubscribe<EntitySpawnEvent>(OnEntitySpawn);
        }

        _networkEntities.Clear();
        _entityToNetworkId.Clear();
        
        IsInitialized = false;
        Console.WriteLine("[NetworkMessageSystem] Disposed");
    }
}