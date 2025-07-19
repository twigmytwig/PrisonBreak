using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Entity;

namespace MonoGameLibrary.Enemy;

public abstract class Enemy : GameEntity
{
    protected Enemy() : base()
    {
    }

    protected Enemy(Vector2 position, AnimatedSprite sprite, bool debugMode, Vector2 scale)
        : base(position, sprite, debugMode, scale)
    {
    }

    // Enemy-specific methods can be added here in the future
    // For example: AI behavior, patrol patterns, etc.
}