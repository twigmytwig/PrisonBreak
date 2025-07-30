using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PrisonBreak.ECS;
using PrisonBreak.Systems;
using PrisonBreak.Scenes;
using PrisonBreak.Multiplayer.Core;
using PrisonBreak.Multiplayer.Messages;
using PrisonBreak.Core.Networking;

namespace PrisonBreak.Managers;

// Central networking coordinator that integrates with existing game lifecycle
public class NetworkManager : IGameSystem
{
    private static NetworkManager _instance;
    private static readonly object _lock = new object();
    
    private readonly EventBus _eventBus;
    private ComponentEntityManager _entityManager;
    
    // Network state
    public NetworkConfig.GameMode CurrentGameMode { get; private set; }
    public NetworkConfig.ConnectionState ConnectionState { get; private set; }
    
    // Singleton instance property
    public static NetworkManager Instance
    {
        get
        {
            if (_instance == null)
            {
                throw new InvalidOperationException("NetworkManager has not been initialized. Call Initialize() first.");
            }
            return _instance;
        }
    }
    
    /// <summary>
    /// Update the EntityManager reference when transitioning between scenes
    /// </summary>
    public void UpdateEntityManager(ComponentEntityManager entityManager)
    {
        _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
        Console.WriteLine("[NetworkManager] Updated EntityManager reference for new scene");
    }
    
    /// <summary>
    /// Get the local player's network ID
    /// </summary>
    public int GetLocalPlayerId()
    {
        return _localPlayerId;
    }
    
    // Network components
    private NetworkClient? _networkClient;
    private NetworkServer? _networkServer;
    private string _hostAddress; // Store host address for client connections
    
    // Player identification
    private int _localPlayerId = -1;
    
    // Initialization tracking
    private bool _isInitialized = false;
    
    // Message handlers
    private readonly Dictionary<NetworkConfig.MessageType, Action<int, INetworkMessage>> _serverMessageHandlers;
    private readonly Dictionary<NetworkConfig.MessageType, Action<INetworkMessage>> _clientMessageHandlers;
    
    // Static initialization method
    public static NetworkManager CreateInstance(EventBus eventBus, ComponentEntityManager entityManager)
    {
        lock (_lock)
        {
            if (_instance == null)
            {
                _instance = new NetworkManager(eventBus, entityManager);
            }
            return _instance;
        }
    }
    
    // Private constructor for singleton
    private NetworkManager(EventBus eventBus, ComponentEntityManager entityManager)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
        
        CurrentGameMode = NetworkConfig.GameMode.SinglePlayer;
        ConnectionState = NetworkConfig.ConnectionState.Disconnected;
        
        // Initialize message handlers
        _serverMessageHandlers = new Dictionary<NetworkConfig.MessageType, Action<int, INetworkMessage>>();
        _clientMessageHandlers = new Dictionary<NetworkConfig.MessageType, Action<INetworkMessage>>();
        
