using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Player;
using MonoGameLibrary.Enemy;
using MonoGameLibrary.Graphics;

namespace PrisonBreak.Systems;

public class CollisionSystem : IGameSystem
{
    private Player _player;
    private Cop _cop;
    private Rectangle _roomBounds;
    private Tilemap _tilemap;

    public void Initialize()
    {
    }

    public void SetEntities(Player player, Cop cop)
    {
        _player = player;
        _cop = cop;
    }

    public void SetBounds(Rectangle roomBounds, Tilemap tilemap)
    {
        _roomBounds = roomBounds;
        _tilemap = tilemap;
    }

    public void Update(GameTime gameTime)
    {
        if (_player != null)
        {
            ConstrainPlayerToBounds();
        }

        if (_cop != null)
        {
            _cop.ConstrainToBounds(_roomBounds);
        }

        CheckPlayerCopCollision();
    }

    public void Draw(SpriteBatch spriteBatch)
    {
    }

    public void Shutdown()
    {
    }

    private void ConstrainPlayerToBounds()
    {
        Vector2 newPosition = _player.Position;
        bool positionChanged = false;

        float colliderXOffset = _player.Sprite.Width * 0.25f;

        if (_player.Collider.rectangleCollider.Left < _roomBounds.Left)
        {
            newPosition.X = _roomBounds.Left - colliderXOffset;
            positionChanged = true;
        }
        else if (_player.Collider.rectangleCollider.Right > _roomBounds.Right)
        {
            newPosition.X = _roomBounds.Right - _player.Sprite.Width + colliderXOffset;
            positionChanged = true;
        }

        if (_player.Collider.rectangleCollider.Top < _roomBounds.Top)
        {
            newPosition.Y = _roomBounds.Top;
            positionChanged = true;
        }
        else if (_player.Collider.rectangleCollider.Bottom > _roomBounds.Bottom)
        {
            newPosition.Y = _roomBounds.Bottom - _player.Sprite.Height;
            positionChanged = true;
        }

        if (positionChanged)
        {
            _player.UpdatePosition(newPosition);
        }
    }

    private void CheckPlayerCopCollision()
    {
        if (_player == null || _cop == null)
            return;

        if (_player.GetBounds().Intersects(_cop.GetBounds()))
        {
            _cop.TeleportToRandomPosition(_roomBounds, _tilemap.TileWidth, _tilemap.TileHeight);
        }
    }
}