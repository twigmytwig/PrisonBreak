using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using PrisonBreak.Systems;

namespace PrisonBreak.ECS;

public class ComponentInputSystem : IGameSystem
{
    private ComponentEntityManager _entityManager;
    private EventBus _eventBus;
    
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
                CheckKeyboardInput(ref movementDirection, ref speedBoost);
            }
            
            // Check gamepad input
            CheckGamePadInput(input.PlayerIndex, ref movementDirection, ref speedBoost);
            
            // Always send input event for player entities (including when stopped)
            _eventBus.Send(new PlayerInputEvent(entity.Id, movementDirection, speedBoost));
        }
    }
    
    public void Draw(SpriteBatch spriteBatch)
    {
        // Input system doesn't draw anything
    }
    
    public void Shutdown()
    {
    }
    
    private void CheckKeyboardInput(ref Vector2 movement, ref bool speedBoost)
    {
        if (Core.Input.Keyboard.IsKeyDown(Keys.Space))
        {
            speedBoost = true;
        }

        if (Core.Input.Keyboard.IsKeyDown(Keys.W) || Core.Input.Keyboard.IsKeyDown(Keys.Up))
        {
            movement.Y -= 1.0f;
        }

        if (Core.Input.Keyboard.IsKeyDown(Keys.S) || Core.Input.Keyboard.IsKeyDown(Keys.Down))
        {
            movement.Y += 1.0f;
        }

        if (Core.Input.Keyboard.IsKeyDown(Keys.A) || Core.Input.Keyboard.IsKeyDown(Keys.Left))
        {
            movement.X -= 1.0f;
        }

        if (Core.Input.Keyboard.IsKeyDown(Keys.D) || Core.Input.Keyboard.IsKeyDown(Keys.Right))
        {
            movement.X += 1.0f;
        }
        
        // Normalize diagonal movement
        if (movement != Vector2.Zero)
        {
            movement.Normalize();
        }
    }
    
    private void CheckGamePadInput(PlayerIndex playerIndex, ref Vector2 movement, ref bool speedBoost)
    {
        var gamePad = Core.Input.GamePads[(int)playerIndex];

        if (gamePad.IsButtonDown(Buttons.A))
        {
            speedBoost = true;
            GamePad.SetVibration(playerIndex, 1.0f, 1.0f);
        }
        else
        {
            GamePad.SetVibration(playerIndex, 0.0f, 0.0f);
        }

        // Check thumbstick first (analog input has priority)
        if (gamePad.LeftThumbStick != Vector2.Zero)
        {
            movement.X += gamePad.LeftThumbStick.X;
            movement.Y -= gamePad.LeftThumbStick.Y; // Invert Y for screen coordinates
        }
        else
        {
            // Fallback to D-Pad for digital input
            if (gamePad.IsButtonDown(Buttons.DPadUp))
            {
                movement.Y -= 1.0f;
            }

            if (gamePad.IsButtonDown(Buttons.DPadDown))
            {
                movement.Y += 1.0f;
            }

            if (gamePad.IsButtonDown(Buttons.DPadLeft))
            {
                movement.X -= 1.0f;
            }

            if (gamePad.IsButtonDown(Buttons.DPadRight))
            {
                movement.X += 1.0f;
            }
        }
        
        // Normalize movement if using D-Pad (thumbstick is already normalized)
        if (gamePad.LeftThumbStick == Vector2.Zero && movement != Vector2.Zero)
        {
            movement.Normalize();
        }
    }
}