using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PrisonBreak.Core.Graphics;
using PrisonBreak.Config;
using PrisonBreak.ECS;
using PrisonBreak.ECS.Systems;
using PrisonBreak.Managers;

namespace PrisonBreak.Scenes;

/// <summary>
/// Main gameplay scene - contains all the existing Game1 logic
/// </summary>
public class GameplayScene : Scene, ITransitionDataReceiver
{
    // All the existing Game1 private fields
    private ComponentInputSystem _inputSystem;
    private AnimationSystem _animationSystem;
    private ComponentMovementSystem _movementSystem;
    private ComponentCollisionSystem _collisionSystem;
    private ComponentRenderSystem _renderSystem;
    private InventorySystem _inventorySystem;
    private InventoryUIRenderSystem _inventoryUIRenderSystem;
    private InteractionSystem _interactionSystem;
    private ChestUIRenderSystem _chestUIRenderSystem;
    private NetworkManager _networkManager;
    private NetworkSyncSystem _networkSyncSystem;

    private Tilemap _tilemap;
    private Rectangle _roomBounds;
    private bool _gameInitialized;
    private bool _tilemapSet;

    // Data from start menu
    private GameStartData? _gameStartData;

    // Chest UI state
    private bool _isChestUIOpen = false;
    private Entity _currentChestEntity = null;

    // Inventory slot selection state for chest UI
    private int _selectedSlotIndex = 0;
    private bool _isPlayerInventorySelected = true; // true = player inventory, false = chest inventory

    // Input state tracking
    private KeyboardState _previousKeyboardState;
    private GamePadState _previousGamepadState;

    public GameplayScene(EventBus eventBus) : base("Gameplay", eventBus)
    {
    }

    /// <summary>
    /// Receive data from the start menu scene
    /// </summary>
    public void ReceiveTransitionData(object data)
    {
        if (data is GameStartData startData)
        {
            _gameStartData = startData;
            Console.WriteLine($"GameplayScene received start data: PlayerType={startData.PlayerType}, PlayerIndex={startData.PlayerIndex}");
        }
    }

    protected override void SetupSystems()
    {
        // Create component-based systems (same as Game1)
        _inputSystem = new ComponentInputSystem();
        _animationSystem = new AnimationSystem(EntityManager);
        _movementSystem = new ComponentMovementSystem();
        _collisionSystem = new ComponentCollisionSystem();
        _renderSystem = new ComponentRenderSystem();
        _inventorySystem = new InventorySystem();
        _inventoryUIRenderSystem = new InventoryUIRenderSystem();
        _interactionSystem = new InteractionSystem();
        _chestUIRenderSystem = new ChestUIRenderSystem();
        _networkSyncSystem = new NetworkSyncSystem();
        // Try to get existing NetworkManager (from lobby), otherwise we're in single-player
        try
        {
            _networkManager = NetworkManager.Instance;
            _networkManager.UpdateEntityManager(EntityManager); // Update to use this scene's EntityManager
            Console.WriteLine($"Debug: Using existing NetworkManager, mode = {_networkManager.CurrentGameMode}");
        }
        catch (InvalidOperationException)
        {
            _networkManager = null;  // Single-player mode
            Console.WriteLine("Debug: No NetworkManager found - single-player mode");
        }

        // Set up system dependencies (same as Game1)
        _inputSystem.SetEntityManager(EntityManager);
        _inputSystem.SetEventBus(EventBus);

        _movementSystem.SetEntityManager(EntityManager);
        _movementSystem.SetEventBus(EventBus);

        _collisionSystem.SetEntityManager(EntityManager);
        _collisionSystem.SetEventBus(EventBus);

        _renderSystem.SetEntityManager(EntityManager);
        _renderSystem.SetEventBus(EventBus);

        _inventorySystem.SetEntityManager(EntityManager);
        _inventorySystem.SetEventBus(EventBus);

        _inventoryUIRenderSystem.SetEntityManager(EntityManager);
        _inventoryUIRenderSystem.SetEventBus(EventBus);

        _interactionSystem.SetEntityManager(EntityManager);
        _interactionSystem.SetEventBus(EventBus);
        _interactionSystem.SetInventorySystem(_inventorySystem);

        _chestUIRenderSystem.SetEntityManager(EntityManager);
        _chestUIRenderSystem.SetEventBus(EventBus);

        _networkSyncSystem.SetEntityManager(EntityManager);
        _networkSyncSystem.SetEventBus(EventBus);

        // Add systems to manager in execution order (same as Game1)
        SystemManager.AddSystem(_inputSystem);
        SystemManager.AddSystem(_interactionSystem);
        SystemManager.AddSystem(_animationSystem);
        SystemManager.AddSystem(_movementSystem);
        SystemManager.AddSystem(_collisionSystem);
        SystemManager.AddSystem(_inventorySystem);
        if (_networkManager != null) // Only add NetworkManager in multiplayer mode
        {
            SystemManager.AddSystem(_networkManager); // Add network manager after game logic systems
            SystemManager.AddSystem(_networkSyncSystem); // Add network sync system after network manager
        }
        SystemManager.AddSystem(_renderSystem);
        SystemManager.AddSystem(_inventoryUIRenderSystem);
        SystemManager.AddSystem(_chestUIRenderSystem); // Render chest UI on top
    }

