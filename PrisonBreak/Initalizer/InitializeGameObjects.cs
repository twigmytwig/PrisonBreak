using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using MonoGameLibrary.GameObjectFactory;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.RectangleCollider;
using MonoGameLibrary.Player;

namespace PrisonBreak.Initializer;

public static class InitializeGameObjects
{

    public static GameObjectFactory getInitObjects(TextureAtlas atlas)
    {
        RectangleCollider _prisonerCollider = new();
        AnimatedSprite _prisoner = new AnimatedSprite();
        Vector2 _prisonerPosition = new();
        _prisoner = atlas.CreateAnimatedSprite("prisoner-animation");
        _prisoner.Scale = new Vector2(4.0f, 4.0f);

        int spriteWidth = (int)(_prisoner.Width);
        int spriteHeight = (int)(_prisoner.Height);
        int collisionWidth = (int)(spriteWidth * 0.5f);
        int collisionHeight = (int)(spriteHeight * 1f);
        _prisonerCollider = new RectangleCollider(
            (int)(_prisonerPosition.X + (spriteWidth - collisionWidth) / 2),
            (int)(_prisonerPosition.Y + (spriteHeight - collisionHeight) / 2),
            collisionWidth,
            collisionHeight,
            true);
        return new GameObjectFactory()
        {

        };
    }

    public static Player InitPlayer(bool isDebug, Vector2 pos, TextureAtlas atlas, string animationName, Vector2 scale)
    {
        AnimatedSprite playerSprite = atlas.CreateAnimatedSprite(animationName);
        var _collider = new RectangleCollider(
                (int)(pos.X + (playerSprite.Width - playerSprite.Width * 0.5f) / 2),
                (int)(pos.Y + (playerSprite.Height - playerSprite.Height) / 2),
                (int)(playerSprite.Width * 0.5f),
                (int)(playerSprite.Height * 1f),
                isDebug);
        Player player = new Player(pos, playerSprite, _collider, isDebug, scale);
        return player;
    }
}