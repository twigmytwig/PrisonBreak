using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PrisonBreak.Core.Graphics;
using PrisonBreak.Config;
using PrisonBreak.ECS;
using PrisonBreak.ECS.Systems;

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

    private Tilemap _tilemap;
    private Rectangle _roomBounds;
    private bool _gameInitialized;
    private bool _tilemapSet;

    // Data from start menu
    private GameStartData? _gameStartData;

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

        // Add systems to manager in execution order (same as Game1)
        SystemManager.AddSystem(_inputSystem);
        SystemManager.AddSystem(_interactionSystem);
        SystemManager.AddSystem(_animationSystem);
        SystemManager.AddSystem(_movementSystem);
        SystemManager.AddSystem(_collisionSystem);
        SystemManager.AddSystem(_inventorySystem);
        SystemManager.AddSystem(_renderSystem);
        SystemManager.AddSystem(_inventoryUIRenderSystem);
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
    }

    public override void Update(GameTime gameTime)
    {
        // Handle escape key to return to menu (new functionality)
        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
        {
            EventBus.Send(new SceneTransitionEvent(SceneType.Gameplay, SceneType.StartMenu));
            return;
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

        // Call base update which runs SystemManager.Update
        base.Update(gameTime);
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

        // Create player entity (enhanced with start menu data)
        int centerRow = _tilemap.Rows / 2;
        int centerColumn = _tilemap.Columns / 2;
        Vector2 playerStartPos = new(centerColumn * _tilemap.TileWidth, centerRow * _tilemap.TileHeight);

        // Use data from start menu if available, otherwise default to prisoner
        PlayerType playerType = _gameStartData?.PlayerType ?? PlayerType.Prisoner;
        PlayerIndex playerIndex = _gameStartData?.PlayerIndex ?? PlayerIndex.One;

        var playerEntity = EntityManager.CreatePlayer(playerStartPos, playerIndex, playerType);

        // Create inventory UI for the player
        int screenHeight = graphicsDevice.PresentationParameters.Bounds.Height;
        EntityManager.CreateInventoryUIForPlayer(playerEntity, true, screenHeight);

        // Give cop players their starting key item (after UI is created for proper event handling)
        if (playerType == PlayerType.Cop)
        {
            try
            {
                var keyItem = EntityManager.CreateKey();
                bool keyAdded = _inventorySystem.TryAddItem(playerEntity, keyItem);
                if (keyAdded)
                {
                    Console.WriteLine($"Human cop player {playerEntity.Id} received starting key");
                }
                else
                {
                    Console.WriteLine($"Warning: Could not add starting key to cop player {playerEntity.Id} inventory");
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
        EntityManager.AddBoundsConstraint(playerEntity, _roomBounds, false); // Player clamps
        EntityManager.AddBoundsConstraint(cop1, _roomBounds, true); // Cops reflect
        EntityManager.AddBoundsConstraint(cop2, _roomBounds, true);

        // Create test items and chests for interaction system testing
        CreateTestItems();

        _gameInitialized = true;

        // Subscribe to events for debugging (same as Game1)
        EventBus.Subscribe<EntitySpawnEvent>(OnEntitySpawn);
        EventBus.Subscribe<PlayerCopCollisionEvent>(OnPlayerCopCollision);
        EventBus.Subscribe<TeleportEvent>(OnTeleport);

        Console.WriteLine($"GameplayScene initialized with PlayerType: {playerType}");
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

    public override void OnExit()
    {
        // Clean up event subscriptions
        EventBus.Unsubscribe<EntitySpawnEvent>(OnEntitySpawn);
        EventBus.Unsubscribe<PlayerCopCollisionEvent>(OnPlayerCopCollision);
        EventBus.Unsubscribe<TeleportEvent>(OnTeleport);

        base.OnExit();
    }
}