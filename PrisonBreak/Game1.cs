using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using PrisonBreak.Config;
using PrisonBreak.Managers;
using PrisonBreak.Systems;

namespace PrisonBreak;

public class Game1 : Core
{
    private SystemManager _systemManager;
    private EntityManager _entityManager;
    private InputSystem _inputSystem;
    private MovementSystem _movementSystem;
    private CollisionSystem _collisionSystem;
    private RenderSystem _renderSystem;
    
    private Tilemap _tilemap;
    private Rectangle _roomBounds;
    private bool _roomBoundsInitialized;

    public Game1() : base(GameConfig.WindowTitle, GameConfig.WindowWidth, GameConfig.WindowHeight, GameConfig.StartFullscreen)
    {

    }

    protected override void Initialize()
    {
        base.Initialize();
        
        _systemManager = new SystemManager();
        _entityManager = new EntityManager();
        
        _inputSystem = new InputSystem();
        _movementSystem = new MovementSystem();
        _collisionSystem = new CollisionSystem();
        _renderSystem = new RenderSystem();
        
        _systemManager.AddSystem(_inputSystem);
        _systemManager.AddSystem(_movementSystem);
        _systemManager.AddSystem(_collisionSystem);
        _systemManager.AddSystem(_renderSystem);
        
        _systemManager.Initialize();
    }

    protected override void LoadContent()
    {
        _tilemap = Tilemap.FromFile(Content, EntityConfig.Tilemap.ConfigFile);
        _tilemap.Scale = EntityConfig.Tilemap.Scale;
        
        if (_renderSystem != null)
        {
            _renderSystem.SetTilemap(_tilemap);
        }
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        if (!_roomBoundsInitialized && _entityManager?.Player == null)
        {
            InitializeRoomBounds();
        }

        _systemManager.Update(gameTime);
        base.Update(gameTime);
    }

    private void InitializeRoomBounds()
    {
        // Only initialize if the EntityManager has been initialized with the atlas
        if (_entityManager?.Player != null)
        {
            return; // Already initialized
        }
        
        // Ensure EntityManager is initialized first
        if (_entityManager != null)
        {
            _entityManager.Initialize(Content);
        }
        
        Rectangle screenBounds = GraphicsDevice.PresentationParameters.Bounds;
        _roomBounds = new Rectangle(
            (int)_tilemap.TileWidth,
            (int)_tilemap.TileHeight,
            screenBounds.Width - (int)_tilemap.TileWidth * 2,
            screenBounds.Height - (int)_tilemap.TileHeight * 2
        );
        
        int centerRow = _tilemap.Rows / 2;
        int centerColumn = _tilemap.Columns / 2;
        Vector2 playerStartPos = new(centerColumn * _tilemap.TileWidth, centerRow * _tilemap.TileHeight);
        Vector2 copStartPos = new(_roomBounds.Left, _roomBounds.Top);
        
        _entityManager.CreatePlayer(playerStartPos);
        _entityManager.CreateCop(copStartPos);
        
        _movementSystem.SetEntities(_entityManager.Player, _entityManager.Cop);
        _movementSystem.SetInputSystem(_inputSystem);
        
        _collisionSystem.SetEntities(_entityManager.Player, _entityManager.Cop);
        _collisionSystem.SetBounds(_roomBounds, _tilemap);
        
        _renderSystem.SetEntities(_entityManager.Player, _entityManager.Cop);
        _renderSystem.SetTilemap(_tilemap);
        
        _roomBoundsInitialized = true;
    }




    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(GameConfig.BackgroundColor);

        SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _systemManager.Draw(SpriteBatch);
        SpriteBatch.End();

        base.Draw(gameTime);
    }
}
