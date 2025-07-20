using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PrisonBreak.Core;
using PrisonBreak.Core.Graphics;
using PrisonBreak.Config;
using PrisonBreak.Managers;
using PrisonBreak.Systems;
using PrisonBreak.ECS;
using PrisonBreak.ECS.Systems;
using PrisonBreak.Scenes;

namespace PrisonBreak.Game;

public class Game1 : PrisonBreak.Core.Core
{
    // Scene management
    private SceneManager _sceneManager;
    private EventBus _eventBus;

    public Game1() : base(GameConfig.WindowTitle, GameConfig.WindowWidth, GameConfig.WindowHeight, GameConfig.StartFullscreen)
    {
    }

    protected override void Initialize()
    {
        // Create event bus first
        _eventBus = new EventBus();

        // Create scene manager
        _sceneManager = new SceneManager(_eventBus);

        // Register scenes
        _sceneManager.RegisterScene(SceneType.StartMenu, new StartMenuScene(_eventBus));
        _sceneManager.RegisterScene(SceneType.Gameplay, new GameplayScene(_eventBus));

        // Initialize with start menu
        _sceneManager.Initialize(SceneType.StartMenu);

        // Call base.Initialize() after our setup (this will call LoadContent)
        base.Initialize();
    }

    protected override void LoadContent()
    {
        base.LoadContent();

        // Load content for current scene
        _sceneManager.LoadContent(Content);
    }

    protected override void Update(GameTime gameTime)
    {
        // Exit only if we get the gamepad back button (not Escape, as that's handled by scenes)
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            Exit();

        // Update current scene
        _sceneManager.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(GameConfig.BackgroundColor);

        SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _sceneManager.Draw(SpriteBatch);
        SpriteBatch.End();

        base.Draw(gameTime);
    }
}