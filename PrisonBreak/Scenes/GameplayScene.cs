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

        // Add systems to manager in execution order (same as Game1)
        SystemManager.AddSystem(_inputSystem);
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

        // Create cop entities (same as Game1)
        Vector2 copStartPos1 = new(_roomBounds.Left + 50, _roomBounds.Top + 50);
        Vector2 copStartPos2 = new(_roomBounds.Right - 100, _roomBounds.Bottom - 100);

        var cop1 = EntityManager.CreateCop(copStartPos1, AIBehavior.Patrol);
        var cop2 = EntityManager.CreateCop(copStartPos2, AIBehavior.Wander);

        // Add bounds constraints to all entities (same as Game1)
        EntityManager.AddBoundsConstraint(playerEntity, _roomBounds, false); // Player clamps
        EntityManager.AddBoundsConstraint(cop1, _roomBounds, true); // Cops reflect
        EntityManager.AddBoundsConstraint(cop2, _roomBounds, true);

        _gameInitialized = true;

        // Subscribe to events for debugging (same as Game1)
        EventBus.Subscribe<EntitySpawnEvent>(OnEntitySpawn);
        EventBus.Subscribe<PlayerCopCollisionEvent>(OnPlayerCopCollision);
        EventBus.Subscribe<TeleportEvent>(OnTeleport);

        Console.WriteLine($"GameplayScene initialized with PlayerType: {playerType}");
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