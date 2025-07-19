using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;

namespace MonoGameLibrary.Entity;

public abstract class GameEntity
{
    public bool DebugMode { get; set; }
    protected Vector2 _position;
    protected AnimatedSprite _sprite;
    protected RectangleCollider.RectangleCollider _collider;
    
    public Vector2 Position => _position;
    public AnimatedSprite Sprite => _sprite;
    public RectangleCollider.RectangleCollider Collider => _collider;

    protected GameEntity()
    {
    }

    protected GameEntity(Vector2 position, AnimatedSprite sprite, bool debugMode, Vector2 scale)
    {
        _position = position;
        _sprite = sprite;
        DebugMode = debugMode;
        _sprite.Scale = scale;
        UpdateCollider();
    }

    protected virtual void UpdateCollider()
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

    public virtual void UpdatePosition(Vector2 newPosition)
    {
        _position = newPosition;
        UpdateCollider();
    }

    public virtual void Draw(SpriteBatch spriteBatch)
    {
        _sprite.Draw(spriteBatch, _position);
    }

    public Rectangle GetBounds()
    {
        return _collider.rectangleCollider;
    }

    public virtual void Update(GameTime gameTime)
    {
        _sprite.Update(gameTime);
    }
}