    protected override void LoadSceneContent()
    {
        Console.WriteLine("GameplayScene.LoadSceneContent called");

        // Load tilemap (same as Game1.LoadContent)
        _tilemap = Tilemap.FromFile(Content, EntityConfig.Tilemap.ConfigFile);
        if (_tilemap != null)
        {
            _tilemap.Scale = EntityConfig.Tilemap.Scale;
            Console.WriteLine($"Loaded tilemap: {_tilemap.Rows}x{_tilemap.Columns}, Scale: {_tilemap.Scale}");
        }
        else
        {
            Console.WriteLine("Failed to load tilemap!");
        }

        // Set content for chest UI render system
        _chestUIRenderSystem.SetContent(Content);
    }

    public override void Update(GameTime gameTime)
    {
        var currentKeyboardState = Keyboard.GetState();
        var currentGamepadState = GamePad.GetState(PlayerIndex.One);

        // Handle chest UI input if open
        if (_isChestUIOpen)
        {
            HandleChestUIInput(currentKeyboardState, currentGamepadState);
        }
        else
        {
            // Handle escape key to return to menu (only when chest UI is not open)
            // Check for key press (was up, now down) to prevent repeated triggers
            if (currentKeyboardState.IsKeyDown(Keys.Escape) && !_previousKeyboardState.IsKeyDown(Keys.Escape))
            {
                EventBus.Send(new SceneTransitionEvent(SceneType.Gameplay, SceneType.StartMenu));
                return;
            }
        }

        // Set tilemap for render system on first update (same as Game1)
        if (!_tilemapSet && _renderSystem != null && _tilemap != null)
        {
            Console.WriteLine($"Setting tilemap in render system: {_tilemap.Rows}x{_tilemap.Columns}");
            _renderSystem.SetTilemap(_tilemap);
            _tilemapSet = true;
        }

        // Initialize game entities on first update (same as Game1)
        // But only after tilemap is loaded
        if (!_gameInitialized && _tilemap != null)
        {
            InitializeGame();
        }

        // TODO: Disable player input when chest UI is open by setting PlayerInputComponent.IsActive = false
        // For now, chest UI input is handled separately in HandleChestUIInput()

        // Call base update which runs SystemManager.Update (world continues running)
        base.Update(gameTime);

        // Store current input state for next frame
        _previousKeyboardState = currentKeyboardState;
        _previousGamepadState = currentGamepadState;
    }

