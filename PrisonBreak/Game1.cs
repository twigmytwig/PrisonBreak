using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using PrisonBreak.Config;
using PrisonBreak.Managers;
using PrisonBreak.Systems;
using PrisonBreak.ECS;

namespace PrisonBreak;

public class Game1 : Core
{
    // Component-based systems
    private SystemManager _systemManager;
    private ComponentEntityManager _entityManager;
    private EventBus _eventBus;
    
    private ComponentInputSystem _inputSystem;
    private ComponentMovementSystem _movementSystem;
    private ComponentCollisionSystem _collisionSystem;
    private ComponentRenderSystem _renderSystem;
    
    private Tilemap _tilemap;
    private Rectangle _roomBounds;
    private bool _gameInitialized;
    private bool _tilemapSet;
    
    public Game1() : base(GameConfig.WindowTitle, GameConfig.WindowWidth, GameConfig.WindowHeight, GameConfig.StartFullscreen)
    {
    }

    protected override void Initialize()
    {
        base.Initialize();
        
        // Create event bus first
        _eventBus = new EventBus();
        
        // Create managers
        _systemManager = new SystemManager();
        _entityManager = new ComponentEntityManager(_eventBus);
        
        // Create component-based systems
        _inputSystem = new ComponentInputSystem();
        _movementSystem = new ComponentMovementSystem();
        _collisionSystem = new ComponentCollisionSystem();
        _renderSystem = new ComponentRenderSystem();
        
        // Set up system dependencies
        _inputSystem.SetEntityManager(_entityManager);
        _inputSystem.SetEventBus(_eventBus);
        
        _movementSystem.SetEntityManager(_entityManager);
        _movementSystem.SetEventBus(_eventBus);
        
        _collisionSystem.SetEntityManager(_entityManager);
        _collisionSystem.SetEventBus(_eventBus);
        
        _renderSystem.SetEntityManager(_entityManager);
        _renderSystem.SetEventBus(_eventBus);
        
        // Add systems to manager in execution order
        _systemManager.AddSystem(_inputSystem);
        _systemManager.AddSystem(_movementSystem);
        _systemManager.AddSystem(_collisionSystem);
        _systemManager.AddSystem(_renderSystem);
        
        _systemManager.Initialize();
    }

    protected override void LoadContent()
    {
        base.LoadContent();
        
        // Initialize entity manager with content
        if (_entityManager != null)
        {
            _entityManager.Initialize(Content);
        }
        
        // Load tilemap
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
        
        // Don't set tilemap here - do it in Update when systems are fully ready
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        // Set tilemap for render system on first update (when systems are fully ready)
        if (!_tilemapSet && _renderSystem != null && _tilemap != null)
        {
            Console.WriteLine($"Setting tilemap in render system: {_tilemap.Rows}x{_tilemap.Columns}");
            _renderSystem.SetTilemap(_tilemap);
            _tilemapSet = true;
        }

        // Initialize game entities on first update (when GraphicsDevice is ready)
        if (!_gameInitialized)
        {
            InitializeGame();
        }

        _systemManager.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(GameConfig.BackgroundColor);

        SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _systemManager.Draw(SpriteBatch);
        SpriteBatch.End();

        base.Draw(gameTime);
    }
    
    private void InitializeGame()
    {
        // Make sure entity manager is initialized
        if (_entityManager != null)
        {
            _entityManager.Initialize(Content);
        }
        
        // Calculate room bounds
        Rectangle screenBounds = GraphicsDevice.PresentationParameters.Bounds;
        _roomBounds = new Rectangle(
            (int)_tilemap.TileWidth,
            (int)_tilemap.TileHeight,
            screenBounds.Width - (int)_tilemap.TileWidth * 2,
            screenBounds.Height - (int)_tilemap.TileHeight * 2
        );
        
        // Set bounds for collision system
        _collisionSystem.SetBounds(_roomBounds, _tilemap);
        
        // Create player entity
        int centerRow = _tilemap.Rows / 2;
        int centerColumn = _tilemap.Columns / 2;
        Vector2 playerStartPos = new(centerColumn * _tilemap.TileWidth, centerRow * _tilemap.TileHeight);
        
        var playerEntity = _entityManager.CreatePlayer(playerStartPos, PlayerIndex.One);
        
        // Create cop entities
        Vector2 copStartPos1 = new(_roomBounds.Left + 50, _roomBounds.Top + 50);
        Vector2 copStartPos2 = new(_roomBounds.Right - 100, _roomBounds.Bottom - 100);
        
        var cop1 = _entityManager.CreateCop(copStartPos1, AIBehavior.Patrol);
        var cop2 = _entityManager.CreateCop(copStartPos2, AIBehavior.Wander);
        
        // Add bounds constraints to all entities
        _entityManager.AddBoundsConstraint(playerEntity, _roomBounds, false); // Player clamps
        _entityManager.AddBoundsConstraint(cop1, _roomBounds, true); // Cops reflect
        _entityManager.AddBoundsConstraint(cop2, _roomBounds, true);
        
        _gameInitialized = true;
        
        // Subscribe to events for debugging
        _eventBus.Subscribe<EntitySpawnEvent>(OnEntitySpawn);
        _eventBus.Subscribe<PlayerCopCollisionEvent>(OnPlayerCopCollision);
        _eventBus.Subscribe<TeleportEvent>(OnTeleport);
    }
    
    // Event handlers for debugging
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
}