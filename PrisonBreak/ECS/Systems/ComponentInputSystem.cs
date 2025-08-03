using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PrisonBreak.Systems;

namespace PrisonBreak.ECS.Systems;

public class ComponentInputSystem : IGameSystem
{
    private ComponentEntityManager _entityManager;
    private EventBus _eventBus;
    private KeyboardState _previousKeyboard;
    private GamePadState[] _previousGamePads;
    
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
        _previousKeyboard = Keyboard.GetState();
        _previousGamePads = new GamePadState[4];
        for (int i = 0; i < 4; i++)
        {
            _previousGamePads[i] = GamePad.GetState((PlayerIndex)i);
        }
    }
    
    public void Update(GameTime gameTime)
    {
        if (_entityManager == null || _eventBus == null) return;
        
        // Process all entities with player input components
        var playerEntities = _entityManager.GetEntitiesWith<PlayerInputComponent, TransformComponent>();
        
        foreach (var entity in playerEntities)
        {
            ref var input = ref entity.GetComponent<PlayerInputComponent>();
            if (!input.IsActive) continue;
            
            var movementDirection = Vector2.Zero;
            bool speedBoost = false;
            
            // Check keyboard input for this player
            if (input.PlayerIndex == PlayerIndex.One)
            {
                CheckKeyboardInput(entity, ref movementDirection, ref speedBoost);
            }
            
            // Check gamepad input
            CheckGamePadInput(entity, input.PlayerIndex, ref movementDirection, ref speedBoost);
            
            // Always send input event for player entities (including when stopped)
            _eventBus.Send(new PlayerInputEvent(entity.Id, movementDirection, speedBoost));
        }
        
        // Update previous input states at the end of the frame
        _previousKeyboard = Keyboard.GetState();
        for (int i = 0; i < 4; i++)
        {
            _previousGamePads[i] = GamePad.GetState((PlayerIndex)i);
        }
    }
    
    public void Draw(SpriteBatch spriteBatch)
    {
        // Input system doesn't draw anything
    }
    
    public void Shutdown()
    {
    }
    
    private void CheckKeyboardInput(Entity entity, ref Vector2 movement, ref bool speedBoost)
    {
        var currentKeyboard = Keyboard.GetState();
        
        if (currentKeyboard.IsKeyDown(Keys.Space))
        {
            speedBoost = true;
        }

        if (currentKeyboard.IsKeyDown(Keys.W) || currentKeyboard.IsKeyDown(Keys.Up))
        {
            movement.Y -= 1.0f;
        }

        if (currentKeyboard.IsKeyDown(Keys.S) || currentKeyboard.IsKeyDown(Keys.Down))
        {
            movement.Y += 1.0f;
        }

        if (currentKeyboard.IsKeyDown(Keys.A) || currentKeyboard.IsKeyDown(Keys.Left))
        {
            movement.X -= 1.0f;
        }

        if (currentKeyboard.IsKeyDown(Keys.D) || currentKeyboard.IsKeyDown(Keys.Right))
        {
            movement.X += 1.0f;
        }
        
        // Check for interaction input (E key press, not hold)
        if (currentKeyboard.IsKeyDown(Keys.E) && !_previousKeyboard.IsKeyDown(Keys.E))
        {
            _eventBus.Send(new InteractionInputEvent(entity.Id));
        }
        
        // Normalize diagonal movement
        if (movement != Vector2.Zero)
        {
            movement.Normalize();
        }
    }
    
    private void CheckGamePadInput(Entity entity, PlayerIndex playerIndex, ref Vector2 movement, ref bool speedBoost)
    {
        var gamePadState = GamePad.GetState(playerIndex);
        var previousGamePad = _previousGamePads[(int)playerIndex];

        if (gamePadState.IsButtonDown(Buttons.A))
        {
            speedBoost = true;
            GamePad.SetVibration(playerIndex, 1.0f, 1.0f);
        }
        else
        {
            GamePad.SetVibration(playerIndex, 0.0f, 0.0f);
        }

        // Check for interaction input (X button press, not hold)
        if (gamePadState.IsButtonDown(Buttons.X) && !previousGamePad.IsButtonDown(Buttons.X))
        {
            _eventBus.Send(new InteractionInputEvent(entity.Id));
        }

        // Check thumbstick first (analog input has priority)
        if (gamePadState.ThumbSticks.Left != Vector2.Zero)
        {
            movement.X += gamePadState.ThumbSticks.Left.X;
            movement.Y -= gamePadState.ThumbSticks.Left.Y; // Invert Y for screen coordinates
        }
        else
        {
            // Fallback to D-Pad for digital input
            if (gamePadState.IsButtonDown(Buttons.DPadUp))
            {
                movement.Y -= 1.0f;
            }

            if (gamePadState.IsButtonDown(Buttons.DPadDown))
            {
                movement.Y += 1.0f;
            }

            if (gamePadState.IsButtonDown(Buttons.DPadLeft))
            {
                movement.X -= 1.0f;
            }

            if (gamePadState.IsButtonDown(Buttons.DPadRight))
            {
                movement.X += 1.0f;
            }
        }
        
        // Normalize movement if using D-Pad (thumbstick is already normalized)
        if (gamePadState.ThumbSticks.Left == Vector2.Zero && movement != Vector2.Zero)
        {
            movement.Normalize();
        }
    }
}