    /// <summary>
    /// Initialize game entities - same logic as Game1.InitializeGame()
    /// </summary>
    private void InitializeGame()
    {
        Console.WriteLine("InitializeGame called");

        // Get GraphicsDevice from the Core class (same as Game1)
        var graphicsDevice = PrisonBreak.Core.Core.GraphicsDevice;
        if (graphicsDevice == null)
        {
            Console.WriteLine("GraphicsDevice not available yet, postponing game initialization");
            return;
        }

        // Calculate room bounds (same as Game1)
        Rectangle screenBounds = graphicsDevice.PresentationParameters.Bounds;
        _roomBounds = new Rectangle(
            (int)_tilemap.TileWidth,
            (int)_tilemap.TileHeight,
            screenBounds.Width - (int)_tilemap.TileWidth * 2,
            screenBounds.Height - (int)_tilemap.TileHeight * 2
        );

        // Set bounds for collision system (same as Game1)
        _collisionSystem.SetBounds(_roomBounds, _tilemap);

        // Set up tile-based collision map for movement system (same as Game1)
        _movementSystem.SetCollisionMap(_tilemap, Vector2.Zero);

        // Calculate player spawn positions
        int centerRow = _tilemap.Rows / 2;
        int centerColumn = _tilemap.Columns / 2;
        Vector2 baseSpawnPos = new(centerColumn * _tilemap.TileWidth, centerRow * _tilemap.TileHeight);

        // Check if this is multiplayer mode
        bool isMultiplayer = _networkManager != null && _networkManager.CurrentGameMode != PrisonBreak.Multiplayer.Core.NetworkConfig.GameMode.SinglePlayer;
        Entity localPlayerEntity = null;

        if (isMultiplayer && _gameStartData?.AllPlayersData != null)
        {
            // Multiplayer: Create all player entities from AllPlayersData
            Console.WriteLine($"[GameplayScene] Multiplayer mode - LocalPlayerId = {_gameStartData.Value.LocalPlayerId}");

            for (int i = 0; i < _gameStartData.Value.AllPlayersData.Length; i++)
            {
                var playerData = _gameStartData.Value.AllPlayersData[i];

                // Calculate spawn position (ensure players spawn in safe, open areas)
                Vector2 playerSpawnPos;
                if (i == 0)
                {
                    // First player spawns at a safe location (left side of center)
                    playerSpawnPos = new Vector2(baseSpawnPos.X - 128, baseSpawnPos.Y);
                }
                else
                {
                    // Second player spawns at a safe location (right side of center)  
                    playerSpawnPos = new Vector2(baseSpawnPos.X + 250, baseSpawnPos.Y);
                }

                // Determine if this is the local player
                bool isLocalPlayer = playerData.PlayerId == _gameStartData.Value.LocalPlayerId;

                // Local player always gets PlayerIndex.One for keyboard input, remote players get their assigned index
                PlayerIndex playerIndex = isLocalPlayer ? PlayerIndex.One : playerData.PlayerIndex;
                Console.WriteLine($"[GameplayScene] Player {playerData.PlayerId} assigned PlayerIndex: {playerIndex} (original: {playerData.PlayerIndex}, isLocal: {isLocalPlayer})");
                var playerEntity = EntityManager.CreatePlayer(playerSpawnPos, playerIndex, playerData.PlayerType);

                // Add network component for all players
                playerEntity.AddComponent(new NetworkComponent(
                    networkId: playerData.PlayerId,
                    authority: PrisonBreak.Multiplayer.Core.NetworkConfig.NetworkAuthority.Client,
                    syncTransform: true,
                    syncMovement: true,
                    syncInventory: false,
                    ownerId: playerData.PlayerId
                ));

                if (isLocalPlayer)
                {
                    localPlayerEntity = playerEntity;
                    Console.WriteLine($"[GameplayScene] Local player created with PlayerIndex: {playerIndex}");
                    Console.WriteLine($"[GameplayScene] Local player has PlayerInputComponent: {playerEntity.HasComponent<PlayerInputComponent>()}");

                    // Create inventory UI only for local player
                    int screenHeight = graphicsDevice.PresentationParameters.Bounds.Height;
                    EntityManager.CreateInventoryUIForPlayer(playerEntity, true, screenHeight);
                }
                else
                {
                    // Remove PlayerInputComponent from remote players (they shouldn't be controllable locally)
                    if (playerEntity.HasComponent<PlayerInputComponent>())
                    {
                        playerEntity.RemoveComponent<PlayerInputComponent>();
                    }
                }
            }
        }
        else
        {
            // Single-player: Create only local player (existing logic)
            PlayerType playerType = _gameStartData?.PlayerType ?? PlayerType.Prisoner;
            PlayerIndex playerIndex = _gameStartData?.PlayerIndex ?? PlayerIndex.One;

            localPlayerEntity = EntityManager.CreatePlayer(baseSpawnPos, playerIndex, playerType);
            Console.WriteLine($"[GameplayScene] Single-player mode");

            // Create inventory UI for single player
            int screenHeight = graphicsDevice.PresentationParameters.Bounds.Height;
            EntityManager.CreateInventoryUIForPlayer(localPlayerEntity, true, screenHeight);
        }

        // Give cop players their starting key item (only for local player to avoid duplicates)
        if (localPlayerEntity != null && localPlayerEntity.HasComponent<PlayerTypeComponent>() &&
            localPlayerEntity.GetComponent<PlayerTypeComponent>().Type == PlayerType.Cop)
        {
            try
            {
                var keyItem = EntityManager.CreateKey();
                bool keyAdded = _inventorySystem.TryAddItem(localPlayerEntity, keyItem);
                if (keyAdded)
                {
                    Console.WriteLine($"Human cop player {localPlayerEntity.Id} received starting key");
                }
                else
                {
                    Console.WriteLine($"Warning: Could not add starting key to cop player {localPlayerEntity.Id} inventory");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not create starting key for cop player: {ex.Message}");
            }
        }

        // Create cop entities (same as Game1)
        Vector2 copStartPos1 = new(_roomBounds.Left + 50, _roomBounds.Top + 50);
        Vector2 copStartPos2 = new(_roomBounds.Right - 100, _roomBounds.Bottom - 100);

        var cop1 = EntityManager.CreateCop(copStartPos1, AIBehavior.Patrol);
        var cop2 = EntityManager.CreateCop(copStartPos2, AIBehavior.Wander);

        // Give AI cops their starting key items
        try
        {
            var keyItem1 = EntityManager.CreateKey();
            var keyItem2 = EntityManager.CreateKey();

            _inventorySystem.TryAddItem(cop1, keyItem1);
            _inventorySystem.TryAddItem(cop2, keyItem2);

            Console.WriteLine($"AI cops {cop1.Id} and {cop2.Id} received starting keys");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not create starting keys for AI cops: {ex.Message}");
        }

        // Add bounds constraints to all entities (same as Game1)
        if (localPlayerEntity != null)
        {
            EntityManager.AddBoundsConstraint(localPlayerEntity, _roomBounds, false); // Player clamps
        }
        EntityManager.AddBoundsConstraint(cop1, _roomBounds, true); // Cops reflect
        EntityManager.AddBoundsConstraint(cop2, _roomBounds, true);

        // Create test items and chests for interaction system testing
        CreateTestItems();

        _gameInitialized = true;

        // Subscribe to events for debugging (same as Game1)
        EventBus.Subscribe<EntitySpawnEvent>(OnEntitySpawn);
        EventBus.Subscribe<PlayerCopCollisionEvent>(OnPlayerCopCollision);
        EventBus.Subscribe<TeleportEvent>(OnTeleport);

        // Subscribe to chest UI events
        EventBus.Subscribe<ChestUIOpenEvent>(OnChestUIOpen);
        EventBus.Subscribe<ChestUICloseEvent>(OnChestUIClose);

        Console.WriteLine($"[GameplayScene] Initialization complete");
    }

    /// <summary>
    /// Create test items and chests for testing the interaction system
    /// </summary>
    private void CreateTestItems()
    {
        try
        {
            // Create a test item near the player for pickup testing
            Vector2 testItemPos = new Vector2(_roomBounds.Left + 200, _roomBounds.Top + 100);
            var testItem = EntityManager.CreateItemAtPosition("key", testItemPos);
            Console.WriteLine($"Created test key item at {testItemPos}");

            // Create a test chest with some items
            Vector2 testChestPos = new Vector2(_roomBounds.Right - 200, _roomBounds.Top + 150);
            string[] chestItems = { "key" }; // Put a key in the chest
            var testChest = EntityManager.CreateChest(testChestPos, chestItems);
            Console.WriteLine($"Created test chest at {testChestPos} with {chestItems.Length} items");

            // Create another test item on the opposite side
            Vector2 testItemPos2 = new Vector2(_roomBounds.Right - 150, _roomBounds.Bottom - 100);
            var testItem2 = EntityManager.CreateItemAtPosition("key", testItemPos2);
            Console.WriteLine($"Created second test key item at {testItemPos2}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not create test items/chests: {ex.Message}");
        }
    }

    // Event handlers for debugging (same as Game1)
    private void OnEntitySpawn(EntitySpawnEvent spawnEvent)
    {
        Console.WriteLine($"Entity {spawnEvent.EntityId} ({spawnEvent.EntityType}) spawned at {spawnEvent.Position}");
    }

    private void OnPlayerCopCollision(PlayerCopCollisionEvent collisionEvent)
    {
        Console.WriteLine($"Player {collisionEvent.PlayerId} collided with Cop {collisionEvent.CopId}");
    }

    private void OnTeleport(TeleportEvent teleportEvent)
    {
        Console.WriteLine($"Entity {teleportEvent.EntityId} teleported from {teleportEvent.FromPosition} to {teleportEvent.ToPosition}");
    }

    private void OnChestUIOpen(ChestUIOpenEvent evt)
    {
        Console.WriteLine($"Opening chest UI for chest {evt.ChestEntity.Id}");
        _isChestUIOpen = true;
        _currentChestEntity = evt.ChestEntity;

        // Reset slot selection to player inventory, slot 0
        _selectedSlotIndex = 0;
        _isPlayerInventorySelected = true;
        SendSlotSelectedEvent();
    }

    private void OnChestUIClose(ChestUICloseEvent evt)
    {
        Console.WriteLine($"Closing chest UI for chest {evt.ChestEntity.Id}");
        _isChestUIOpen = false;
        _currentChestEntity = null;
    }

    private void HandleChestUIInput(KeyboardState keyboardState, GamePadState gamepadState)
    {
        // Close chest UI on Escape or gamepad B button (check for key press, not hold)
        bool escapePressed = keyboardState.IsKeyDown(Keys.Escape) && !_previousKeyboardState.IsKeyDown(Keys.Escape);
        bool bButtonPressed = gamepadState.Buttons.B == ButtonState.Pressed && _previousGamepadState.Buttons.B == ButtonState.Released;

        if (escapePressed || bButtonPressed)
        {
            if (_currentChestEntity != null)
            {
                EventBus.Send(new ChestUICloseEvent(_currentChestEntity, null));
            }
            return;
        }

        // Handle slot navigation with arrow keys / gamepad D-Pad
        HandleSlotNavigation(keyboardState, gamepadState);

        // Handle item transfer with Enter key / gamepad A button
        HandleItemTransfer(keyboardState, gamepadState);
    }

    private void HandleSlotNavigation(KeyboardState keyboardState, GamePadState gamepadState)
    {
        // Left/Right arrow keys or D-Pad to change selected slot
        bool leftPressed = (keyboardState.IsKeyDown(Keys.Left) && !_previousKeyboardState.IsKeyDown(Keys.Left)) ||
                          (gamepadState.DPad.Left == ButtonState.Pressed && _previousGamepadState.DPad.Left == ButtonState.Released);
        bool rightPressed = (keyboardState.IsKeyDown(Keys.Right) && !_previousKeyboardState.IsKeyDown(Keys.Right)) ||
                           (gamepadState.DPad.Right == ButtonState.Pressed && _previousGamepadState.DPad.Right == ButtonState.Released);

        // Up/Down arrow keys or D-Pad to switch between player and chest inventory
        bool upPressed = (keyboardState.IsKeyDown(Keys.Up) && !_previousKeyboardState.IsKeyDown(Keys.Up)) ||
                        (gamepadState.DPad.Up == ButtonState.Pressed && _previousGamepadState.DPad.Up == ButtonState.Released);
        bool downPressed = (keyboardState.IsKeyDown(Keys.Down) && !_previousKeyboardState.IsKeyDown(Keys.Down)) ||
                          (gamepadState.DPad.Down == ButtonState.Pressed && _previousGamepadState.DPad.Down == ButtonState.Released);

        if (leftPressed)
        {
            _selectedSlotIndex = Math.Max(0, _selectedSlotIndex - 1);
            SendSlotSelectedEvent();
        }
        else if (rightPressed)
        {
            int maxSlots = GetMaxSlotsForCurrentInventory();
            _selectedSlotIndex = Math.Min(maxSlots - 1, _selectedSlotIndex + 1);
            SendSlotSelectedEvent();
        }
        else if (downPressed && !_isPlayerInventorySelected)
        {
            // Switch from chest to player inventory
            _isPlayerInventorySelected = true;
            _selectedSlotIndex = Math.Min(_selectedSlotIndex, GetMaxSlotsForCurrentInventory() - 1);
            SendSlotSelectedEvent();
        }
        else if (upPressed && _isPlayerInventorySelected)
        {
            // Switch from player to chest inventory
            _isPlayerInventorySelected = false;
            _selectedSlotIndex = Math.Min(_selectedSlotIndex, GetMaxSlotsForCurrentInventory() - 1);
            SendSlotSelectedEvent();
        }
    }

    private void HandleItemTransfer(KeyboardState keyboardState, GamePadState gamepadState)
    {
        // Enter key or gamepad A button to transfer item
        bool enterPressed = keyboardState.IsKeyDown(Keys.Enter) && !_previousKeyboardState.IsKeyDown(Keys.Enter);
        bool aButtonPressed = gamepadState.Buttons.A == ButtonState.Pressed && _previousGamepadState.Buttons.A == ButtonState.Released;

        if (enterPressed || aButtonPressed)
        {
            PerformItemTransfer();
        }
    }

    private void PerformItemTransfer()
    {
        if (_inventorySystem == null || _currentChestEntity == null)
            return;

        // Get the player entity (assume first player for now)
        var playerEntities = EntityManager.GetEntitiesWith<PlayerTag>();
        var playerEntity = playerEntities.FirstOrDefault();
        if (playerEntity == null)
            return;

        bool transferSuccess = false;

        if (_isPlayerInventorySelected)
        {
            // Transfer from player to chest
            transferSuccess = _inventorySystem.TryTransferItemToContainer(playerEntity, _currentChestEntity, _selectedSlotIndex);
            if (transferSuccess)
            {
                Console.WriteLine($"[DEBUG] GameplayScene: Transferred item from player slot {_selectedSlotIndex} to chest");
            }
        }
        else
        {
            // Transfer from chest to player
            transferSuccess = _inventorySystem.TryTransferItemToPlayer(_currentChestEntity, playerEntity, _selectedSlotIndex);
            if (transferSuccess)
            {
                Console.WriteLine($"[DEBUG] GameplayScene: Transferred item from chest slot {_selectedSlotIndex} to player");
            }
        }

        if (!transferSuccess)
        {
            Console.WriteLine($"[DEBUG] GameplayScene: Item transfer failed - slot {_selectedSlotIndex} in {(_isPlayerInventorySelected ? "player" : "chest")} inventory");
        }
    }

    private int GetMaxSlotsForCurrentInventory()
    {
        if (_isPlayerInventorySelected)
        {
            // Get player inventory size
            var playerEntity = EntityManager.GetEntitiesWith<PlayerTag>().FirstOrDefault();
            if (playerEntity != null && playerEntity.HasComponent<InventoryComponent>())
            {
                return playerEntity.GetComponent<InventoryComponent>().MaxSlots;
            }
            return 3; // Default player inventory size
        }
        else
        {
            // Get chest inventory size
            if (_currentChestEntity != null && _currentChestEntity.HasComponent<ContainerComponent>())
            {
                return _currentChestEntity.GetComponent<ContainerComponent>().MaxItems;
            }
            return 10; // Default chest size
        }
    }

    private void SendSlotSelectedEvent()
    {
        Entity targetContainer = _isPlayerInventorySelected ?
            EntityManager.GetEntitiesWith<PlayerTag>().FirstOrDefault() :
            _currentChestEntity;

        if (targetContainer != null)
        {
            EventBus.Send(new InventorySlotSelectedEvent(targetContainer, _selectedSlotIndex, _isPlayerInventorySelected));
        }
    }

    public override void OnExit()
    {
        // Clean up event subscriptions
        EventBus.Unsubscribe<EntitySpawnEvent>(OnEntitySpawn);
        EventBus.Unsubscribe<PlayerCopCollisionEvent>(OnPlayerCopCollision);
        EventBus.Unsubscribe<TeleportEvent>(OnTeleport);
        EventBus.Unsubscribe<ChestUIOpenEvent>(OnChestUIOpen);
        EventBus.Unsubscribe<ChestUICloseEvent>(OnChestUIClose);

        // Clean up multiplayer entities if in multiplayer mode
        if (_networkManager != null && _networkManager.CurrentGameMode != PrisonBreak.Multiplayer.Core.NetworkConfig.GameMode.SinglePlayer)
        {
            // Log multiplayer entity cleanup
            var networkedEntities = EntityManager.GetEntitiesWith<NetworkComponent>();
            Console.WriteLine($"[GameplayScene] Cleaning up {networkedEntities.Count()} networked entities on scene exit");

            // EntityManager.Clear() in base.OnExit() will handle actual cleanup
            // This is just for logging and future enhanced cleanup if needed
        }

        base.OnExit();
    }
}