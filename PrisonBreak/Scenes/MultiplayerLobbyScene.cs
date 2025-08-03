using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PrisonBreak.ECS;
using PrisonBreak.ECS.Systems;
using PrisonBreak.Managers;
using PrisonBreak.Multiplayer.Core;
using PrisonBreak.Core.Networking;

namespace PrisonBreak.Scenes;

/// <summary>
/// Multiplayer lobby scene for hosting/joining games and character selection
/// </summary>
public class MultiplayerLobbyScene : Scene
{
    private MenuInputSystem _menuInputSystem;
    private MenuRenderSystem _menuRenderSystem;
    private NetworkManager _networkManager;

    // UI entities
    private Entity _titleEntity;
    private Entity _hostButtonEntity;
    private Entity _joinButtonEntity;
    private Entity _backButtonEntity;
    private Entity _playerListEntity;
    private Entity _startGameButtonEntity;

    // Lobby state
    private LobbyState _currentState = LobbyState.MainMenu;
    private int _selectedIndex = 0;
    private readonly Dictionary<int, LobbyPlayer> _connectedPlayers = new();
    private int _localPlayerId = -1;
    private bool _isHost = false;
    private bool _entitiesCreated = false;

    // Menu items for different states
    private readonly string[] _mainMenuItems = { "Host Game", "Join Game", "Back to Menu" };
    private readonly string[] _lobbyHostItems = { "Start Game", "Leave Lobby" };
    private readonly string[] _lobbyClientItems = { "Leave Lobby" };

    // Input state tracking
    private KeyboardState _previousKeyboardState;
    private GamePadState _previousGamepadState;

    public MultiplayerLobbyScene(EventBus eventBus) : base("Multiplayer Lobby", eventBus)
    {
    }

    protected override void SetupSystems()
    {
        // Create menu-specific systems
        _menuInputSystem = new MenuInputSystem();
        _menuRenderSystem = new MenuRenderSystem();
        _networkManager = NetworkManager.CreateInstance(EventBus, EntityManager);

        // Set up system dependencies
        _menuInputSystem.SetEntityManager(EntityManager);
        _menuInputSystem.SetEventBus(EventBus);

        _menuRenderSystem.SetEntityManager(EntityManager);
        _menuRenderSystem.SetEventBus(EventBus);

        // Add systems to manager
        SystemManager.AddSystem(_menuInputSystem);
        SystemManager.AddSystem(_networkManager); // Network manager handles connections
        SystemManager.AddSystem(_menuRenderSystem);

        // Subscribe to events
        EventBus.Subscribe<MenuNavigationEvent>(OnMenuNavigation);
        EventBus.Subscribe<NetworkConnectionEvent>(OnNetworkConnection);
        EventBus.Subscribe<PlayerJoinedLobbyEvent>(OnPlayerJoinedLobby);
        EventBus.Subscribe<PlayerLeftLobbyEvent>(OnPlayerLeftLobby);
        EventBus.Subscribe<PlayerReadyChangedEvent>(OnPlayerReadyChanged);
    }

    protected override void LoadSceneContent()
    {
        Console.WriteLine("MultiplayerLobbyScene.LoadSceneContent called");
        // Set content for render system so it can load fonts
        _menuRenderSystem.SetContent(Content);
    }

    public override void OnEnter()
    {
        Console.WriteLine("Entered MultiplayerLobby Scene");
        
        // Initialize input states to current state to prevent false input detection on first frame
        _previousKeyboardState = Keyboard.GetState();
        _previousGamepadState = GamePad.GetState(PlayerIndex.One);
        
        base.OnEnter();
    }

