using System;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;
using PrisonBreak.Multiplayer.Messages;

namespace PrisonBreak.Multiplayer.Core;

/// <summary>
/// Client-side connection management with LiteNetLib
/// Handles connection to LocalHost, message sending, and local network discovery
/// </summary>
public class NetworkClient : INetEventListener
{
    private NetManager _netManager;
    private NetPeer? _serverPeer;
    
    // Connection state
    public NetworkConfig.ConnectionState ConnectionState { get; private set; }
    public bool IsConnected => ConnectionState == NetworkConfig.ConnectionState.Connected;
    
    // Connection info
    public string? ConnectedServerAddress { get; private set; }
    public int ConnectedServerPort { get; private set; }
    
    // Events for NetworkManager integration
    public event Action? OnConnected;
    public event Action<string>? OnDisconnected; // reason
    public event Action<INetworkMessage>? OnMessageReceived;
    public event Action<IPEndPoint>? OnServerDiscovered;
    
    public NetworkClient()
    {
        _netManager = new NetManager(this);
        ConnectionState = NetworkConfig.ConnectionState.Disconnected;
    }
    
    #region Connection Management
    
    /// <summary>
    /// Start the client (required before connecting)
    /// </summary>
    public void Start()
    {
        if (!_netManager.IsRunning)
        {
            _netManager.Start();
            Console.WriteLine("[NetworkClient] Client started");
        }
    }
    
    /// <summary>
    /// Connect to a specific host address
    /// </summary>
    public void ConnectToHost(string hostAddress, int port = NetworkConfig.DefaultPort)
    {
        if (ConnectionState != NetworkConfig.ConnectionState.Disconnected)
        {
            Console.WriteLine($"[NetworkClient] Cannot connect - current state: {ConnectionState}");
            return;
        }
        
        Start(); // Ensure client is started
        
        ConnectionState = NetworkConfig.ConnectionState.Connecting;
        ConnectedServerAddress = hostAddress;
        ConnectedServerPort = port;
        
        Console.WriteLine($"[NetworkClient] Connecting to {hostAddress}:{port}");
        _serverPeer = _netManager.Connect(hostAddress, port, NetworkConfig.DiscoveryKey);
    }
    
    /// <summary>
    /// Disconnect from current server
    /// </summary>
    public void Disconnect()
    {
        if (ConnectionState == NetworkConfig.ConnectionState.Disconnected)
            return;
            
        ConnectionState = NetworkConfig.ConnectionState.Disconnecting;
        
        if (_serverPeer != null)
        {
            _serverPeer.Disconnect();
            _serverPeer = null;
        }
        
        Console.WriteLine("[NetworkClient] Disconnecting from server");
    }
    
    /// <summary>
    /// Stop the client completely
    /// </summary>
    public void Stop()
    {
        if (_netManager.IsRunning)
        {
            Disconnect();
            _netManager.Stop();
            ConnectionState = NetworkConfig.ConnectionState.Disconnected;
            Console.WriteLine("[NetworkClient] Client stopped");
        }
    }
    
    #endregion
    
    #region Local Network Discovery
    
    /// <summary>
    /// Discover local network servers
    /// </summary>
    public void DiscoverLocalServers()
    {
        if (!_netManager.IsRunning)
            Start();
            
        Console.WriteLine("[NetworkClient] Starting local server discovery");
        var writer = new NetDataWriter();
        writer.Put(NetworkConfig.DiscoveryKey);
        _netManager.SendUnconnectedMessage(writer, new IPEndPoint(IPAddress.Broadcast, NetworkConfig.DefaultPort));
    }
    
    #endregion
    
    #region Message Sending
    
    /// <summary>
    /// Send a message to the connected server
    /// </summary>
    public void SendMessage(INetworkMessage message, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
    {
        if (!IsConnected || _serverPeer == null)
        {
            Console.WriteLine($"[NetworkClient] Cannot send message - not connected");
            return;
        }
        
        var writer = new NetDataWriter();
        message.Serialize(writer);
        _serverPeer.Send(writer, deliveryMethod);
    }
    
    /// <summary>
    /// Send raw data to the connected server
    /// </summary>
    public void SendData(NetDataWriter data, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
    {
        if (!IsConnected || _serverPeer == null)
        {
            Console.WriteLine($"[NetworkClient] Cannot send data - not connected");
            return;
        }
        
        _serverPeer.Send(data, deliveryMethod);
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
        Console.WriteLine($"[NetworkClient] Connected to server: {peer.Address}:{peer.Port}");
        _serverPeer = peer;
        ConnectionState = NetworkConfig.ConnectionState.Connected;
        OnConnected?.Invoke();
    }
    
    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Console.WriteLine($"[NetworkClient] Disconnected from server: {disconnectInfo.Reason}");
        _serverPeer = null;
        ConnectionState = NetworkConfig.ConnectionState.Disconnected;
        OnDisconnected?.Invoke(disconnectInfo.Reason.ToString());
    }
    
    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        try
        {
            // Read message type first
            var messageType = (NetworkConfig.MessageType)reader.GetByte();
            
            // Create appropriate message instance and deserialize
            var message = CreateMessageInstance(messageType);
            if (message != null)
            {
                message.Deserialize(reader);
                OnMessageReceived?.Invoke(message);
            }
            else
            {
                // For unknown message types, create an UnknownNetworkMessage with raw data
                // NetworkManager will handle the actual deserialization
                byte[] remainingData = reader.GetRemainingBytes();
                var unknownMessage = new UnknownNetworkMessage(messageType, remainingData);
                OnMessageReceived?.Invoke(unknownMessage);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NetworkClient] Error processing received message: {ex.Message}");
        }
        finally
        {
            reader.Recycle();
        }
    }
    
    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        Console.WriteLine($"[NetworkClient] Network error: {socketError} from {endPoint}");
    }
    
    public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
        // Optional: Track network latency for debugging
        // Console.WriteLine($"[NetworkClient] Latency to server: {latency}ms");
    }
    
    public void OnConnectionRequest(ConnectionRequest request)
    {
        // Clients don't accept connections, only servers do
        request.Reject();
    }
    
    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        if (messageType == UnconnectedMessageType.BasicMessage)
        {
            var responseKey = reader.GetString();
            if (responseKey == NetworkConfig.DiscoveryKey)
            {
                Console.WriteLine($"[NetworkClient] Discovered server at: {remoteEndPoint}");
                OnServerDiscovered?.Invoke(remoteEndPoint);
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