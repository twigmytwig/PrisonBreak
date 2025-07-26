using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;
using Newtonsoft.Json;
using PrisonBreak.ECS;

namespace PrisonBreak.Network;

public class NetworkManager : INetEventListener
{
    private NetManager _netManager;
    private NetPeer _hostPeer; // For clients: connection to host
    private readonly List<NetPeer> _connectedPeers = new(); // For host: connected clients
    private readonly Dictionary<int, LobbyPlayer> _lobbyPlayers = new();
    private readonly EventBus _eventBus;
    
    private bool _isHost = false;
    private bool _isConnected = false;
    private int _localPlayerId = -1;
    private int _nextPlayerId = 1;
    private string _localPlayerName = "Player";
    
    // Network configuration
    private const int DEFAULT_PORT = 9050;
    private const string GAME_VERSION = "0.1.0";

    public bool IsHost => _isHost;
    public bool IsConnected => _isConnected;
    public int LocalPlayerId => _localPlayerId;
    public string LocalPlayerName => _localPlayerName;
    public IReadOnlyList<NetPeer> ConnectedPeers => _connectedPeers.AsReadOnly();
    public IReadOnlyDictionary<int, LobbyPlayer> LobbyPlayers => _lobbyPlayers.AsReadOnly();

    // Events for network state changes
    public event Action<NetPeer> PeerConnected;
    public event Action<NetPeer, string> PeerDisconnected;
    public event Action<NetworkMessage> MessageReceived;

    public NetworkManager(EventBus eventBus)
    {
        _eventBus = eventBus;
        _netManager = new NetManager(this)
        {
            AutoRecycle = true,
            IPv6Enabled = false
        };
        
        Console.WriteLine("[NetworkManager] Initialized");
    }

    // Host Operations
    