    public override void Update(GameTime gameTime)
    {
        var currentKeyboardState = Keyboard.GetState();
        var currentGamepadState = GamePad.GetState(PlayerIndex.One);

        // Create UI entities on first update
        if (!_entitiesCreated)
        {
            CreateUIEntities();
            _entitiesCreated = true;
        }

        // Handle input based on current lobby state
        HandleLobbyInput(currentKeyboardState, currentGamepadState);

        // Call base update which runs SystemManager.Update
        base.Update(gameTime);

        // Store current input state for next frame
        _previousKeyboardState = currentKeyboardState;
        _previousGamepadState = currentGamepadState;
    }

    #region UI Creation

    private void CreateUIEntities()
    {
        Console.WriteLine("MultiplayerLobbyScene.CreateUIEntities called");
        
        // Get screen center like StartMenuScene does
        var graphicsDevice = PrisonBreak.Core.Core.GraphicsDevice;
        if (graphicsDevice == null)
        {
            Console.WriteLine("ERROR: GraphicsDevice is null in CreateUIEntities");
            return;
        }

        int screenWidth = graphicsDevice.PresentationParameters.BackBufferWidth;
        int screenHeight = graphicsDevice.PresentationParameters.BackBufferHeight;
        Vector2 screenCenter = new Vector2(screenWidth / 2, screenHeight / 2);

        Console.WriteLine($"Screen: {screenWidth}x{screenHeight}, Center: {screenCenter}");
        
        // Title
        _titleEntity = EntityManager.CreateEntity();
        _titleEntity.AddComponent(new TransformComponent(new Vector2(screenCenter.X, screenCenter.Y - 200)));
        _titleEntity.AddComponent(new TextComponent
        {
            Text = "Multiplayer Lobby",
            Color = Color.White,
            Alignment = TextAlignment.Center,
            DrawOrder = 10,
            Visible = true
        });
        
        Console.WriteLine($"Created title entity: {_titleEntity.Id} at position {screenCenter.X}, {screenCenter.Y - 200}");

        UpdateMenuItems();
        Console.WriteLine("MultiplayerLobbyScene UI entities created");
    }

    private void UpdateMenuItems()
    {
        // Clear existing menu entities (except title)
        ClearMenuEntities();

        // Get screen center for positioning
        var graphicsDevice = PrisonBreak.Core.Core.GraphicsDevice;
        if (graphicsDevice == null) return;
        
        int screenWidth = graphicsDevice.PresentationParameters.BackBufferWidth;
        int screenHeight = graphicsDevice.PresentationParameters.BackBufferHeight;
        Vector2 screenCenter = new Vector2(screenWidth / 2, screenHeight / 2);

        string[] currentMenuItems = _currentState switch
        {
            LobbyState.MainMenu => _mainMenuItems,
            LobbyState.InLobby when _isHost => _lobbyHostItems,
            LobbyState.InLobby when !_isHost => _lobbyClientItems,
            _ => _mainMenuItems
        };

        // Create menu item entities
        for (int i = 0; i < currentMenuItems.Length; i++)
        {
            var menuEntity = EntityManager.CreateEntity();
            menuEntity.AddComponent(new TransformComponent(new Vector2(screenCenter.X - 100, screenCenter.Y - 50 + i * 50)));
            menuEntity.AddComponent(new MenuItemComponent
            {
                IsSelected = i == _selectedIndex,
                Width = 200,
                Height = 40,
                BackgroundColor = i == _selectedIndex ? Color.Yellow : Color.Gray
            });
            menuEntity.AddComponent(new TextComponent
            {
                Text = currentMenuItems[i],
                Color = i == _selectedIndex ? Color.Black : Color.White,
                Alignment = TextAlignment.TopLeft,
                DrawOrder = 10,
                Visible = true
            });

            // Store reference based on menu item type
            if (i == 0)
            {
                if (_currentState == LobbyState.MainMenu)
                    _hostButtonEntity = menuEntity;
                else if (_currentState == LobbyState.InLobby && _isHost)
                    _startGameButtonEntity = menuEntity;
            }
        }

        // Create player list if in lobby
        if (_currentState == LobbyState.InLobby)
        {
            CreatePlayerListUI();
        }
    }

