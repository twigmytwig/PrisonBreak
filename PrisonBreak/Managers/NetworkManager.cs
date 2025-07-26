using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PrisonBreak.ECS;
using PrisonBreak.Systems;
using PrisonBreak.Multiplayer.Core;

namespace PrisonBreak.Managers;

// Central networking coordinator that integrates with existing game lifecycle
public class NetworkManager : IGameSystem
{
    private readonly EventBus _eventBus;
    private readonly ComponentEntityManager _entityManager;
    
    // Network state
    public NetworkConfig.GameMode CurrentGameMode { get; private set; }
    public NetworkConfig.ConnectionState ConnectionState { get; private set; }
    
    // Network components (will be implemented in Phase 2)
    // private NetworkClient? _networkClient;
    // private NetworkServer? _networkServer;
    private string _hostAddress; // Store host address for client connections
    
    public NetworkManager(EventBus eventBus, ComponentEntityManager entityManager)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
        
        CurrentGameMode = NetworkConfig.GameMode.SinglePlayer;
        ConnectionState = NetworkConfig.ConnectionState.Disconnected;
    }
    
    #region IGameSystem Implementation
    
    public void Initialize()
    {
        // Always subscribe to events regardless of game mode
        _eventBus.Subscribe<PlayerInputEvent>(OnPlayerInput);
        _eventBus.Subscribe<EntityCollisionEvent>(OnEntityCollision);
        _eventBus.Subscribe<ItemTransferEvent>(OnItemTransfer);
        
        // Initialize network components based on current game mode
        InitializeNetworkComponents();
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
                // _networkServer?.Update(); // TODO: Implement in Phase 2
                // _networkClient?.Update(); // Host is also a client
                break;
                
            case NetworkConfig.GameMode.Client:
                // _networkClient?.Update(); // TODO: Implement in Phase 2
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
        // _networkClient?.Disconnect(); // TODO: Implement in Phase 2
        // _networkServer?.Stop(); // TODO: Implement in Phase 2
        // _networkClient = null;
        // _networkServer = null;
        _hostAddress = null;
    }
    
    private void InitializeAsHost()
    {
        ConnectionState = NetworkConfig.ConnectionState.Connecting;
        // TODO: Create and start NetworkServer
        // TODO: Create NetworkClient for local player
        ConnectionState = NetworkConfig.ConnectionState.Connected;
    }
    
    private void InitializeAsClient()
    {
        ConnectionState = NetworkConfig.ConnectionState.Connecting;
        // TODO: Create and connect NetworkClient to _hostAddress
        ConnectionState = NetworkConfig.ConnectionState.Connected;
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
}