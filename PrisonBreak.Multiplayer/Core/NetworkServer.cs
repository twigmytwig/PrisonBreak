using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;
using PrisonBreak.Multiplayer.Messages;

namespace PrisonBreak.Multiplayer.Core;

/// <summary>
/// Server-side connection management with LiteNetLib
/// Handles hosting games, accepting client connections, and message broadcasting
/// </summary>
public class NetworkServer : INetEventListener
{
    private NetManager _netManager;
    private readonly Dictionary<int, NetPeer> _connectedClients;
    private readonly Dictionary<NetPeer, int> _clientIds;
    private int _nextClientId = 1;
    
    // Server state
    public bool IsRunning => _netManager?.IsRunning ?? false;
    public int ConnectedClientCount => _connectedClients.Count;
    public int MaxClients { get; private set; }
    
    // Server info
    public int Port { get; private set; }
    
    // Events for NetworkManager integration
    public event Action? OnStarted;
    public event Action? OnStopped;
    public event Action<int>? OnClientConnected; // clientId
    public event Action<int, string>? OnClientDisconnected; // clientId, reason
    public event Action<int, INetworkMessage>? OnMessageReceived; // clientId, message
    
    public NetworkServer(int maxClients = NetworkConfig.MaxPlayers)
    {
        _connectedClients = new Dictionary<int, NetPeer>();
        _clientIds = new Dictionary<NetPeer, int>();
        MaxClients = maxClients;
        _netManager = new NetManager(this);
    }
    
    #region Server Management
    