    private void CreatePlayerListUI()
    {
        _playerListEntity = EntityManager.CreateEntity();
        _playerListEntity.AddComponent(new TransformComponent(new Vector2(100, 350)));
        
        string playerListText = "Connected Players:\\n";
        foreach (var player in _connectedPlayers.Values)
        {
            string readyStatus = player.IsReady ? "[READY]" : "[NOT READY]";
            string playerTypeText = player.SelectedPlayerType?.ToString() ?? "Selecting...";
            playerListText += $"{player.Name} - {playerTypeText} {readyStatus}\\n";
        }

        _playerListEntity.AddComponent(new TextComponent
        {
            Text = playerListText,
            Color = Color.White,
            Alignment = TextAlignment.TopLeft,
            DrawOrder = 10,
            Visible = true
        });
    }

    private void ClearMenuEntities()
    {
        // Remove all menu entities except title
        var menuEntities = EntityManager.GetEntitiesWith<MenuItemComponent>();
        foreach (var entity in menuEntities.ToList())
        {
            EntityManager.DestroyEntity(entity.Id);
        }

        // Clear player list
        if (_playerListEntity != null)
        {
            EntityManager.DestroyEntity(_playerListEntity.Id);
            _playerListEntity = null;
        }
    }

    #endregion

    #region Input Handling

    private void HandleLobbyInput(KeyboardState keyboardState, GamePadState gamepadState)
    {
        // Handle menu navigation
        bool upPressed = (keyboardState.IsKeyDown(Keys.Up) && !_previousKeyboardState.IsKeyDown(Keys.Up)) ||
                        (gamepadState.DPad.Up == ButtonState.Pressed && _previousGamepadState.DPad.Up == ButtonState.Released);
        bool downPressed = (keyboardState.IsKeyDown(Keys.Down) && !_previousKeyboardState.IsKeyDown(Keys.Down)) ||
                          (gamepadState.DPad.Down == ButtonState.Pressed && _previousGamepadState.DPad.Down == ButtonState.Released);
        bool enterPressed = (keyboardState.IsKeyDown(Keys.Enter) && !_previousKeyboardState.IsKeyDown(Keys.Enter)) ||
                           (gamepadState.Buttons.A == ButtonState.Pressed && _previousGamepadState.Buttons.A == ButtonState.Released);
        bool escapePressed = (keyboardState.IsKeyDown(Keys.Escape) && !_previousKeyboardState.IsKeyDown(Keys.Escape)) ||
                            (gamepadState.Buttons.B == ButtonState.Pressed && _previousGamepadState.Buttons.B == ButtonState.Released);

        // Handle escape key
        if (escapePressed)
        {
            HandleEscapeKey();
            return;
        }

        // Handle menu navigation
        if (upPressed || downPressed)
        {
            HandleMenuNavigation(upPressed);
        }

        // Handle menu selection
        if (enterPressed)
        {
            HandleMenuSelection();
        }

        // Handle character type selection in lobby
        if (_currentState == LobbyState.InLobby)
        {
            HandleCharacterSelection(keyboardState, gamepadState);
        }
    }

    private void HandleMenuNavigation(bool up)
    {
        string[] currentMenuItems = _currentState switch
        {
            LobbyState.MainMenu => _mainMenuItems,
            LobbyState.InLobby when _isHost => _lobbyHostItems,
            LobbyState.InLobby when !_isHost => _lobbyClientItems,
            _ => _mainMenuItems
        };

        if (up)
        {
            _selectedIndex = (_selectedIndex - 1 + currentMenuItems.Length) % currentMenuItems.Length;
        }
        else
        {
            _selectedIndex = (_selectedIndex + 1) % currentMenuItems.Length;
        }

        UpdateMenuItems();
    }

