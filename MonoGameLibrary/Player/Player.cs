using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;

namespace MonoGameLibrary.Player;

public class Player
{
    public bool DebugMode { get; set; }
    private float _movementSpeed = 5f;
    public Vector2 _position { get; set; }
    public AnimatedSprite _sprite { get; set; }
    public RectangleCollider.RectangleCollider _collider { get; set; }

    public Player()
    {

    }

    public Player(Vector2 position, AnimatedSprite sprite, RectangleCollider.RectangleCollider collider, bool debugMode, Vector2 scale)
    {
        _position = position;
        _sprite = sprite;
        _collider = collider;
        DebugMode = debugMode;
        _sprite.Scale = scale;
    }

    public void UpdateCollider()
    {
        var colliderWidth = _sprite.Width * 0.5f;
        var colliderHeight = _sprite.Height * 1f;
        _collider = new RectangleCollider.RectangleCollider(
            (int)(_position.X + (_sprite.Width - colliderWidth) / 2),
            (int)(_position.Y + (_sprite.Height - colliderHeight) / 2),
            (int)colliderWidth,
            (int)colliderHeight,
            DebugMode
            );
    }

    public void UpdatePosition(Vector2 newPosition)
    {
        _position = newPosition;
        UpdateCollider();
    }

}