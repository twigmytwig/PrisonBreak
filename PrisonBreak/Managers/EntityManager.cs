using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Player;
using MonoGameLibrary.Enemy;
using PrisonBreak.Config;
using PrisonBreak.Initializer;

namespace PrisonBreak.Managers;

public class EntityManager
{
    private Player _player;
    private Cop _cop;
    private TextureAtlas _atlas;

    public Player Player => _player;
    public Cop Cop => _cop;

    public void Initialize(ContentManager content)
    {
        _atlas = TextureAtlas.FromFile(content, EntityConfig.TextureAtlas.ConfigFile);
    }

    public void CreatePlayer(Vector2 position)
    {
        if (_atlas == null)
        {
            throw new InvalidOperationException("EntityManager must be initialized before creating entities");
        }
        
        _player = InitializeGameObjects.InitPlayer(
            EntityConfig.Player.DebugMode,
            position,
            _atlas,
            EntityConfig.Player.AnimationName,
            EntityConfig.Player.Scale
        );
    }

    public void CreateCop(Vector2 position)
    {
        if (_atlas == null)
        {
            throw new InvalidOperationException("EntityManager must be initialized before creating entities");
        }
        
        AnimatedSprite copSprite = _atlas.CreateAnimatedSprite(EntityConfig.Cop.AnimationName);
        _cop = new Cop(
            position,
            copSprite,
            EntityConfig.Cop.DebugMode,
            EntityConfig.Cop.Scale
        );
    }

    public void DestroyPlayer()
    {
        _player = null;
    }

    public void DestroyCop()
    {
        _cop = null;
    }

    public void DestroyAll()
    {
        _player = null;
        _cop = null;
    }
}