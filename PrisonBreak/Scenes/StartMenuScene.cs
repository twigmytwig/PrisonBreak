using System;
using Microsoft.Xna.Framework;
using PrisonBreak.ECS;
using PrisonBreak.ECS.Systems;

namespace PrisonBreak.Scenes;

/// <summary>
/// Start menu scene with player type selection
/// </summary>
public class StartMenuScene : Scene
{
    private MenuInputSystem _menuInputSystem;
    private MenuRenderSystem _menuRenderSystem;

    private Entity _titleEntity;
    private Entity _startGameEntity;
    private Entity _exitGameEntity;
    private Entity _playerTypeEntity;

    private int _selectedIndex = 0;
    private readonly string[] _menuItems = { "Start Game", "Exit" };
    private PlayerType _selectedPlayerType = PlayerType.Prisoner;
    private bool _entitiesCreated = false;

    public StartMenuScene(EventBus eventBus) : base("Start Menu", eventBus)
    {
    }

    protected override void SetupSystems()
    {
        // Create menu-specific systems
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
        // Set content for render system so it can load fonts
        _menuRenderSystem.SetContent(Content);

        // Don't create entities here - wait until first Update when GraphicsDevice is available
    }

    private void CreateMenuEntities()
    {
        // Get screen center
        var graphicsDevice = PrisonBreak.Core.Core.GraphicsDevice;
        if (graphicsDevice == null)
        {
            Console.WriteLine("ERROR: GraphicsDevice is null in CreateMenuEntities");
            return;
        }

        int screenWidth = graphicsDevice.PresentationParameters.BackBufferWidth;
        int screenHeight = graphicsDevice.PresentationParameters.BackBufferHeight;
        Vector2 screenCenter = new Vector2(screenWidth / 2, screenHeight / 2);

        Console.WriteLine($"Creating menu entities. Screen: {screenWidth}x{screenHeight}, Center: {screenCenter}");

        // Create title
        _titleEntity = EntityManager.CreateEntity();
        _titleEntity.AddComponent(new TransformComponent(new Vector2(screenCenter.X, screenCenter.Y - 150)));
        _titleEntity.AddComponent(new TextComponent("PRISON BREAK")
        {
            Color = Color.White,
            Alignment = TextAlignment.Center,
            DrawOrder = 10
        });

        // Create player type selection
        _playerTypeEntity = EntityManager.CreateEntity();
        _playerTypeEntity.AddComponent(new TransformComponent(new Vector2(screenCenter.X, screenCenter.Y - 80)));
        _playerTypeEntity.AddComponent(new TextComponent($"Player Type: {_selectedPlayerType}")
        {
            Color = Color.Yellow,
            Alignment = TextAlignment.Center,
            DrawOrder = 10
        });

        // Create start game button
        _startGameEntity = EntityManager.CreateEntity();
        var startGameTransform = new TransformComponent(new Vector2(screenCenter.X - 100, screenCenter.Y - 20));
        _startGameEntity.AddComponent(startGameTransform);
        _startGameEntity.AddComponent(new MenuItemComponent(200, 40, "start_game")
        {
            DrawOrder = 0
        });
        _startGameEntity.AddComponent(new TextComponent("Start Game")
        {
            Color = Color.Yellow,
            Alignment = TextAlignment.TopLeft,
            DrawOrder = 10
        });

        // Create exit game button
        _exitGameEntity = EntityManager.CreateEntity();
        var exitGameTransform = new TransformComponent(new Vector2(screenCenter.X - 100, screenCenter.Y + 30));
        _exitGameEntity.AddComponent(exitGameTransform);
        _exitGameEntity.AddComponent(new MenuItemComponent(200, 40, "exit_game")
        {
            DrawOrder = 0
        });
        _exitGameEntity.AddComponent(new TextComponent("Exit")
        {
            Color = Color.White,
            Alignment = TextAlignment.TopLeft,
            DrawOrder = 10
        });

        Console.WriteLine($"Created entities: Title={_titleEntity?.Id}, PlayerType={_playerTypeEntity?.Id}, Start={_startGameEntity?.Id}, Exit={_exitGameEntity?.Id}");
        _entitiesCreated = true;
    }

