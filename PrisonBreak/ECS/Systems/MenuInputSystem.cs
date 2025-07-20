using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PrisonBreak.Systems;

namespace PrisonBreak.ECS.Systems;

/// <summary>
/// Handles input for menu navigation
/// </summary>
public class MenuInputSystem : IGameSystem
{
    private ComponentEntityManager _entityManager;
    private EventBus _eventBus;
    private KeyboardState _previousKeyboardState;
    private GamePadState _previousGamePadState;
    
    public void SetEntityManager(ComponentEntityManager entityManager)
    {
        _entityManager = entityManager;
    }
    
    public void SetEventBus(EventBus eventBus)
    {
        _eventBus = eventBus;
    }
    
    public void Initialize()
    {
        _previousKeyboardState = Keyboard.GetState();
        _previousGamePadState = GamePad.GetState(PlayerIndex.One);
    }
    
    public void Update(GameTime gameTime)
    {
        if (_eventBus == null) return;
        
        var currentKeyboardState = Keyboard.GetState();
        var currentGamePadState = GamePad.GetState(PlayerIndex.One);
        
        // Check for menu navigation inputs
        bool upPressed = IsKeyJustPressed(currentKeyboardState, Keys.Up) || 
                        IsKeyJustPressed(currentKeyboardState, Keys.W) ||
                        IsButtonJustPressed(currentGamePadState, Buttons.DPadUp) ||
                        (currentGamePadState.ThumbSticks.Left.Y > 0.5f && _previousGamePadState.ThumbSticks.Left.Y <= 0.5f);
        
        bool downPressed = IsKeyJustPressed(currentKeyboardState, Keys.Down) || 
                          IsKeyJustPressed(currentKeyboardState, Keys.S) ||
                          IsButtonJustPressed(currentGamePadState, Buttons.DPadDown) ||
                          (currentGamePadState.ThumbSticks.Left.Y < -0.5f && _previousGamePadState.ThumbSticks.Left.Y >= -0.5f);
        
        bool leftPressed = IsKeyJustPressed(currentKeyboardState, Keys.Left) || 
                          IsKeyJustPressed(currentKeyboardState, Keys.A) ||
                          IsButtonJustPressed(currentGamePadState, Buttons.DPadLeft) ||
                          (currentGamePadState.ThumbSticks.Left.X < -0.5f && _previousGamePadState.ThumbSticks.Left.X >= -0.5f);
        
        bool rightPressed = IsKeyJustPressed(currentKeyboardState, Keys.Right) || 
                           IsKeyJustPressed(currentKeyboardState, Keys.D) ||
                           IsButtonJustPressed(currentGamePadState, Buttons.DPadRight) ||
                           (currentGamePadState.ThumbSticks.Left.X > 0.5f && _previousGamePadState.ThumbSticks.Left.X <= 0.5f);
        
        bool selectPressed = IsKeyJustPressed(currentKeyboardState, Keys.Enter) || 
                            IsKeyJustPressed(currentKeyboardState, Keys.Space) ||
                            IsButtonJustPressed(currentGamePadState, Buttons.A);
        
        bool backPressed = IsKeyJustPressed(currentKeyboardState, Keys.Escape) || 
                          IsButtonJustPressed(currentGamePadState, Buttons.B) ||
                          IsButtonJustPressed(currentGamePadState, Buttons.Back);
        
        // Send menu navigation events
        if (upPressed)
            _eventBus.Send(new MenuNavigationEvent(MenuNavigation.Up));
        
        if (downPressed)
            _eventBus.Send(new MenuNavigationEvent(MenuNavigation.Down));
        
        if (leftPressed)
            _eventBus.Send(new MenuNavigationEvent(MenuNavigation.Left));
        
        if (rightPressed)
            _eventBus.Send(new MenuNavigationEvent(MenuNavigation.Right));
        
        if (selectPressed)
            _eventBus.Send(new MenuNavigationEvent(MenuNavigation.Select));
        
        if (backPressed)
            _eventBus.Send(new MenuNavigationEvent(MenuNavigation.Back));
        
        // Store current states for next frame
        _previousKeyboardState = currentKeyboardState;
        _previousGamePadState = currentGamePadState;
    }
    
    public void Draw(SpriteBatch spriteBatch)
    {
        // Menu input system doesn't draw anything
    }
    
    public void Shutdown()
    {
        // Clean up if needed
    }
    
    private bool IsKeyJustPressed(KeyboardState currentState, Keys key)
    {
        return currentState.IsKeyDown(key) && !_previousKeyboardState.IsKeyDown(key);
    }
    
    private bool IsButtonJustPressed(GamePadState currentState, Buttons button)
    {
        return currentState.IsButtonDown(button) && !_previousGamePadState.IsButtonDown(button);
    }
}

/// <summary>
/// Menu navigation directions
/// </summary>
public enum MenuNavigation
{
    Up,
    Down,
    Left,
    Right,
    Select,
    Back
}

/// <summary>
/// Event for menu navigation input
/// </summary>
public struct MenuNavigationEvent
{
    public MenuNavigation Direction;
    
    public MenuNavigationEvent(MenuNavigation direction)
    {
        Direction = direction;
    }
}