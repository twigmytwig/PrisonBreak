using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Input;

namespace PrisonBreak.Systems;

public struct MovementInput
{
    public Vector2 Direction;
    public bool SpeedBoost;
    
    public MovementInput(Vector2 direction, bool speedBoost)
    {
        Direction = direction;
        SpeedBoost = speedBoost;
    }
}

public class InputSystem : IGameSystem
{
    public MovementInput PlayerMovement { get; private set; }

    public void Initialize()
    {
        PlayerMovement = new MovementInput(Vector2.Zero, false);
    }

    public void Update(GameTime gameTime)
    {
        Vector2 movement = Vector2.Zero;
        bool speedBoost = false;

        CheckKeyboardInput(ref movement, ref speedBoost);
        CheckGamePadInput(ref movement, ref speedBoost);

        PlayerMovement = new MovementInput(movement, speedBoost);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
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
    }

    private void CheckGamePadInput(ref Vector2 movement, ref bool speedBoost)
    {
        GamePadInfo gamePadOne = Core.Input.GamePads[(int)PlayerIndex.One];

        if (gamePadOne.IsButtonDown(Buttons.A))
        {
            speedBoost = true;
            GamePad.SetVibration(PlayerIndex.One, 1.0f, 1.0f);
        }
        else
        {
            GamePad.SetVibration(PlayerIndex.One, 0.0f, 0.0f);
        }

        if (gamePadOne.LeftThumbStick != Vector2.Zero)
        {
            movement.X += gamePadOne.LeftThumbStick.X;
            movement.Y -= gamePadOne.LeftThumbStick.Y;
        }
        else
        {
            if (gamePadOne.IsButtonDown(Buttons.DPadUp))
            {
                movement.Y -= 1.0f;
            }

            if (gamePadOne.IsButtonDown(Buttons.DPadDown))
            {
                movement.Y += 1.0f;
            }

            if (gamePadOne.IsButtonDown(Buttons.DPadLeft))
            {
                movement.X -= 1.0f;
            }

            if (gamePadOne.IsButtonDown(Buttons.DPadRight))
            {
                movement.X += 1.0f;
            }
        }
    }
}