    public override void Update(GameTime gameTime)
    {
        // Create entities on first update when GraphicsDevice is available
        if (!_entitiesCreated && IsContentLoaded)
        {
            CreateMenuEntities();
            if (_startGameEntity != null && _exitGameEntity != null)
            {
                UpdateMenuSelection();
                UpdatePlayerTypeDisplay();
            }
        }

        base.Update(gameTime);
    }

    private void OnMenuNavigation(MenuNavigationEvent navEvent)
    {
        switch (navEvent.Direction)
        {
            case MenuNavigation.Up:
                _selectedIndex = Math.Max(0, _selectedIndex - 1);
                UpdateMenuSelection();
                break;

            case MenuNavigation.Down:
                _selectedIndex = Math.Min(_menuItems.Length - 1, _selectedIndex + 1);
                UpdateMenuSelection();
                break;

            case MenuNavigation.Left:
            case MenuNavigation.Right:
                // Toggle player type when on start game option
                if (_selectedIndex == 0)
                {
                    _selectedPlayerType = _selectedPlayerType == PlayerType.Prisoner ? PlayerType.Cop : PlayerType.Prisoner;
                    UpdatePlayerTypeDisplay();
                }
                break;

            case MenuNavigation.Select:
                HandleMenuSelection();
                break;

            case MenuNavigation.Back:
                // Exit the game
                Environment.Exit(0);
                break;
        }
    }

    private void UpdateMenuSelection()
    {
        // Update start game button selection
        if (_startGameEntity != null && _startGameEntity.HasComponent<MenuItemComponent>())
        {
            ref var startMenuItem = ref _startGameEntity.GetComponent<MenuItemComponent>();
            startMenuItem.IsSelected = _selectedIndex == 0;
            if (_startGameEntity.HasComponent<TextComponent>())
            {
                ref var textComp = ref _startGameEntity.GetComponent<TextComponent>();
                textComp.Color = _selectedIndex == 0 ? Color.Yellow : Color.White;
            }
        }

        // Update exit game button selection
        if (_exitGameEntity != null && _exitGameEntity.HasComponent<MenuItemComponent>())
        {
            ref var exitMenuItem = ref _exitGameEntity.GetComponent<MenuItemComponent>();
            exitMenuItem.IsSelected = _selectedIndex == 1;
            if (_exitGameEntity.HasComponent<TextComponent>())
            {
                ref var exitText = ref _exitGameEntity.GetComponent<TextComponent>();
                exitText.Color = _selectedIndex == 1 ? Color.Yellow : Color.White;
            }
        }
    }

    private void UpdatePlayerTypeDisplay()
    {
        if (_playerTypeEntity != null && _playerTypeEntity.HasComponent<TextComponent>())
        {
            ref var textComp = ref _playerTypeEntity.GetComponent<TextComponent>();
            textComp.Text = $"Player Type: {_selectedPlayerType} (Press Left/Right to change)";
        }
    }

    private void HandleMenuSelection()
    {
        switch (_selectedIndex)
        {
            case 0: // Start Game
                var gameStartData = new GameStartData(_selectedPlayerType, PlayerIndex.One);
                EventBus.Send(new SceneTransitionEvent(SceneType.StartMenu, SceneType.Gameplay, gameStartData));
                break;

            case 1: // Exit
                Environment.Exit(0);
                break;
        }
    }

    public override void OnEnter()
    {
        Console.WriteLine("Entered Start Menu Scene");
        _selectedIndex = 0;
        _selectedPlayerType = PlayerType.Prisoner;

        if (IsContentLoaded && _startGameEntity != null && _exitGameEntity != null)
        {
            UpdateMenuSelection();
            UpdatePlayerTypeDisplay();
        }

        base.OnEnter();
    }

    public override void OnExit()
    {
        Console.WriteLine("Exited Start Menu Scene");
        EventBus.Unsubscribe<MenuNavigationEvent>(OnMenuNavigation);
        base.OnExit();
    }
}