    private void HandleMenuSelection()
    {
        switch (_currentState)
        {
            case LobbyState.MainMenu:
                HandleMainMenuSelection();
                break;
            case LobbyState.InLobby:
                HandleLobbyMenuSelection();
                break;
        }
    }

    private void HandleMainMenuSelection()
    {
        Console.WriteLine($"[MultiplayerLobbyScene] Main menu selection: index {_selectedIndex}");
        switch (_selectedIndex)
        {
            case 0: // Host Game
                Console.WriteLine("[MultiplayerLobbyScene] Selected: Host Game");
                StartHosting();
                break;
            case 1: // Join Game
                Console.WriteLine("[MultiplayerLobbyScene] Selected: Join Game");
                StartJoining();
                break;
            case 2: // Back to Menu
                Console.WriteLine("[MultiplayerLobbyScene] Selected: Back to Menu");
                ReturnToMainMenu();
                break;
        }
    }

    private void HandleLobbyMenuSelection()
    {
        if (_isHost)
        {
            switch (_selectedIndex)
            {
                case 0: // Start Game
                    StartGame();
                    break;
                case 1: // Leave Lobby
                    LeaveLobby();
                    break;
            }
        }
        else
        {
            switch (_selectedIndex)
            {
                case 0: // Leave Lobby
                    LeaveLobby();
                    break;
            }
        }
    }

    private void HandleCharacterSelection(KeyboardState keyboardState, GamePadState gamepadState)
    {
        // Handle character type selection (C for Cop, P for Prisoner)
        bool cPressed = keyboardState.IsKeyDown(Keys.C) && !_previousKeyboardState.IsKeyDown(Keys.C);
        bool pPressed = keyboardState.IsKeyDown(Keys.P) && !_previousKeyboardState.IsKeyDown(Keys.P);
        bool rPressed = keyboardState.IsKeyDown(Keys.R) && !_previousKeyboardState.IsKeyDown(Keys.R);

        if (cPressed)
        {
            ChangePlayerType(PlayerType.Cop);
        }
        else if (pPressed)
        {
            ChangePlayerType(PlayerType.Prisoner);
        }
        else if (rPressed)
        {
            ToggleReady();
        }
    }

    private void HandleEscapeKey()
    {
        switch (_currentState)
        {
            case LobbyState.MainMenu:
                ReturnToMainMenu();
                break;
            case LobbyState.InLobby:
                LeaveLobby();
                break;
        }
    }

    #endregion

    #region Network Actions

    private void StartHosting()
    {
        Console.WriteLine("[MultiplayerLobbyScene] Starting to host game");
        _networkManager.StartHost();
        _isHost = true;
        _localPlayerId = 1; // Host is always player 1
        
        // Add local player to lobby
        _connectedPlayers[_localPlayerId] = new LobbyPlayer
        {
            PlayerId = _localPlayerId,
            Name = "Host",
            IsReady = false,
            SelectedPlayerType = PlayerType.Prisoner
        };

        _currentState = LobbyState.InLobby;
        _selectedIndex = 0;
        UpdateMenuItems();
    }

    private void StartJoining()
    {
        Console.WriteLine("[MultiplayerLobbyScene] Starting to join game");
        // For now, try to connect to localhost - in a full implementation,
        // this would show a server browser or IP input dialog
        _networkManager.ConnectToHost("127.0.0.1");
        _isHost = false;
    }

