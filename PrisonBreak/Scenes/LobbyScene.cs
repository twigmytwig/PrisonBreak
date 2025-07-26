using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using PrisonBreak.ECS;
using PrisonBreak.ECS.Systems;
using PrisonBreak.Network;

namespace PrisonBreak.Scenes;

/// <summary>
/// Multiplayer lobby scene for host/join operations and player management
/// </summary>
public class LobbyScene : Scene, ITransitionDataReceiver
{
    // Network management
    private NetworkManager _networkManager;
    private NetworkEventBridge _networkEventBridge;
    private bool _networkInitialized = false;
    
    // Scene systems
    private MenuInputSystem _menuInputSystem;
    private MenuRenderSystem _menuRenderSystem;
    
    // Lobby state
    private LobbyStartData? _lobbyStartData;
    private Dictionary<int, LobbyPlayer> _connectedPlayers = new();
    private bool _isHost = false;
    private bool _isConnected = false;
    private string _connectionStatus = "Connecting...";
    
    // UI entities
    private Entity _titleEntity;
    private Entity _statusEntity;
    private Entity _playersListTitleEntity;
    private Entity _localPlayerEntity;
    private Entity _instructionsEntity;
    private Entity _startGameEntity;
    private Entity _backToMenuEntity;
    private List<Entity> _playerListEntities = new();
    
    // UI state
    private bool _entitiesCreated = false;
    private int _selectedIndex = 0; // 0 = character selection, 1 = start game, 2 = back
    private readonly string[] _menuOptions = { "Character Selection", "Start Game", "Back to Menu" };

    public LobbyScene(EventBus eventBus) : base("Lobby", eventBus)
    {
    }

    public void ReceiveTransitionData(object data)
    {
        if (data is LobbyStartData lobbyData)
        {
            _lobbyStartData = lobbyData;
            _isHost = lobbyData.IsHost;
            Console.WriteLine($"LobbyScene received data: IsHost={lobbyData.IsHost}, PlayerName={lobbyData.PlayerName}");
        }
    }

    protected override void SetupSystems()
    {
        // Create menu systems for UI
        _menuInputSystem = new MenuInputSystem();
        _menuRenderSystem = new MenuRenderSystem();

        // Set up system dependencies
        _menuInputSystem.SetEntityManager(EntityManager);
        _menuInputSystem.SetEventBus(EventBus);

        _menuRenderSystem.SetEntityManager(EntityManager);
        _menuRenderSystem.SetEventBus(EventBus);

        // Add systems to manager
        SystemManager.AddSystem(_menuInputSystem);
        SystemManager.AddSystem(_menuRenderSystem);

        // Subscribe to menu navigation events
        EventBus.Subscribe<MenuNavigationEvent>(OnMenuNavigation);
    }

    protected override void LoadSceneContent()
    {
        // Set content for render system
        _menuRenderSystem.SetContent(Content);
        
        // Initialize networking if we have lobby data
        if (_lobbyStartData.HasValue)
        {
            InitializeNetworking();
        }
    }

