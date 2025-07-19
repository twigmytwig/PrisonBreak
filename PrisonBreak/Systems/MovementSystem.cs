using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Player;
using MonoGameLibrary.Enemy;
using PrisonBreak.Config;

namespace PrisonBreak.Systems;

public class MovementSystem : IGameSystem
{

    private Player _player;
    private Cop _cop;
    private InputSystem _inputSystem;

    public void Initialize()
    {
    }

    public void SetEntities(Player player, Cop cop)
    {
        _player = player;
        _cop = cop;
    }

    public void SetInputSystem(InputSystem inputSystem)
    {
        _inputSystem = inputSystem;
    }

    public void Update(GameTime gameTime)
    {
        if (_player != null && _inputSystem != null)
        {
            UpdatePlayerMovement();
        }

        if (_cop != null)
        {
            UpdateCopMovement();
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
    }

    public void Shutdown()
    {
    }

    private void UpdatePlayerMovement()
    {
        MovementInput input = _inputSystem.PlayerMovement;
        
        if (input.Direction == Vector2.Zero)
            return;

        float speed = GameConfig.BaseMovementSpeed;
        if (input.SpeedBoost)
        {
            speed *= GameConfig.SpeedBoostMultiplier;
        }

        Vector2 movement = input.Direction * speed;
        Vector2 newPosition = _player.Position + movement;
        
        _player.UpdatePosition(newPosition);
    }

    private void UpdateCopMovement()
    {
        Vector2 newPosition = _cop.Position + _cop.Velocity;
        _cop.UpdatePosition(newPosition);
    }
}