    private void StartGame()
    {
        if (!_isHost) return;

        // Check if all players are ready
        bool allPlayersReady = _connectedPlayers.Values.All(p => p.IsReady && p.SelectedPlayerType.HasValue);
        
        if (!allPlayersReady)
        {
            Console.WriteLine("[MultiplayerLobbyScene] Cannot start game - not all players are ready");
            return;
        }

        Console.WriteLine("[MultiplayerLobbyScene] Starting multiplayer game");
        
        // Create game start data for all players
        var playerStartData = _connectedPlayers.Values
            .Where(p => p.SelectedPlayerType.HasValue)
            .Select((p, index) => new GameStartPlayerData
            {
                PlayerId = p.PlayerId,
                PlayerType = p.SelectedPlayerType.Value,
                PlayerIndex = (Microsoft.Xna.Framework.PlayerIndex)index
            }).ToArray();

        // Send game start message to all clients
        _networkManager.SendGameStart(playerStartData);
        
        // Create game start data for local player (host) using multiplayer constructor
        var localPlayer = _connectedPlayers[_localPlayerId];
        var localPlayerData = playerStartData.First(p => p.PlayerId == _localPlayerId);
        
        var gameStartData = new GameStartData(
            localPlayerType: localPlayer.SelectedPlayerType.Value,
            localPlayerIndex: localPlayerData.PlayerIndex,
            allPlayersData: playerStartData,
            localPlayerId: _localPlayerId
        );

        Console.WriteLine($"[MultiplayerLobbyScene] Host transitioning to gameplay with {playerStartData.Length} players");
        // Transition to gameplay scene
        EventBus.Send(new SceneTransitionEvent(SceneType.MultiplayerLobby, SceneType.Gameplay, gameStartData));
    }

    private void LeaveLobby()
    {
        Console.WriteLine("[MultiplayerLobbyScene] Leaving lobby");
        
        if (_isHost)
        {
            _networkManager.StartSinglePlayer(); // This will stop the server
        }
        else
        {
            _networkManager.StartSinglePlayer(); // This will disconnect the client
        }

        // Reset lobby state
        _connectedPlayers.Clear();
        _currentState = LobbyState.MainMenu;
        _selectedIndex = 0;
        _isHost = false;
        _localPlayerId = -1;
        UpdateMenuItems();
    }

    private void ReturnToMainMenu()
    {
        Console.WriteLine("[MultiplayerLobbyScene] Returning to main menu");
        EventBus.Send(new SceneTransitionEvent(SceneType.MultiplayerLobby, SceneType.StartMenu));
    }

    private void ChangePlayerType(PlayerType newType)
    {
        if (_localPlayerId >= 0 && _connectedPlayers.ContainsKey(_localPlayerId))
        {
            _connectedPlayers[_localPlayerId].SelectedPlayerType = newType;
            _connectedPlayers[_localPlayerId].IsReady = false; // Reset ready state when changing type
            
            Console.WriteLine($"[MultiplayerLobbyScene] Changed player type to {newType}");
            UpdateMenuItems();
            
            // Send network message to other players about type change
            _networkManager.SendCharacterSelection(_localPlayerId, newType);
        }
    }

    private void ToggleReady()
    {
        if (_localPlayerId >= 0 && _connectedPlayers.ContainsKey(_localPlayerId))
        {
            var player = _connectedPlayers[_localPlayerId];
            if (player.SelectedPlayerType.HasValue)
            {
                player.IsReady = !player.IsReady;
                Console.WriteLine($"[MultiplayerLobbyScene] Ready state changed to {player.IsReady}");
                UpdateMenuItems();
                
                // Send network message to other players about ready state change
                _networkManager.SendReadyState(_localPlayerId, player.IsReady, player.SelectedPlayerType.Value);
            }
        }
    }

    #endregion

    #region Event Handlers

    private void OnMenuNavigation(MenuNavigationEvent navigationEvent)
    {
        // Menu navigation is handled directly in HandleLobbyInput
        // This event handler can be used for additional navigation logic if needed
    }

