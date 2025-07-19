using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Entity;

namespace MonoGameLibrary.Player;

public class Player : GameEntity
{
    public Player() : base()
    {
    }

    public Player(Vector2 position, AnimatedSprite sprite, RectangleCollider.RectangleCollider collider, bool debugMode, Vector2 scale)
        : base(position, sprite, debugMode, scale)
    {
        // Player-specific initialization if needed
    }
}