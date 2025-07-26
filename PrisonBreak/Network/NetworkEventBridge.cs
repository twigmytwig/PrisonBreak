using System;
using LiteNetLib;
using PrisonBreak.ECS;

namespace PrisonBreak.Network;

/// <summary>
/// Bridges between NetworkManager events and the game's EventBus system
/// Provides a clean integration layer between networking and ECS architecture
/// </summary>
public class NetworkEventBridge : IDisposable
{
    private readonly NetworkManager _networkManager;
    private readonly EventBus _eventBus;
    private bool _isDisposed = false;

    public NetworkEventBridge(NetworkManager networkManager, EventBus eventBus)
    {
        _networkManager = networkManager ?? throw new ArgumentNullException(nameof(networkManager));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

        Initialize();
    }

    private void Initialize()
    {
        // Subscribe to NetworkManager events and bridge them to EventBus
        
        _networkManager.PeerConnected += OnPeerConnected;
        _networkManager.PeerDisconnected += OnPeerDisconnected;
        _networkManager.MessageReceived += OnMessageReceived;

        Console.WriteLine("[NetworkEventBridge] Initialized - bridging network events to EventBus");
    }

    // NetworkManager Event Handlers → EventBus Events

    private void OnPeerConnected(NetPeer peer)
    {
        Console.WriteLine($"[NetworkEventBridge] Peer connected: {peer.EndPoint}");
        
        if (_networkManager.IsHost)
        {
            // Host: A client connected
            _eventBus.Send(new NetworkConnectionEvent(
                isConnected: true, 
                host: peer.EndPoint.Address.ToString(), 
                port: peer.EndPoint.Port, 
                message: $"Client connected from {peer.EndPoint}"
            ));
        }
        else
        {
            // Client: Connected to host
            _eventBus.Send(new NetworkConnectionEvent(
                isConnected: true, 
                host: peer.EndPoint.Address.ToString(), 
                port: peer.EndPoint.Port, 
                message: "Connected to host successfully"
            ));
        }
    }

    private void OnPeerDisconnected(NetPeer peer, string reason)
    {
        Console.WriteLine($"[NetworkEventBridge] Peer disconnected: {peer.EndPoint}, Reason: {reason}");
        
        _eventBus.Send(new NetworkConnectionEvent(
            isConnected: false, 
            host: peer.EndPoint.Address.ToString(), 
            port: peer.EndPoint.Port, 
            message: $"Disconnected: {reason}"
        ));
    }