    private void OnNetworkConnection(NetworkConnectionEvent connectionEvent)
    {
        Console.WriteLine($"[MultiplayerLobbyScene] Network connection event: {connectionEvent.Type}");
        
        switch (connectionEvent.Type)
        {
            case NetworkConnectionType.Connected:
                if (!_isHost)
                {
                    // Successfully joined a lobby as client
                    _currentState = LobbyState.InLobby;
                    _localPlayerId = connectionEvent.PlayerId; // Assigned by server
                    
                    _connectedPlayers[_localPlayerId] = new LobbyPlayer
                    {
                        PlayerId = _localPlayerId,
                        Name = $"Player {_localPlayerId}",
                        IsReady = false,
                        SelectedPlayerType = PlayerType.Prisoner
                    };
                    
                    // Send join lobby message to inform the host about this client
                    // This will add the client to the host's _connectedPlayers dictionary
                    _networkManager.SendPlayerJoinLobby(_localPlayerId, $"Player {_localPlayerId}");
                    
                    _selectedIndex = 0;
                    UpdateMenuItems();
                }
                break;
                
            case NetworkConnectionType.Disconnected:
                // Connection lost, return to main menu
                LeaveLobby();
                break;
        }
    }

    private void OnPlayerJoinedLobby(PlayerJoinedLobbyEvent joinEvent)
    {
        Console.WriteLine($"[MultiplayerLobbyScene] Player {joinEvent.PlayerId} joined lobby");
        
        _connectedPlayers[joinEvent.PlayerId] = new LobbyPlayer
        {
            PlayerId = joinEvent.PlayerId,
            Name = joinEvent.PlayerName,
            IsReady = false,
            SelectedPlayerType = null
        };
        
        UpdateMenuItems();
    }

    private void OnPlayerLeftLobby(PlayerLeftLobbyEvent leaveEvent)
    {
        Console.WriteLine($"[MultiplayerLobbyScene] Player {leaveEvent.PlayerId} left lobby");
        
        _connectedPlayers.Remove(leaveEvent.PlayerId);
        UpdateMenuItems();
    }

    private void OnPlayerReadyChanged(PlayerReadyChangedEvent readyEvent)
    {
        Console.WriteLine($"[MultiplayerLobbyScene] Player {readyEvent.PlayerId} ready state: {readyEvent.IsReady}");
        
        // Ignore events for our own player - we already updated locally when we made the change
        if (readyEvent.PlayerId == _localPlayerId)
        {
            Console.WriteLine($"[MultiplayerLobbyScene] Ignoring ready changed event for local player {_localPlayerId}");
            return;
        }
        
        if (_connectedPlayers.ContainsKey(readyEvent.PlayerId))
        {
            _connectedPlayers[readyEvent.PlayerId].IsReady = readyEvent.IsReady;
            _connectedPlayers[readyEvent.PlayerId].SelectedPlayerType = readyEvent.SelectedPlayerType;
            UpdateMenuItems();
        }
    }

    #endregion

    public override void OnExit()
    {
        // Clean up event subscriptions
        EventBus.Unsubscribe<MenuNavigationEvent>(OnMenuNavigation);
        EventBus.Unsubscribe<NetworkConnectionEvent>(OnNetworkConnection);
        EventBus.Unsubscribe<PlayerJoinedLobbyEvent>(OnPlayerJoinedLobby);
        EventBus.Unsubscribe<PlayerLeftLobbyEvent>(OnPlayerLeftLobby);
        EventBus.Unsubscribe<PlayerReadyChangedEvent>(OnPlayerReadyChanged);

        // Don't automatically disconnect when exiting lobby scene
        // Network connections should persist into gameplay scene
        // Only disconnect when explicitly returning to main menu

        base.OnExit();
    }
}

#region Supporting Types

public enum LobbyState
{
    MainMenu,
    InLobby
}

public class LobbyPlayer
{
    public int PlayerId { get; set; }
    public string Name { get; set; }
    public bool IsReady { get; set; }
    public PlayerType? SelectedPlayerType { get; set; }
}

// Event types for lobby functionality
public struct NetworkConnectionEvent
{
    public NetworkConnectionType Type;
    public int PlayerId;
    public string Reason;
}

public enum NetworkConnectionType
{
    Connected,
    Disconnected,
    ConnectionFailed
}


#endregion