    private void InitializeNetworking()
    {
        if (_networkInitialized) return;

        try
        {
            // Create network manager
            _networkManager = new NetworkManager(EventBus);
            _networkEventBridge = new NetworkEventBridge(_networkManager, EventBus);

            // Subscribe to network events
            EventBus.Subscribe<NetworkConnectionEvent>(OnNetworkConnection);
            EventBus.Subscribe<NetworkPlayerJoinEvent>(OnNetworkPlayerJoin);
            EventBus.Subscribe<NetworkPlayerLeaveEvent>(OnNetworkPlayerLeave);
            EventBus.Subscribe<NetworkLobbyUpdateEvent>(OnNetworkLobbyUpdate);
            EventBus.Subscribe<NetworkGameStateUpdateEvent>(OnNetworkGameStateUpdate);

            var lobbyData = _lobbyStartData.Value;

            if (lobbyData.IsHost)
            {
                // Start hosting
                bool success = _networkManager.StartHost(lobbyData.Port, lobbyData.PlayerName);
                if (success)
                {
                    _connectionStatus = $"Hosting on {_networkManager.GetLocalIPAddress()}:{lobbyData.Port}";
                    _isConnected = true;
                    
                    // Add local player (host) to lobby
                    var hostPlayer = new LobbyPlayer
                    {
                        PlayerId = 0,
                        Name = lobbyData.PlayerName,
                        SelectedType = lobbyData.InitialPlayerType,
                        IsReady = true, // Host is always ready
                        IsHost = true
                    };
                    _connectedPlayers[0] = hostPlayer;
                }
                else
                {
                    _connectionStatus = "Failed to start host";
                }
            }
            else
            {
                // Connect to host
                bool success = _networkManager.ConnectToHost(lobbyData.HostIP, lobbyData.Port, lobbyData.PlayerName);
                if (success)
                {
                    _connectionStatus = $"Connecting to {lobbyData.HostIP}:{lobbyData.Port}...";
                }
                else
                {
                    _connectionStatus = "Failed to connect";
                }
            }

            _networkInitialized = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LobbyScene] Error initializing networking: {ex.Message}");
            _connectionStatus = $"Network error: {ex.Message}";
        }
    }

    private void CreateLobbyUI()
    {
        var graphicsDevice = PrisonBreak.Core.Core.GraphicsDevice;
        if (graphicsDevice == null) return;

        int screenWidth = graphicsDevice.PresentationParameters.BackBufferWidth;
        int screenHeight = graphicsDevice.PresentationParameters.BackBufferHeight;
        Vector2 screenCenter = new Vector2(screenWidth / 2, screenHeight / 2);

        // Title
        _titleEntity = EntityManager.CreateEntity();
        _titleEntity.AddComponent(new TransformComponent(new Vector2(screenCenter.X, 100)));
        _titleEntity.AddComponent(new TextComponent("MULTIPLAYER LOBBY")
        {
            Color = Color.White,
            Alignment = TextAlignment.Center,
            DrawOrder = 10
        });

        // Connection Status
        _statusEntity = EntityManager.CreateEntity();
        _statusEntity.AddComponent(new TransformComponent(new Vector2(screenCenter.X, 150)));
        _statusEntity.AddComponent(new TextComponent(_connectionStatus)
        {
            Color = _isConnected ? Color.Green : Color.Yellow,
            Alignment = TextAlignment.Center,
            DrawOrder = 10
        });

        // Players List Title
        _playersListTitleEntity = EntityManager.CreateEntity();
        _playersListTitleEntity.AddComponent(new TransformComponent(new Vector2(screenCenter.X, 200)));
        _playersListTitleEntity.AddComponent(new TextComponent("CONNECTED PLAYERS")
        {
            Color = Color.Cyan,
            Alignment = TextAlignment.Center,
            DrawOrder = 10
        });

        // Local Player Display (with character selection)
        _localPlayerEntity = EntityManager.CreateEntity();
        _localPlayerEntity.AddComponent(new TransformComponent(new Vector2(screenCenter.X, 250)));
        _localPlayerEntity.AddComponent(new MenuItemComponent(400, 40, "local_player")
        {
            DrawOrder = 0
        });
        
        var localPlayerText = GetLocalPlayerDisplayText();
        _localPlayerEntity.AddComponent(new TextComponent(localPlayerText)
        {
            Color = _selectedIndex == 0 ? Color.Yellow : Color.White,
            Alignment = TextAlignment.Center,
            DrawOrder = 10
        });

        // Instructions
        _instructionsEntity = EntityManager.CreateEntity();
        _instructionsEntity.AddComponent(new TransformComponent(new Vector2(screenCenter.X, screenHeight - 150)));
        _instructionsEntity.AddComponent(new TextComponent("Use UP/DOWN to navigate, LEFT/RIGHT to change character, ENTER to select")
        {
            Color = Color.Gray,
            Alignment = TextAlignment.Center,
            DrawOrder = 10
        });

        // Start Game Button (only for host)
        if (_isHost)
        {
            _startGameEntity = EntityManager.CreateEntity();
            _startGameEntity.AddComponent(new TransformComponent(new Vector2(screenCenter.X, screenHeight - 100)));
            _startGameEntity.AddComponent(new MenuItemComponent(200, 40, "start_game")
            {
                DrawOrder = 0
            });
            _startGameEntity.AddComponent(new TextComponent("Start Game")
            {
                Color = _selectedIndex == 1 ? Color.Yellow : Color.White,
                Alignment = TextAlignment.Center,
                DrawOrder = 10
            });
        }

        // Back to Menu Button
        _backToMenuEntity = EntityManager.CreateEntity();
        var backButtonY = _isHost ? screenHeight - 50 : screenHeight - 100;
        _backToMenuEntity.AddComponent(new TransformComponent(new Vector2(screenCenter.X, backButtonY)));
        _backToMenuEntity.AddComponent(new MenuItemComponent(200, 40, "back_menu")
        {
            DrawOrder = 0
        });
        _backToMenuEntity.AddComponent(new TextComponent("Back to Menu")
        {
            Color = (_selectedIndex == (_isHost ? 2 : 1)) ? Color.Yellow : Color.White,
            Alignment = TextAlignment.Center,
            DrawOrder = 10
        });

        UpdatePlayersList();
        _entitiesCreated = true;
    }

    private string GetLocalPlayerDisplayText()
    {
        if (!_lobbyStartData.HasValue) return "Unknown Player";
        
        var lobbyData = _lobbyStartData.Value;
        var roleText = _isHost ? " (Host)" : "";
        return $"You: {lobbyData.PlayerName} - {lobbyData.InitialPlayerType}{roleText} (Press LEFT/RIGHT to change)";
    }

    private void UpdatePlayersList()
    {
        // Clear existing player list entities
        foreach (var entity in _playerListEntities)
        {
            EntityManager.DestroyEntity(entity.Id);
        }
        _playerListEntities.Clear();

        // Create entities for each connected player (excluding local player)
        int yOffset = 300;
        foreach (var player in _connectedPlayers.Values.Where(p => p.PlayerId != _networkManager?.LocalPlayerId))
        {
            var playerEntity = EntityManager.CreateEntity();
            playerEntity.AddComponent(new TransformComponent(new Vector2(960, yOffset))); // Center X
            
            var playerText = $"{player.Name} - {player.SelectedType}";
            if (player.IsHost) playerText += " (Host)";
            if (player.IsReady) playerText += " [Ready]";
            
            playerEntity.AddComponent(new TextComponent(playerText)
            {
                Color = player.IsReady ? Color.Green : Color.White,
                Alignment = TextAlignment.Center,
                DrawOrder = 10
            });

            _playerListEntities.Add(playerEntity);
            yOffset += 30;
        }
    }

    public override void Update(GameTime gameTime)
    {
        // Create UI on first update when GraphicsDevice is available
        if (!_entitiesCreated && IsContentLoaded)
        {
            CreateLobbyUI();
        }

        // Update network manager
        if (_networkManager != null)
        {
            _networkManager.Update();
        }

        base.Update(gameTime);
    }

    private void OnMenuNavigation(MenuNavigationEvent navEvent)
    {
        switch (navEvent.Direction)
        {
            case MenuNavigation.Up:
                var maxIndex = _isHost ? 2 : 1; // Host has Start Game option
                _selectedIndex = Math.Max(0, _selectedIndex - 1);
                UpdateMenuSelection();
                break;

            case MenuNavigation.Down:
                var maxIndexDown = _isHost ? 2 : 1;
                _selectedIndex = Math.Min(maxIndexDown, _selectedIndex + 1);
                UpdateMenuSelection();
                break;

            case MenuNavigation.Left:
            case MenuNavigation.Right:
                if (_selectedIndex == 0) // Character selection
                {
                    ToggleCharacterType();
                }
                break;

            case MenuNavigation.Select:
                HandleMenuSelection();
                break;

            case MenuNavigation.Back:
                ReturnToMainMenu();
                break;
        }
    }

    private void UpdateMenuSelection()
    {
        // Update local player selection
        if (_localPlayerEntity?.HasComponent<TextComponent>() == true)
        {
            ref var localText = ref _localPlayerEntity.GetComponent<TextComponent>();
            localText.Color = _selectedIndex == 0 ? Color.Yellow : Color.White;
        }

        // Update start game button (if host)
        if (_isHost && _startGameEntity?.HasComponent<TextComponent>() == true)
        {
            ref var startText = ref _startGameEntity.GetComponent<TextComponent>();
            startText.Color = _selectedIndex == 1 ? Color.Yellow : Color.White;
        }

        // Update back button
        if (_backToMenuEntity?.HasComponent<TextComponent>() == true)
        {
            ref var backText = ref _backToMenuEntity.GetComponent<TextComponent>();
            var backIndex = _isHost ? 2 : 1;
            backText.Color = _selectedIndex == backIndex ? Color.Yellow : Color.White;
        }
    }

    private void ToggleCharacterType()
    {
        if (!_lobbyStartData.HasValue) return;

        var lobbyData = _lobbyStartData.Value;
        lobbyData.InitialPlayerType = lobbyData.InitialPlayerType == PlayerType.Prisoner ? PlayerType.Cop : PlayerType.Prisoner;
        _lobbyStartData = lobbyData;

        // Update local player display
        if (_localPlayerEntity?.HasComponent<TextComponent>() == true)
        {
            ref var textComp = ref _localPlayerEntity.GetComponent<TextComponent>();
            textComp.Text = GetLocalPlayerDisplayText();
        }

        // Send character update to network
        if (_networkManager?.IsConnected == true)
        {
            _networkEventBridge?.SendPlayerCharacterUpdate(_networkManager.LocalPlayerId, lobbyData.InitialPlayerType);
        }

        Console.WriteLine($"[LobbyScene] Changed character type to: {lobbyData.InitialPlayerType}");
    }

    private void HandleMenuSelection()
    {
        switch (_selectedIndex)
        {
            case 0: // Character selection - already handled by left/right
                break;

            case 1: // Start Game (host only) or Back (client)
                if (_isHost)
                {
                    StartMultiplayerGame();
                }
                else
                {
                    ReturnToMainMenu();
                }
                break;

            case 2: // Back to Menu (host only)
                if (_isHost)
                {
                    ReturnToMainMenu();
                }
                break;
        }
    }

    private void StartMultiplayerGame()
    {
        if (!_isHost || !_lobbyStartData.HasValue) return;

        try
        {
            Console.WriteLine("[LobbyScene] HOST: Starting multiplayer game...");
            
            // Send game start message to all clients
            _networkEventBridge?.SendGameStateUpdate(GameStateType.Starting, "Host started the game");
            Console.WriteLine("[LobbyScene] HOST: Sent game start message to clients");

            // Transition to gameplay scene with multiplayer data
            var lobbyData = _lobbyStartData.Value;
            var multiplayerGameData = new MultiplayerGameStartData(
                lobbyData.InitialPlayerType,
                PlayerIndex.One,
                isMultiplayer: true,
                isHost: true,
                _networkManager.LocalPlayerId
            )
            {
                NetworkManager = _networkManager,
                NetworkEventBridge = _networkEventBridge
            };

            EventBus.Send(new SceneTransitionEvent(SceneType.Lobby, SceneType.Gameplay, multiplayerGameData));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LobbyScene] Error starting game: {ex.Message}");
        }
    }

    private void ReturnToMainMenu()
    {
        // Disconnect from network
        _networkManager?.Disconnect();
        EventBus.Send(new SceneTransitionEvent(SceneType.Lobby, SceneType.StartMenu));
    }

    // Network Event Handlers

    private void OnNetworkConnection(NetworkConnectionEvent evt)
    {
        _isConnected = evt.IsConnected;
        _connectionStatus = evt.Message;

        // Update status display
        if (_statusEntity?.HasComponent<TextComponent>() == true)
        {
            ref var statusText = ref _statusEntity.GetComponent<TextComponent>();
            statusText.Text = _connectionStatus;
            statusText.Color = _isConnected ? Color.Green : Color.Red;
        }

        Console.WriteLine($"[LobbyScene] Connection status: {evt.Message}");
    }

    private void OnNetworkPlayerJoin(NetworkPlayerJoinEvent evt)
    {
        var newPlayer = new LobbyPlayer
        {
            PlayerId = evt.PlayerId,
            Name = evt.PlayerName,
            SelectedType = evt.SelectedType,
            IsReady = false,
            IsHost = false
        };

        _connectedPlayers[evt.PlayerId] = newPlayer;
        UpdatePlayersList();

        Console.WriteLine($"[LobbyScene] Player joined: {evt.PlayerName} (ID: {evt.PlayerId})");
    }

    private void OnNetworkPlayerLeave(NetworkPlayerLeaveEvent evt)
    {
        _connectedPlayers.Remove(evt.PlayerId);
        UpdatePlayersList();

        Console.WriteLine($"[LobbyScene] Player left: ID {evt.PlayerId}");
    }

    private void OnNetworkLobbyUpdate(NetworkLobbyUpdateEvent evt)
    {
        // Update connected players list
        _connectedPlayers.Clear();
        foreach (var player in evt.ConnectedPlayers)
        {
            _connectedPlayers[player.PlayerId] = player;
        }

        UpdatePlayersList();
        Console.WriteLine($"[LobbyScene] Lobby updated: {evt.ConnectedPlayers.Length} players");
    }

    private void OnNetworkGameStateUpdate(NetworkGameStateUpdateEvent evt)
    {
        Console.WriteLine($"[LobbyScene] Game state update received: {evt.State} - {evt.AdditionalData}");
        Console.WriteLine($"[LobbyScene] IsHost={_isHost}, HasLobbyData={_lobbyStartData.HasValue}");

        if (evt.State == GameStateType.Starting)
        {
            Console.WriteLine("[LobbyScene] Game is starting...");
            
            // Host started the game - clients should transition to gameplay
            if (!_isHost && _lobbyStartData.HasValue)
            {
                Console.WriteLine("[LobbyScene] CLIENT: Conditions met, calling StartMultiplayerGameAsClient...");
                StartMultiplayerGameAsClient();
            }
            else
            {
                Console.WriteLine($"[LobbyScene] Not transitioning - IsHost={_isHost}, HasLobbyData={_lobbyStartData.HasValue}");
            }
        }
    }

    private void StartMultiplayerGameAsClient()
    {
        if (_isHost || !_lobbyStartData.HasValue) return;

        try
        {
            Console.WriteLine("[LobbyScene] CLIENT: Received game start, transitioning to gameplay...");

            // Get current character selection
            var lobbyData = _lobbyStartData.Value;
            Console.WriteLine($"[LobbyScene] CLIENT: Character={lobbyData.InitialPlayerType}, NetworkID={_networkManager?.LocalPlayerId}");
            
            // Create multiplayer game data for client
            var multiplayerGameData = new MultiplayerGameStartData(
                lobbyData.InitialPlayerType,
                PlayerIndex.Two, // Clients use PlayerIndex.Two for now
                isMultiplayer: true,
                isHost: false,
                _networkManager?.LocalPlayerId ?? -1
            )
            {
                NetworkManager = _networkManager,
                NetworkEventBridge = _networkEventBridge
            };

            // Transition to gameplay scene
            Console.WriteLine("[LobbyScene] CLIENT: Sending scene transition event...");
            EventBus.Send(new SceneTransitionEvent(SceneType.Lobby, SceneType.Gameplay, multiplayerGameData));
            Console.WriteLine("[LobbyScene] CLIENT: Scene transition event sent!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LobbyScene] Error starting client game: {ex.Message}");
        }
    }

    public override void OnEnter()
    {
        Console.WriteLine("Entered Lobby Scene");
        _selectedIndex = 0;

        if (IsContentLoaded && !_entitiesCreated)
        {
            CreateLobbyUI();
        }

        base.OnEnter();
    }

    public override void OnExit()
    {
        Console.WriteLine("Exited Lobby Scene");

        // Don't dispose NetworkManager - it's being passed to GameplayScene
        // The GameplayScene will handle disposal when appropriate
        Console.WriteLine("[LobbyScene] NetworkManager ownership transferred to GameplayScene");

        // Unsubscribe from events
        EventBus.Unsubscribe<MenuNavigationEvent>(OnMenuNavigation);
        EventBus.Unsubscribe<NetworkConnectionEvent>(OnNetworkConnection);
        EventBus.Unsubscribe<NetworkPlayerJoinEvent>(OnNetworkPlayerJoin);
        EventBus.Unsubscribe<NetworkPlayerLeaveEvent>(OnNetworkPlayerLeave);
        EventBus.Unsubscribe<NetworkLobbyUpdateEvent>(OnNetworkLobbyUpdate);
        EventBus.Unsubscribe<NetworkGameStateUpdateEvent>(OnNetworkGameStateUpdate);

        base.OnExit();
    }
}