    private void OnMessageReceived(NetworkMessage message)
    {
        // Route different message types to appropriate EventBus events
        try
        {
            switch (message)
            {
                case PlayerJoinMessage joinMsg:
                    _eventBus.Send(new NetworkPlayerJoinEvent(
                        joinMsg.PlayerId, 
                        joinMsg.PlayerName, 
                        joinMsg.SelectedType
                    ));
                    break;

                case PlayerLeaveMessage leaveMsg:
                    _eventBus.Send(new NetworkPlayerLeaveEvent(
                        leaveMsg.PlayerId, 
                        leaveMsg.Reason
                    ));
                    break;

                case EntityStateMessage entityMsg:
                    _eventBus.Send(new NetworkEntityUpdateEvent(entityMsg));
                    break;

                case InventoryActionMessage inventoryMsg:
                    _eventBus.Send(new NetworkInventoryActionEvent(inventoryMsg));
                    break;

                case PlayerCharacterUpdateMessage charUpdateMsg:
                    _eventBus.Send(new NetworkPlayerCharacterUpdateEvent(
                        charUpdateMsg.PlayerId,
                        charUpdateMsg.SelectedType
                    ));
                    break;

                case GameStateMessage gameStateMsg:
                    _eventBus.Send(new NetworkGameStateUpdateEvent(
                        gameStateMsg.State,
                        gameStateMsg.AdditionalData
                    ));
                    break;

                case LobbyStateMessage lobbyMsg:
                    _eventBus.Send(new NetworkLobbyUpdateEvent(
                        lobbyMsg.ConnectedPlayers,
                        lobbyMsg.CanStart
                    ));
                    break;

                case PlayerInputMessage inputMsg:
                    // Only process input messages if we're the host
                    if (_networkManager.IsHost)
                    {
                        _eventBus.Send(new NetworkPlayerInputEvent(
                            inputMsg.PlayerId,
                            inputMsg.MovementInput,
                            inputMsg.ActionPressed,
                            inputMsg.InteractionPressed
                        ));
                    }
                    break;

                default:
                    Console.WriteLine($"[NetworkEventBridge] Unhandled message type: {message.GetType().Name}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NetworkEventBridge] Error processing message {message.GetType().Name}: {ex.Message}");
        }
    }

    // EventBus Event Handlers → NetworkManager Messages
    // These methods can be called by game systems to send network messages

    public void SendPlayerCharacterUpdate(int playerId, PlayerType selectedType)
    {
        if (!_networkManager.IsConnected) return;

        var message = new PlayerCharacterUpdateMessage
        {
            PlayerId = playerId,
            SelectedType = selectedType
        };

        if (_networkManager.IsHost)
        {
            _networkManager.BroadcastToClients(message);
        }
        else
        {
            _networkManager.SendToHost(message);
        }

        Console.WriteLine($"[NetworkEventBridge] Sent character update: Player {playerId} → {selectedType}");
    }

    public void SendInventoryAction(int playerId, InventoryActionType action, int itemId, int slotIndex, int? containerId = null)
    {
        if (!_networkManager.IsConnected) return;

        var message = new InventoryActionMessage
        {
            PlayerId = playerId,
            Action = action,
            ItemId = itemId,
            SlotIndex = slotIndex,
            ContainerId = containerId
        };

        if (_networkManager.IsHost)
        {
            _networkManager.BroadcastToClients(message);
        }
        else
        {
            _networkManager.SendToHost(message);
        }

        Console.WriteLine($"[NetworkEventBridge] Sent inventory action: {action} for player {playerId}");
    }

    public void SendGameStateUpdate(GameStateType state, string additionalData = null)
    {
        if (!_networkManager.IsConnected || !_networkManager.IsHost) return;

        var message = new GameStateMessage
        {
            State = state,
            AdditionalData = additionalData ?? string.Empty
        };

        _networkManager.BroadcastToClients(message);
        Console.WriteLine($"[NetworkEventBridge] Sent game state update: {state}");
    }

    public void SendLobbyUpdate(LobbyPlayer[] players, bool canStart)
    {
        if (!_networkManager.IsConnected || !_networkManager.IsHost) return;

        var message = new LobbyStateMessage
        {
            ConnectedPlayers = players,
            CanStart = canStart
        };

        _networkManager.BroadcastToClients(message);
        Console.WriteLine($"[NetworkEventBridge] Sent lobby update: {players.Length} players, CanStart: {canStart}");
    }

    // Utility methods for game systems

    public bool IsNetworkActive => _networkManager.IsConnected;
    public bool IsHost => _networkManager.IsHost;
    public int LocalPlayerId => _networkManager.LocalPlayerId;

    public void Dispose()
    {
        if (_isDisposed) return;

        // Unsubscribe from NetworkManager events
        if (_networkManager != null)
        {
            _networkManager.PeerConnected -= OnPeerConnected;
            _networkManager.PeerDisconnected -= OnPeerDisconnected;
            _networkManager.MessageReceived -= OnMessageReceived;
        }

        _isDisposed = true;
        Console.WriteLine("[NetworkEventBridge] Disposed");
    }
}

// Additional EventBus events for network integration

public class NetworkPlayerCharacterUpdateEvent
{
    public int PlayerId { get; }
    public PlayerType SelectedType { get; }

    public NetworkPlayerCharacterUpdateEvent(int playerId, PlayerType selectedType)
    {
        PlayerId = playerId;
        SelectedType = selectedType;
    }
}

public class NetworkGameStateUpdateEvent
{
    public GameStateType State { get; }
    public string AdditionalData { get; }

    public NetworkGameStateUpdateEvent(GameStateType state, string additionalData)
    {
        State = state;
        AdditionalData = additionalData;
    }
}

public class NetworkLobbyUpdateEvent
{
    public LobbyPlayer[] ConnectedPlayers { get; }
    public bool CanStart { get; }

    public NetworkLobbyUpdateEvent(LobbyPlayer[] connectedPlayers, bool canStart)
    {
        ConnectedPlayers = connectedPlayers;
        CanStart = canStart;
    }
}

public class NetworkPlayerInputEvent
{
    public int PlayerId { get; }
    public Microsoft.Xna.Framework.Vector2 MovementInput { get; }
    public bool ActionPressed { get; }
    public bool InteractionPressed { get; }

    public NetworkPlayerInputEvent(int playerId, Microsoft.Xna.Framework.Vector2 movementInput, bool actionPressed, bool interactionPressed)
    {
        PlayerId = playerId;
        MovementInput = movementInput;
        ActionPressed = actionPressed;
        InteractionPressed = interactionPressed;
    }
}