    /// <summary>
    /// Start the server on the specified port
    /// </summary>
    public bool Start(int port = NetworkConfig.DefaultPort)
    {
        if (IsRunning)
        {
            Console.WriteLine("[NetworkServer] Server already running");
            return false;
        }
        
        Port = port;
        
        try
        {
            bool started = _netManager.Start(port);
            if (started)
            {
                Console.WriteLine($"[NetworkServer] Server started on port {port}");
                OnStarted?.Invoke();
                return true;
            }
            else
            {
                Console.WriteLine($"[NetworkServer] Failed to start server on port {port}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NetworkServer] Error starting server: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Stop the server and disconnect all clients
    /// </summary>
    public void Stop()
    {
        if (!IsRunning)
            return;
            
        Console.WriteLine("[NetworkServer] Stopping server...");
        
        // Disconnect all clients
        foreach (var client in _connectedClients.Values)
        {
            client.Disconnect();
        }
        
        _connectedClients.Clear();
        _clientIds.Clear();
        _nextClientId = 1;
        
        _netManager.Stop();
        
        Console.WriteLine("[NetworkServer] Server stopped");
        OnStopped?.Invoke();
    }
    
    #endregion
    
    #region Client Management
    
    /// <summary>
    /// Get all connected client IDs
    /// </summary>
    public IEnumerable<int> GetConnectedClientIds()
    {
        return _connectedClients.Keys;
    }
    
    /// <summary>
    /// Check if a specific client is connected
    /// </summary>
    public bool IsClientConnected(int clientId)
    {
        return _connectedClients.ContainsKey(clientId);
    }
    
    /// <summary>
    /// Disconnect a specific client
    /// </summary>
    public void DisconnectClient(int clientId, string reason = "Disconnected by server")
    {
        if (_connectedClients.TryGetValue(clientId, out var client))
        {
            Console.WriteLine($"[NetworkServer] Disconnecting client {clientId}: {reason}");
            client.Disconnect();
        }
    }
    
    #endregion
    
    #region Message Sending
    
    /// <summary>
    /// Send a message to a specific client
    /// </summary>
    public void SendMessageToClient(int clientId, INetworkMessage message, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
    {
        if (!_connectedClients.TryGetValue(clientId, out var client))
        {
            Console.WriteLine($"[NetworkServer] Cannot send message - client {clientId} not found");
            return;
        }
        
        var writer = new NetDataWriter();
        message.Serialize(writer);
        client.Send(writer, deliveryMethod);
    }
    
    /// <summary>
    /// Send welcome message to newly connected client with their assigned player ID
    /// </summary>
    private void SendWelcomeMessage(int clientId)
    {
        // Create welcome message with the assigned player ID
        var welcomeMessage = new WelcomeMessage(
            clientId, 
            $"Welcome to PrisonBreak! You are Player {clientId}."
        );
        
        Console.WriteLine($"[NetworkServer] Sending welcome message to client {clientId}");
        SendMessageToClient(clientId, welcomeMessage);
    }
    
    /// <summary>
    /// Broadcast a message to all connected clients
    /// </summary>
    public void BroadcastMessage(INetworkMessage message, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
    {
        if (_connectedClients.Count == 0)
            return;
            
        var writer = new NetDataWriter();
        message.Serialize(writer);
        
        foreach (var client in _connectedClients.Values)
        {
            client.Send(writer, deliveryMethod);
        }
    }
    
    /// <summary>
    /// Broadcast a message to all clients except one
    /// </summary>
    public void BroadcastMessageExcept(int excludeClientId, INetworkMessage message, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
    {
        if (_connectedClients.Count == 0)
            return;
            
        var writer = new NetDataWriter();
        message.Serialize(writer);
        
        foreach (var (clientId, client) in _connectedClients)
        {
            if (clientId != excludeClientId)
            {
                client.Send(writer, deliveryMethod);
            }
        }
    }
    
    /// <summary>
    /// Send raw data to a specific client
    /// </summary>
    public void SendDataToClient(int clientId, NetDataWriter data, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
    {
        if (!_connectedClients.TryGetValue(clientId, out var client))
        {
            Console.WriteLine($"[NetworkServer] Cannot send data - client {clientId} not found");
            return;
        }
        
        client.Send(data, deliveryMethod);
    }
    
    /// <summary>
    /// Broadcast raw data to all connected clients
    /// </summary>
    public void BroadcastData(NetDataWriter data, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
    {
        foreach (var client in _connectedClients.Values)
        {
            client.Send(data, deliveryMethod);
        }
    }
    
    #endregion
    
    #region Network Updates
    
    /// <summary>
    /// Poll for network events - should be called regularly (every frame)
    /// </summary>
    public void PollEvents()
    {
        _netManager?.PollEvents();
    }
    
    #endregion
    
    #region INetEventListener Implementation
    
    public void OnPeerConnected(NetPeer peer)
    {
        // Check if server is full
        if (_connectedClients.Count >= MaxClients)
        {
            Console.WriteLine($"[NetworkServer] Rejecting connection from {peer.Address}:{peer.Port} - server full");
            peer.Disconnect();
            return;
        }
        
        // Assign client ID and register
        int clientId = _nextClientId++;
        _connectedClients[clientId] = peer;
        _clientIds[peer] = clientId;
        
        Console.WriteLine($"[NetworkServer] Client {clientId} connected from {peer.Address}:{peer.Port}");
        
        // Send welcome message with assigned player ID
        SendWelcomeMessage(clientId);
        
        OnClientConnected?.Invoke(clientId);
    }
    
    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        if (_clientIds.TryGetValue(peer, out int clientId))
        {
            _connectedClients.Remove(clientId);
            _clientIds.Remove(peer);
            
            Console.WriteLine($"[NetworkServer] Client {clientId} disconnected: {disconnectInfo.Reason}");
            OnClientDisconnected?.Invoke(clientId, disconnectInfo.Reason.ToString());
        }
    }
    
    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        if (!_clientIds.TryGetValue(peer, out int clientId))
        {
            Console.WriteLine("[NetworkServer] Received message from unregistered peer");
            reader.Recycle();
            return;
        }
        
        try
        {
            // Read message type first
            var messageType = (NetworkConfig.MessageType)reader.GetByte();
            
            // Create appropriate message instance and deserialize
            var message = CreateMessageInstance(messageType);
            if (message != null)
            {
                message.Deserialize(reader);
                OnMessageReceived?.Invoke(clientId, message);
            }
            else
            {
                // For unknown message types, create an UnknownNetworkMessage with raw data
                // NetworkManager will handle the actual deserialization
                byte[] remainingData = reader.GetRemainingBytes();
                var unknownMessage = new UnknownNetworkMessage(messageType, remainingData);
                OnMessageReceived?.Invoke(clientId, unknownMessage);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NetworkServer] Error processing message from client {clientId}: {ex.Message}");
        }
        finally
        {
            reader.Recycle();
        }
    }
    
    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        Console.WriteLine($"[NetworkServer] Network error: {socketError} from {endPoint}");
    }
    
    public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
        // Optional: Track network latency for debugging
        if (_clientIds.TryGetValue(peer, out int clientId))
        {
            // Console.WriteLine($"[NetworkServer] Client {clientId} latency: {latency}ms");
        }
    }
    
    public void OnConnectionRequest(ConnectionRequest request)
    {
        // Validate connection key
        if (request.Data.GetString() == NetworkConfig.DiscoveryKey)
        {
            if (_connectedClients.Count < MaxClients)
            {
                Console.WriteLine($"[NetworkServer] Accepting connection from {request.RemoteEndPoint}");
                request.Accept();
            }
            else
            {
                Console.WriteLine($"[NetworkServer] Rejecting connection from {request.RemoteEndPoint} - server full");
                request.Reject();
            }
        }
        else
        {
            Console.WriteLine($"[NetworkServer] Rejecting connection from {request.RemoteEndPoint} - invalid key");
            request.Reject();
        }
    }
    
    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        if (messageType == UnconnectedMessageType.BasicMessage)
        {
            var requestKey = reader.GetString();
            if (requestKey == NetworkConfig.DiscoveryKey)
            {
                Console.WriteLine($"[NetworkServer] Discovery request from {remoteEndPoint}");
                
                // Respond to discovery request
                var writer = new NetDataWriter();
                writer.Put(NetworkConfig.DiscoveryKey);
                writer.Put($"PrisonBreak Server - {_connectedClients.Count}/{MaxClients} players");
                _netManager.SendUnconnectedMessage(writer, remoteEndPoint);
            }
        }
        reader.Recycle();
    }
    
    #endregion
    
    #region Message Factory
    
    /// <summary>
    /// Create message instance based on message type
    /// Factory pattern for creating appropriate message instances for deserialization
    /// </summary>
    private INetworkMessage CreateMessageInstance(NetworkConfig.MessageType messageType)
    {
        // NOTE: Message factory in pure networking library cannot create game-specific message instances
        // Game-specific messages are handled by the NetworkManager in the main game project
        // This method is kept for potential future extension with pure networking messages
        return null;
    }
    
    #endregion
    
    #region Disposal
    
    public void Dispose()
    {
        Stop();
        // NetManager doesn't implement IDisposable in LiteNetLib 1.3.1
    }
    
    #endregion
}