    public bool StartHost(int port = DEFAULT_PORT, string playerName = "Host")
    {
        try
        {
            _localPlayerName = playerName;
            
            if (!_netManager.Start(port))
            {
                Console.WriteLine($"[NetworkManager] Failed to start host on port {port}");
                return false;
            }

            _isHost = true;
            _isConnected = true;
            _localPlayerId = 0; // Host is always player 0
            
            // Add host to lobby
            var hostPlayer = new LobbyPlayer
            {
                PlayerId = _localPlayerId,
                Name = _localPlayerName,
                SelectedType = PlayerType.Prisoner, // Default
                IsReady = false,
                IsHost = true
            };
            _lobbyPlayers[_localPlayerId] = hostPlayer;

            Console.WriteLine($"[NetworkManager] Host started on port {port}. Local IP: {GetLocalIPAddress()}");
            
            // Notify game systems
            _eventBus.Send(new NetworkConnectionEvent(true, "localhost", port, "Host started successfully"));
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NetworkManager] Error starting host: {ex.Message}");
            return false;
        }
    }

    // Client Operations
    
    public bool ConnectToHost(string host, int port = DEFAULT_PORT, string playerName = "Player")
    {
        try
        {
            _localPlayerName = playerName;
            
            if (!_netManager.Start())
            {
                Console.WriteLine("[NetworkManager] Failed to start client");
                return false;
            }

            _hostPeer = _netManager.Connect(host, port, "PrisonBreak");
            if (_hostPeer == null)
            {
                Console.WriteLine($"[NetworkManager] Failed to connect to {host}:{port}");
                return false;
            }

            _isHost = false;
            Console.WriteLine($"[NetworkManager] Attempting to connect to {host}:{port}");
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NetworkManager] Error connecting to host: {ex.Message}");
            return false;
        }
    }

    // Message Broadcasting
    
    public void BroadcastToClients<T>(T data, DeliveryMethod method = DeliveryMethod.ReliableOrdered) where T : NetworkMessage
    {
        if (!_isHost || _connectedPeers.Count == 0) return;

        try
        {
            string json = JsonConvert.SerializeObject(data);
            var writer = new NetDataWriter();
            writer.Put(json);

            foreach (var peer in _connectedPeers)
            {
                peer.Send(writer, method);
            }
            
            Console.WriteLine($"[NetworkManager] Broadcast {typeof(T).Name} to {_connectedPeers.Count} clients");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NetworkManager] Error broadcasting message: {ex.Message}");
        }
    }

    public void SendToHost<T>(T data, DeliveryMethod method = DeliveryMethod.ReliableOrdered) where T : NetworkMessage
    {
        if (_isHost || _hostPeer == null) return;

        try
        {
            string json = JsonConvert.SerializeObject(data);
            var writer = new NetDataWriter();
            writer.Put(json);

            _hostPeer.Send(writer, method);
            
            Console.WriteLine($"[NetworkManager] Sent {typeof(T).Name} to host");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NetworkManager] Error sending message to host: {ex.Message}");
        }
    }

    public void SendToPeer<T>(NetPeer peer, T data, DeliveryMethod method = DeliveryMethod.ReliableOrdered) where T : NetworkMessage
    {
        if (peer == null) return;

        try
        {
            string json = JsonConvert.SerializeObject(data);
            var writer = new NetDataWriter();
            writer.Put(json);

            peer.Send(writer, method);
            
            Console.WriteLine($"[NetworkManager] Sent {typeof(T).Name} to peer {peer.Id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NetworkManager] Error sending message to peer: {ex.Message}");
        }
    }

    // Network Update Loop
    
    public void Update()
    {
        _netManager?.PollEvents();
    }

    // Connection Management
    
    public void Disconnect()
    {
        try
        {
            if (_isHost)
            {
                // Notify all clients that host is shutting down
                BroadcastToClients(new GameStateMessage { State = GameStateType.Ended, AdditionalData = "Host disconnected" });
            }
            else if (_hostPeer != null)
            {
                // Notify host that client is leaving
                SendToHost(new PlayerLeaveMessage { PlayerId = _localPlayerId, Reason = "Player disconnected" });
            }

            _netManager?.Stop();
            _connectedPeers.Clear();
            _lobbyPlayers.Clear();
            
            _isHost = false;
            _isConnected = false;
            _localPlayerId = -1;
            _hostPeer = null;

            Console.WriteLine("[NetworkManager] Disconnected");
            
            // Notify game systems
            _eventBus.Send(new NetworkConnectionEvent(false, "", 0, "Disconnected"));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NetworkManager] Error during disconnect: {ex.Message}");
        }
    }

    // Utility Methods
    
    public string GetLocalIPAddress()
    {
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socket.Connect("8.8.8.8", 65530);
            var endPoint = socket.LocalEndPoint as IPEndPoint;
            return endPoint?.Address.ToString() ?? "127.0.0.1";
        }
        catch
        {
            return "127.0.0.1";
        }
    }

    public void Dispose()
    {
        Disconnect();
        // Note: LiteNetLib NetManager doesn't have Dispose method
        // _netManager = null; // Let GC handle it
    }

    // INetEventListener Implementation
    
    public void OnPeerConnected(NetPeer peer)
    {
        Console.WriteLine($"[NetworkManager] Peer connected: {peer.EndPoint}");
        
        if (_isHost)
        {
            _connectedPeers.Add(peer);
            PeerConnected?.Invoke(peer);
            
            // Send current lobby state to new client
            var lobbyState = new LobbyStateMessage
            {
                ConnectedPlayers = _lobbyPlayers.Values.ToArray(),
                CanStart = CanStartGame()
            };
            SendToPeer(peer, lobbyState);
        }
        else
        {
            _isConnected = true;
            PeerConnected?.Invoke(peer);
            
            // Send connection request to host
            var connectionRequest = new ConnectionRequestMessage
            {
                PlayerName = _localPlayerName,
                GameVersion = GAME_VERSION
            };
            SendToHost(connectionRequest);
        }
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Console.WriteLine($"[NetworkManager] Peer disconnected: {peer.EndPoint}, Reason: {disconnectInfo.Reason}");
        
        if (_isHost)
        {
            _connectedPeers.Remove(peer);
            
            // Find and remove player from lobby
            var playerToRemove = _lobbyPlayers.Values.FirstOrDefault(p => p.PlayerId == peer.Id);
            if (playerToRemove.PlayerId != 0) // Don't remove host
            {
                _lobbyPlayers.Remove(playerToRemove.PlayerId);
                
                // Notify remaining clients
                var leaveMessage = new PlayerLeaveMessage
                {
                    PlayerId = playerToRemove.PlayerId,
                    Reason = disconnectInfo.Reason.ToString()
                };
                BroadcastToClients(leaveMessage);
                
                // Notify game systems
                _eventBus.Send(new NetworkPlayerLeaveEvent(playerToRemove.PlayerId, disconnectInfo.Reason.ToString()));
            }
        }
        else
        {
            _isConnected = false;
            _hostPeer = null;
        }
        
        PeerDisconnected?.Invoke(peer, disconnectInfo.Reason.ToString());
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        try
        {
            string json = reader.GetString();
            
            // First, deserialize to a dynamic object to get the MessageType
            dynamic dynamicMessage = JsonConvert.DeserializeObject(json);
            string messageType = dynamicMessage?.MessageType;
            
            if (string.IsNullOrEmpty(messageType))
            {
                Console.WriteLine("[NetworkManager] Received message without MessageType");
                return;
            }

            // Route message based on type
            ProcessReceivedMessage(peer, json, messageType);
            
            // For NetworkEventBridge, we need to create a concrete message object
            NetworkMessage concreteMessage = CreateConcreteMessage(json, messageType);
            if (concreteMessage != null)
            {
                MessageReceived?.Invoke(concreteMessage);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NetworkManager] Error processing received message: {ex.Message}");
        }
    }

    private void ProcessReceivedMessage(NetPeer peer, string json, string messageType)
    {
        try
        {
            switch (messageType)
            {
                case "ConnectionRequest":
                    if (_isHost)
                        HandleConnectionRequest(peer, JsonConvert.DeserializeObject<ConnectionRequestMessage>(json));
                    break;
                    
                case "ConnectionResponse":
                    if (!_isHost)
                        HandleConnectionResponse(JsonConvert.DeserializeObject<ConnectionResponseMessage>(json));
                    break;
                    
                case "PlayerJoin":
                    HandlePlayerJoin(JsonConvert.DeserializeObject<PlayerJoinMessage>(json));
                    break;
                    
                case "PlayerLeave":
                    HandlePlayerLeave(JsonConvert.DeserializeObject<PlayerLeaveMessage>(json));
                    break;
                    
                case "PlayerCharacterUpdate":
                    HandlePlayerCharacterUpdate(JsonConvert.DeserializeObject<PlayerCharacterUpdateMessage>(json));
                    break;
                    
                case "EntityState":
                    var entityState = JsonConvert.DeserializeObject<EntityStateMessage>(json);
                    _eventBus.Send(new NetworkEntityUpdateEvent(entityState));
                    break;
                    
                case "PlayerInput":
                    if (_isHost)
                    {
                        var inputMsg = JsonConvert.DeserializeObject<PlayerInputMessage>(json);
                        // TODO: Process client input
                        Console.WriteLine($"[NetworkManager] Received input from player {inputMsg.PlayerId}");
                    }
                    break;
                    
                case "InventoryAction":
                    var inventoryAction = JsonConvert.DeserializeObject<InventoryActionMessage>(json);
                    _eventBus.Send(new NetworkInventoryActionEvent(inventoryAction));
                    break;
                    
                case "LobbyState":
                    var lobbyState = JsonConvert.DeserializeObject<LobbyStateMessage>(json);
                    // This is handled by the NetworkEventBridge, just log it
                    Console.WriteLine($"[NetworkManager] Received lobby state with {lobbyState.ConnectedPlayers.Length} players");
                    break;
                    
                case "GameState":
                    var gameState = JsonConvert.DeserializeObject<GameStateMessage>(json);
                    Console.WriteLine($"[NetworkManager] Received game state: {gameState.State}");
                    // This will be processed by NetworkEventBridge and sent to EventBus
                    break;
                    
                default:
                    Console.WriteLine($"[NetworkManager] Unknown message type: {messageType}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NetworkManager] Error processing {messageType} message: {ex.Message}");
        }
    }

    private void HandleConnectionRequest(NetPeer peer, ConnectionRequestMessage request)
    {
        bool accepted = true;
        string reason = "Welcome!";
        int assignedPlayerId = _nextPlayerId++;

        // Validate connection request
        if (request.GameVersion != GAME_VERSION)
        {
            accepted = false;
            reason = $"Version mismatch. Server: {GAME_VERSION}, Client: {request.GameVersion}";
        }
        else if (_lobbyPlayers.Count >= 8) // Max 8 players
        {
            accepted = false;
            reason = "Lobby is full";
        }

        var response = new ConnectionResponseMessage
        {
            Accepted = accepted,
            Reason = reason,
            AssignedPlayerId = assignedPlayerId
        };
        
        SendToPeer(peer, response);

        if (accepted)
        {
            // Add player to lobby
            var newPlayer = new LobbyPlayer
            {
                PlayerId = assignedPlayerId,
                Name = request.PlayerName,
                SelectedType = PlayerType.Prisoner, // Default
                IsReady = false,
                IsHost = false
            };
            _lobbyPlayers[assignedPlayerId] = newPlayer;

            // Notify all clients about new player
            var joinMessage = new PlayerJoinMessage
            {
                PlayerId = assignedPlayerId,
                PlayerName = request.PlayerName,
                SelectedType = PlayerType.Prisoner
            };
            BroadcastToClients(joinMessage);
            
            // Notify game systems
            _eventBus.Send(new NetworkPlayerJoinEvent(assignedPlayerId, request.PlayerName, PlayerType.Prisoner));
            
            Console.WriteLine($"[NetworkManager] Player {request.PlayerName} joined with ID {assignedPlayerId}");
        }
        else
        {
            Console.WriteLine($"[NetworkManager] Connection rejected: {reason}");
        }
    }

    private void HandleConnectionResponse(ConnectionResponseMessage response)
    {
        if (response.Accepted)
        {
            _localPlayerId = response.AssignedPlayerId;
            Console.WriteLine($"[NetworkManager] Connection accepted. Assigned player ID: {_localPlayerId}");
            
            // Add self to lobby
            var localPlayer = new LobbyPlayer
            {
                PlayerId = _localPlayerId,
                Name = _localPlayerName,
                SelectedType = PlayerType.Prisoner,
                IsReady = false,
                IsHost = false
            };
            _lobbyPlayers[_localPlayerId] = localPlayer;
        }
        else
        {
            Console.WriteLine($"[NetworkManager] Connection rejected: {response.Reason}");
            Disconnect();
        }
    }

    private void HandlePlayerJoin(PlayerJoinMessage message)
    {
        if (!_lobbyPlayers.ContainsKey(message.PlayerId))
        {
            var player = new LobbyPlayer
            {
                PlayerId = message.PlayerId,
                Name = message.PlayerName,
                SelectedType = message.SelectedType,
                IsReady = false,
                IsHost = false
            };
            _lobbyPlayers[message.PlayerId] = player;
            
            _eventBus.Send(new NetworkPlayerJoinEvent(message.PlayerId, message.PlayerName, message.SelectedType));
            Console.WriteLine($"[NetworkManager] Player {message.PlayerName} joined lobby");
        }
    }

    private void HandlePlayerLeave(PlayerLeaveMessage message)
    {
        if (_lobbyPlayers.Remove(message.PlayerId))
        {
            _eventBus.Send(new NetworkPlayerLeaveEvent(message.PlayerId, message.Reason));
            Console.WriteLine($"[NetworkManager] Player {message.PlayerId} left lobby: {message.Reason}");
        }
    }

    private void HandlePlayerCharacterUpdate(PlayerCharacterUpdateMessage message)
    {
        if (_lobbyPlayers.TryGetValue(message.PlayerId, out var player))
        {
            player.SelectedType = message.SelectedType;
            _lobbyPlayers[message.PlayerId] = player;
            Console.WriteLine($"[NetworkManager] Player {message.PlayerId} changed character to {message.SelectedType}");
        }
    }

    private NetworkMessage CreateConcreteMessage(string json, string messageType)
    {
        try
        {
            return messageType switch
            {
                "PlayerJoin" => JsonConvert.DeserializeObject<PlayerJoinMessage>(json),
                "PlayerLeave" => JsonConvert.DeserializeObject<PlayerLeaveMessage>(json),
                "PlayerCharacterUpdate" => JsonConvert.DeserializeObject<PlayerCharacterUpdateMessage>(json),
                "EntityState" => JsonConvert.DeserializeObject<EntityStateMessage>(json),
                "PlayerInput" => JsonConvert.DeserializeObject<PlayerInputMessage>(json),
                "InventoryAction" => JsonConvert.DeserializeObject<InventoryActionMessage>(json),
                "GameState" => JsonConvert.DeserializeObject<GameStateMessage>(json),
                "LobbyState" => JsonConvert.DeserializeObject<LobbyStateMessage>(json),
                "ConnectionRequest" => JsonConvert.DeserializeObject<ConnectionRequestMessage>(json),
                "ConnectionResponse" => JsonConvert.DeserializeObject<ConnectionResponseMessage>(json),
                _ => null
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NetworkManager] Error creating concrete message for {messageType}: {ex.Message}");
            return null;
        }
    }

    private bool CanStartGame()
    {
        return _lobbyPlayers.Count >= 2 && _lobbyPlayers.Values.All(p => p.IsReady || p.IsHost);
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        Console.WriteLine($"[NetworkManager] Network error from {endPoint}: {socketError}");
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        // Handle discovery or unconnected messages if needed
    }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
        // Handle latency updates if needed for lag compensation
    }

    public void OnConnectionRequest(ConnectionRequest request)
    {
        request.Accept();
    }
}