        RegisterMessageHandlers();
    }
    
    #region IGameSystem Implementation
    
    public void Initialize()
    {
        if (_isInitialized)
        {
            Console.WriteLine("[NetworkManager] Already initialized, skipping duplicate initialization");
            return;
        }
        
        // Always subscribe to events regardless of game mode
        _eventBus.Subscribe<PlayerInputEvent>(OnPlayerInput);
        _eventBus.Subscribe<EntityCollisionEvent>(OnEntityCollision);
        _eventBus.Subscribe<ItemTransferEvent>(OnItemTransfer);
        
        // Initialize network components based on current game mode
        InitializeNetworkComponents();
        
        _isInitialized = true;
        Console.WriteLine("[NetworkManager] Initialization completed");
    }
    
    public void Update(GameTime gameTime)
    {
        // Update network components based on game mode
        switch (CurrentGameMode)
        {
            case NetworkConfig.GameMode.SinglePlayer:
                // No network updates needed
                break;
                
            case NetworkConfig.GameMode.LocalHost:
                _networkServer?.PollEvents();
                _networkClient?.PollEvents(); // Host is also a client
                break;
                
            case NetworkConfig.GameMode.Client:
                _networkClient?.PollEvents();
                break;
        }
    }
    
    public void Draw(SpriteBatch spriteBatch)
    {
        // NetworkManager doesn't render anything
    }
    
    public void Shutdown()
    {
        // Only unsubscribe when completely shutting down the system
        _eventBus.Unsubscribe<PlayerInputEvent>(OnPlayerInput);
        _eventBus.Unsubscribe<EntityCollisionEvent>(OnEntityCollision);
        _eventBus.Unsubscribe<ItemTransferEvent>(OnItemTransfer);
        
        // Cleanup network connections
        CleanupNetworkComponents();
        
        CurrentGameMode = NetworkConfig.GameMode.SinglePlayer;
        ConnectionState = NetworkConfig.ConnectionState.Disconnected;
    }
    
    #endregion
    
    #region Game Mode Management
    
    public void StartSinglePlayer()
    {
        // Just cleanup network components, stay subscribed to events
        CleanupNetworkComponents();
        CurrentGameMode = NetworkConfig.GameMode.SinglePlayer;
        ConnectionState = NetworkConfig.ConnectionState.Disconnected;
    }
    
    public void StartHost()
    {
        // Cleanup any existing network connections first
        CleanupNetworkComponents();
        CurrentGameMode = NetworkConfig.GameMode.LocalHost;
        InitializeNetworkComponents();
    }
    
    public void ConnectToHost(string hostAddress)
    {
        // Cleanup any existing network connections first
        CleanupNetworkComponents();
        CurrentGameMode = NetworkConfig.GameMode.Client;
        _hostAddress = hostAddress;
        InitializeNetworkComponents();
    }
    
    #endregion
    
    #region Network Component Management
    
    private void InitializeNetworkComponents()
    {
        switch (CurrentGameMode)
        {
            case NetworkConfig.GameMode.SinglePlayer:
                // No network initialization needed
                break;
                
            case NetworkConfig.GameMode.LocalHost:
                InitializeAsHost();
                break;
                
            case NetworkConfig.GameMode.Client:
                InitializeAsClient();
                break;
        }
    }
    
    private void CleanupNetworkComponents()
    {
        _networkClient?.Disconnect();
        _networkServer?.Stop();
        _networkClient?.Dispose();
        _networkServer?.Dispose();
        _networkClient = null;
        _networkServer = null;
        _hostAddress = null;
    }
    
    private void InitializeAsHost()
    {
        ConnectionState = NetworkConfig.ConnectionState.Connecting;
        
        // Create and start server
        _networkServer = new NetworkServer();
        _networkServer.OnStarted += OnServerStarted;
        _networkServer.OnStopped += OnServerStopped;
        _networkServer.OnClientConnected += OnClientConnected;
        _networkServer.OnClientDisconnected += OnClientDisconnected;
        _networkServer.OnMessageReceived += OnServerMessageReceived;
        
        // Create client for local player (host is also a client)
        _networkClient = new NetworkClient();
        _networkClient.OnConnected += OnClientConnected;
        _networkClient.OnDisconnected += OnClientDisconnected;
        _networkClient.OnMessageReceived += OnClientMessageReceived;
        
        // Start server
        bool serverStarted = _networkServer.Start();
        if (serverStarted)
        {
            // Connect local client to own server
            _networkClient.ConnectToHost("127.0.0.1");
        }
        else
        {
            ConnectionState = NetworkConfig.ConnectionState.Disconnected;
            CleanupNetworkComponents();
        }
    }
    
    private void InitializeAsClient()
    {
        ConnectionState = NetworkConfig.ConnectionState.Connecting;
        
        // Create client and connect to host
        _networkClient = new NetworkClient();
        _networkClient.OnConnected += OnClientConnected;
        _networkClient.OnDisconnected += OnClientDisconnected;
        _networkClient.OnMessageReceived += OnClientMessageReceived;
        
        // Connect to the specified host
        if (!string.IsNullOrEmpty(_hostAddress))
        {
            _networkClient.ConnectToHost(_hostAddress);
        }
        else
        {
            Console.WriteLine("[NetworkManager] No host address specified for client connection");
            ConnectionState = NetworkConfig.ConnectionState.Disconnected;
        }
    }
    
    #endregion
    
    #region Public Network Message Methods
    
    /// <summary>
    /// Send player join lobby message to server
    /// </summary>
    public void SendPlayerJoinLobby(int playerId, string playerName)
    {
        if (CurrentGameMode == NetworkConfig.GameMode.SinglePlayer)
            return;
            
        var message = new PlayerJoinLobbyMessage(playerId, playerName);
        
        if (CurrentGameMode == NetworkConfig.GameMode.Client)
        {
            // As client, send to server
            _networkClient?.SendMessage(message);
        }
        // Note: Host doesn't need to send this message to itself
    }
    
    /// <summary>
    /// Send character selection to other players
    /// </summary>
    public void SendCharacterSelection(int playerId, PlayerType selectedType)
    {
        if (CurrentGameMode == NetworkConfig.GameMode.SinglePlayer)
            return;
            
        var message = new PlayerCharacterSelectMessage(playerId, selectedType);
        
        if (CurrentGameMode == NetworkConfig.GameMode.LocalHost)
        {
            // As host, broadcast to all clients
            _networkServer?.BroadcastMessage(message);
        }
        else if (CurrentGameMode == NetworkConfig.GameMode.Client)
        {
            // As client, send to server
            _networkClient?.SendMessage(message);
        }
    }
    
    /// <summary>
    /// Send ready state change to other players
    /// </summary>
    public void SendReadyState(int playerId, bool isReady, PlayerType selectedType)
    {
        if (CurrentGameMode == NetworkConfig.GameMode.SinglePlayer)
            return;
            
        var message = new PlayerReadyStateMessage(playerId, isReady, selectedType);
        
        if (CurrentGameMode == NetworkConfig.GameMode.LocalHost)
        {
            // As host, broadcast to all clients
            _networkServer?.BroadcastMessage(message);
        }
        else if (CurrentGameMode == NetworkConfig.GameMode.Client)
        {
            // As client, send to server
            _networkClient?.SendMessage(message);
        }
    }
    
    /// <summary>
    /// Send game start message to all clients (host only)
    /// </summary>
    public void SendGameStart(GameStartPlayerData[] playerData)
    {
        if (CurrentGameMode != NetworkConfig.GameMode.LocalHost)
            return;
            
        var message = new GameStartMessage(playerData);
        _networkServer?.BroadcastMessage(message);
    }
    
    /// <summary>
    /// Send transform update to other players
    /// </summary>
    public void SendTransformUpdate(TransformMessage transformMessage)
    {
        if (CurrentGameMode == NetworkConfig.GameMode.SinglePlayer)
            return;
            
        if (CurrentGameMode == NetworkConfig.GameMode.LocalHost)
        {
            // Host broadcasts to all clients
            _networkServer?.BroadcastMessage(transformMessage);
        }
        else if (CurrentGameMode == NetworkConfig.GameMode.Client)
        {
            // Client sends to host
            _networkClient?.SendMessage(transformMessage);
        }
    }
    
    #endregion
    
    #region Event Handlers - Always subscribed, filter by game mode
    
    private void OnPlayerInput(PlayerInputEvent inputEvent)
    {
        // Only process network events in multiplayer modes
        if (CurrentGameMode == NetworkConfig.GameMode.SinglePlayer)
            return;
            
        // TODO: Convert to network message and send
    }
    
    private void OnEntityCollision(EntityCollisionEvent collisionEvent)
    {
        // Only process network events in multiplayer modes
        if (CurrentGameMode == NetworkConfig.GameMode.SinglePlayer)
            return;
            
        // TODO: Convert to network message and send
    }
    
    private void OnItemTransfer(ItemTransferEvent transferEvent)
    {
        // Only process network events in multiplayer modes
        if (CurrentGameMode == NetworkConfig.GameMode.SinglePlayer)
            return;
            
        // TODO: Convert to network message and send
    }
    
    #endregion
    
    #region Message Handler Registration
    
    /// <summary>
    /// Register all message handlers for server and client
    /// </summary>
    private void RegisterMessageHandlers()
    {
        // Server message handlers (received from clients)
        _serverMessageHandlers[NetworkConfig.MessageType.PlayerJoinLobby] = HandleServerPlayerJoinLobby;
        _serverMessageHandlers[NetworkConfig.MessageType.PlayerLeaveLobby] = HandleServerPlayerLeaveLobby;
        _serverMessageHandlers[NetworkConfig.MessageType.PlayerCharacterSelect] = HandleServerPlayerCharacterSelect;
        _serverMessageHandlers[NetworkConfig.MessageType.PlayerReadyState] = HandleServerPlayerReadyState;
        _serverMessageHandlers[NetworkConfig.MessageType.Transform] = HandleServerTransform;
        
        // Client message handlers (received from server)
        _clientMessageHandlers[NetworkConfig.MessageType.Welcome] = HandleClientWelcome;
        _clientMessageHandlers[NetworkConfig.MessageType.PlayerJoinLobby] = HandleClientPlayerJoinLobby;
        _clientMessageHandlers[NetworkConfig.MessageType.PlayerLeaveLobby] = HandleClientPlayerLeaveLobby;
        _clientMessageHandlers[NetworkConfig.MessageType.PlayerCharacterSelect] = HandleClientPlayerCharacterSelect;
        _clientMessageHandlers[NetworkConfig.MessageType.PlayerReadyState] = HandleClientPlayerReadyState;
        _clientMessageHandlers[NetworkConfig.MessageType.LobbyState] = HandleClientLobbyState;
        _clientMessageHandlers[NetworkConfig.MessageType.GameStart] = HandleClientGameStart;
        _clientMessageHandlers[NetworkConfig.MessageType.Transform] = HandleClientTransform;
    }
    
    #endregion
    
    #region Server Message Handlers
    
    private void HandleServerPlayerJoinLobby(int clientId, INetworkMessage message)
    {
        var joinMsg = (PlayerJoinLobbyMessage)message;
        Console.WriteLine($"[NetworkManager] Client {clientId} joining lobby as player {joinMsg.PlayerId}");
        
        _eventBus.Send(new PlayerJoinedLobbyEvent
        {
            PlayerId = joinMsg.PlayerId,
            PlayerName = joinMsg.PlayerName
        });
        
        // Broadcast to all other clients
        _networkServer?.BroadcastMessageExcept(clientId, joinMsg);
    }
    
    private void HandleServerPlayerLeaveLobby(int clientId, INetworkMessage message)
    {
        var leaveMsg = (PlayerLeaveLobbyMessage)message;
        Console.WriteLine($"[NetworkManager] Client {clientId} leaving lobby: {leaveMsg.Reason}");
        
        _eventBus.Send(new PlayerLeftLobbyEvent
        {
            PlayerId = leaveMsg.PlayerId,
            Reason = leaveMsg.Reason
        });
        
        // Broadcast to all other clients
        _networkServer?.BroadcastMessageExcept(clientId, leaveMsg);
    }
    
    private void HandleServerPlayerCharacterSelect(int clientId, INetworkMessage message)
    {
        var charMsg = (PlayerCharacterSelectMessage)message;
        Console.WriteLine($"[NetworkManager] Client {clientId} selected character: {charMsg.SelectedPlayerType}");
        
        // Update server's UI to show client's character change
        _eventBus.Send(new PlayerReadyChangedEvent
        {
            PlayerId = charMsg.PlayerId,
            IsReady = false, // Reset ready state when changing character
            SelectedPlayerType = charMsg.SelectedPlayerType
        });
        
        // Broadcast to all other clients
        _networkServer?.BroadcastMessageExcept(clientId, charMsg);
    }
    
    private void HandleServerPlayerReadyState(int clientId, INetworkMessage message)
    {
        var readyMsg = (PlayerReadyStateMessage)message;
        Console.WriteLine($"[NetworkManager] Client {clientId} ready state: {readyMsg.IsReady}");
        
        // Update server's UI to show client's ready state change
        _eventBus.Send(new PlayerReadyChangedEvent
        {
            PlayerId = readyMsg.PlayerId,
            IsReady = readyMsg.IsReady,
            SelectedPlayerType = readyMsg.SelectedPlayerType
        });
        
        // Broadcast to all other clients
        _networkServer?.BroadcastMessageExcept(clientId, readyMsg);
    }
    
    private void HandleServerTransform(int clientId, INetworkMessage message)
    {
        var transformMsg = (TransformMessage)message;
        Console.WriteLine($"[NetworkManager] Server received transform from client {clientId} for entity {transformMsg.EntityId}");
        
        // Server broadcasts the transform update to all other clients
        _networkServer?.BroadcastMessageExcept(clientId, transformMsg);
    }
    
    #endregion
    
    #region Client Message Handlers
    
    private void HandleClientWelcome(INetworkMessage message)
    {
        var welcomeMsg = (PrisonBreak.Multiplayer.Messages.WelcomeMessage)message;
        Console.WriteLine($"[NetworkManager] Received welcome message - assigned player ID: {welcomeMsg.AssignedPlayerId}");
        
        // Store the assigned player ID
        _localPlayerId = welcomeMsg.AssignedPlayerId;
        
        // Update connection event with correct player ID
        _eventBus.Send(new NetworkConnectionEvent 
        { 
            Type = NetworkConnectionType.Connected, 
            PlayerId = _localPlayerId,
            Reason = welcomeMsg.WelcomeText 
        });
    }
    
    private void HandleClientPlayerJoinLobby(INetworkMessage message)
    {
        var joinMsg = (PlayerJoinLobbyMessage)message;
        Console.WriteLine($"[NetworkManager] Player {joinMsg.PlayerId} joined lobby: {joinMsg.PlayerName}");
        
        _eventBus.Send(new PlayerJoinedLobbyEvent
        {
            PlayerId = joinMsg.PlayerId,
            PlayerName = joinMsg.PlayerName
        });
    }
    
    private void HandleClientPlayerLeaveLobby(INetworkMessage message)
    {
        var leaveMsg = (PlayerLeaveLobbyMessage)message;
        Console.WriteLine($"[NetworkManager] Player {leaveMsg.PlayerId} left lobby: {leaveMsg.Reason}");
        
        _eventBus.Send(new PlayerLeftLobbyEvent
        {
            PlayerId = leaveMsg.PlayerId,
            Reason = leaveMsg.Reason
        });
    }
    
    private void HandleClientPlayerCharacterSelect(INetworkMessage message)
    {
        var charMsg = (PlayerCharacterSelectMessage)message;
        Console.WriteLine($"[NetworkManager] Player {charMsg.PlayerId} selected character: {charMsg.SelectedPlayerType}");
        
        _eventBus.Send(new PlayerReadyChangedEvent
        {
            PlayerId = charMsg.PlayerId,
            IsReady = false, // Reset ready state when changing character
            SelectedPlayerType = charMsg.SelectedPlayerType
        });
    }
    
    private void HandleClientPlayerReadyState(INetworkMessage message)
    {
        var readyMsg = (PlayerReadyStateMessage)message;
        Console.WriteLine($"[NetworkManager] Player {readyMsg.PlayerId} ready state: {readyMsg.IsReady}");
        
        _eventBus.Send(new PlayerReadyChangedEvent
        {
            PlayerId = readyMsg.PlayerId,
            IsReady = readyMsg.IsReady,
            SelectedPlayerType = readyMsg.SelectedPlayerType
        });
    }
    
    private void HandleClientLobbyState(INetworkMessage message)
    {
        var lobbyMsg = (LobbyStateMessage)message;
        Console.WriteLine($"[NetworkManager] Received full lobby state with {lobbyMsg.Players.Length} players");
        
        // Send join events for each player in the lobby state
        foreach (var player in lobbyMsg.Players)
        {
            _eventBus.Send(new PlayerJoinedLobbyEvent
            {
                PlayerId = player.PlayerId,
                PlayerName = player.PlayerName
            });
            
            // Send ready state if player has selected character type
            if (player.HasSelectedPlayerType)
            {
                _eventBus.Send(new PlayerReadyChangedEvent
                {
                    PlayerId = player.PlayerId,
                    IsReady = player.IsReady,
                    SelectedPlayerType = player.SelectedPlayerType
                });
            }
        }
    }
    
    private void HandleClientGameStart(INetworkMessage message)
    {
        var gameStartMsg = (GameStartMessage)message;
        Console.WriteLine($"[NetworkManager] Game starting with {gameStartMsg.PlayerStartData.Length} players");
        
        // Find our player's start data using the assigned player ID
        var ourPlayerData = gameStartMsg.PlayerStartData.FirstOrDefault(p => p.PlayerId == _localPlayerId);
        
        if (ourPlayerData.PlayerId == _localPlayerId) // Check if we found our data
        {
            // Use the new multiplayer constructor to pass all player data
            var gameStartData = new GameStartData(
                localPlayerType: ourPlayerData.PlayerType,
                localPlayerIndex: ourPlayerData.PlayerIndex,
                allPlayersData: gameStartMsg.PlayerStartData,
                localPlayerId: _localPlayerId
            );
            
            Console.WriteLine($"[NetworkManager] Starting game as {ourPlayerData.PlayerType} with player index {ourPlayerData.PlayerIndex}");
            Console.WriteLine($"[NetworkManager] Passing complete player data for {gameStartMsg.PlayerStartData.Length} players to GameplayScene");
            _eventBus.Send(new SceneTransitionEvent(SceneType.MultiplayerLobby, SceneType.Gameplay, gameStartData));
        }
        else
        {
            Console.WriteLine($"[NetworkManager] ERROR: Could not find game start data for local player ID {_localPlayerId}");
        }
    }
    
    private void HandleClientTransform(INetworkMessage message)
    {
        var transformMsg = (TransformMessage)message;
        
        // Find the entity by NetworkComponent.NetworkId (since that's what TransformMessage.EntityId contains)
        var networkedEntities = _entityManager.GetEntitiesWith<NetworkComponent, TransformComponent>();
        var entity = networkedEntities.FirstOrDefault(e => e.GetComponent<NetworkComponent>().NetworkId == transformMsg.EntityId);
        
        if (entity == null)
        {
            return;
        }
        
        var networkComp = entity.GetComponent<NetworkComponent>();
        
        // Ignore updates for entities we own (prevent feedback loops)
        // Check both OwnerId and NetworkId to ensure we don't override our own player
        if ((networkComp.Authority == NetworkConfig.NetworkAuthority.Client && networkComp.OwnerId == _localPlayerId) ||
            (transformMsg.EntityId == _localPlayerId))
        {
            return;
        }
        
        // Apply the networked transform (only if it's different to avoid unnecessary updates)
        var currentTransform = entity.GetComponent<TransformComponent>();
        var newTransform = transformMsg.ToComponent();
        
        if (Vector2.Distance(currentTransform.Position, newTransform.Position) > 0.1f)
        {
            entity.AddComponent(newTransform); // AddComponent replaces existing component for structs
        }
    }
    
    #endregion
    
    #region Network Event Handlers
    
    private void OnServerStarted()
    {
        Console.WriteLine("[NetworkManager] Server started successfully");
    }
    
    private void OnServerStopped()
    {
        Console.WriteLine("[NetworkManager] Server stopped");
        ConnectionState = NetworkConfig.ConnectionState.Disconnected;
    }
    
    private void OnClientConnected(int clientId)
    {
        Console.WriteLine($"[NetworkManager] Client {clientId} connected to server");
    }
    
    private void OnClientConnected()
    {
        Console.WriteLine("[NetworkManager] Connected to server as client");
        ConnectionState = NetworkConfig.ConnectionState.Connected;
        
        // Don't send NetworkConnectionEvent here - wait for Welcome message with actual player ID
        // The HandleClientWelcome method will send the event with the correct player ID
    }
    
    private void OnClientDisconnected(int clientId, string reason)
    {
        Console.WriteLine($"[NetworkManager] Client {clientId} disconnected: {reason}");
    }
    
    private void OnClientDisconnected(string reason)
    {
        Console.WriteLine($"[NetworkManager] Disconnected from server: {reason}");
        ConnectionState = NetworkConfig.ConnectionState.Disconnected;
        
        // Send network disconnection event to UI
        _eventBus.Send(new NetworkConnectionEvent 
        { 
            Type = NetworkConnectionType.Disconnected, 
            PlayerId = -1,
            Reason = reason 
        });
    }
    
    private void OnServerMessageReceived(int clientId, PrisonBreak.Multiplayer.Messages.INetworkMessage message)
    {
        Console.WriteLine($"[NetworkManager] Received message from client {clientId}: {message.Type}");
        
        // Handle UnknownNetworkMessage by deserializing it properly
        if (message is UnknownNetworkMessage unknownMsg)
        {
            var deserializedMessage = DeserializeGameMessage(unknownMsg.Type, unknownMsg.RawData);
            if (deserializedMessage != null)
            {
                message = deserializedMessage;
            }
        }
        
        // Use handler dictionary for cleaner message processing
        if (_serverMessageHandlers.TryGetValue(message.Type, out var handler))
        {
            try
            {
                handler.Invoke(clientId, message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NetworkManager] Error handling server message {message.Type}: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine($"[NetworkManager] No server handler registered for message type: {message.Type}");
        }
    }
    
    private void OnClientMessageReceived(PrisonBreak.Multiplayer.Messages.INetworkMessage message)
    {
        Console.WriteLine($"[NetworkManager] Received message from server: {message.Type}");
        
        // Handle UnknownNetworkMessage by deserializing it properly
        if (message is UnknownNetworkMessage unknownMsg)
        {
            var deserializedMessage = DeserializeGameMessage(unknownMsg.Type, unknownMsg.RawData);
            if (deserializedMessage != null)
            {
                message = deserializedMessage;
            }
        }
        
        // Use handler dictionary for cleaner message processing
        if (_clientMessageHandlers.TryGetValue(message.Type, out var handler))
        {
            try
            {
                handler.Invoke(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NetworkManager] Error handling client message {message.Type}: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine($"[NetworkManager] No client handler registered for message type: {message.Type}");
        }
    }
    
    #endregion
    
    #region Message Deserialization
    
    /// <summary>
    /// Deserialize game-specific messages that the pure networking library can't handle
    /// </summary>
    private PrisonBreak.Multiplayer.Messages.INetworkMessage DeserializeGameMessage(NetworkConfig.MessageType messageType, byte[] rawData)
    {
        try
        {
            // Create a reader for the raw data
            var reader = new LiteNetLib.Utils.NetDataReader(rawData);
            
            // Create the appropriate message instance
            PrisonBreak.Multiplayer.Messages.INetworkMessage message = messageType switch
            {
                NetworkConfig.MessageType.Welcome => new PrisonBreak.Multiplayer.Messages.WelcomeMessage(),
                NetworkConfig.MessageType.PlayerJoinLobby => new PlayerJoinLobbyMessage(),
                NetworkConfig.MessageType.PlayerLeaveLobby => new PlayerLeaveLobbyMessage(),
                NetworkConfig.MessageType.PlayerCharacterSelect => new PlayerCharacterSelectMessage(),
                NetworkConfig.MessageType.PlayerReadyState => new PlayerReadyStateMessage(),
                NetworkConfig.MessageType.LobbyState => new LobbyStateMessage(),
                NetworkConfig.MessageType.GameStart => new GameStartMessage(),
                NetworkConfig.MessageType.Transform => new TransformMessage(),
                _ => null
            };
            
            if (message != null)
            {
                message.Deserialize(reader);
                return message;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NetworkManager] Error deserializing game message {messageType}: {ex.Message}");
        }
        
        return null;
    }
    
    #endregion
}