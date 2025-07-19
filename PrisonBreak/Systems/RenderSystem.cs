using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using MonoGameLibrary.Player;
using MonoGameLibrary.Enemy;
using MonoGameLibrary.Graphics;

namespace PrisonBreak.Systems;

public class RenderSystem : IGameSystem
{
    private Player _player;
    private Cop _cop;
    private Tilemap _tilemap;
    private Texture2D _debugTexture;
    private Texture2D _debugCopTexture;

    public void Initialize()
    {
        CreateDebugTextures();
    }

    public void SetEntities(Player player, Cop cop)
    {
        _player = player;
        _cop = cop;
    }

    public void SetTilemap(Tilemap tilemap)
    {
        _tilemap = tilemap;
    }

    public void Update(GameTime gameTime)
    {
        if (_player != null)
        {
            _player.Update(gameTime);
        }

        if (_cop != null)
        {
            _cop.Update(gameTime);
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (_tilemap != null)
        {
            _tilemap.Draw(spriteBatch);
        }

        if (_player != null)
        {
            _player.Draw(spriteBatch);
        }

        if (_cop != null)
        {
            _cop.Draw(spriteBatch);
        }

        DrawDebugInfo(spriteBatch);
    }

    public void Shutdown()
    {
        _debugTexture?.Dispose();
        _debugCopTexture?.Dispose();
    }

    private void CreateDebugTextures()
    {
        if (Core.GraphicsDevice != null)
        {
            _debugTexture = new Texture2D(Core.GraphicsDevice, 1, 1);
            _debugTexture.SetData(new[] { Color.White });

            _debugCopTexture = new Texture2D(Core.GraphicsDevice, 1, 1);
            _debugCopTexture.SetData(new[] { Color.White });
        }
    }

    private void DrawDebugInfo(SpriteBatch spriteBatch)
    {
        if (_player?.DebugMode == true && _debugTexture != null && _debugCopTexture != null)
        {
            _player.Collider.Draw(spriteBatch, Color.Red, _debugTexture, 2);
            
            if (_cop != null)
            {
                _cop.Collider.Draw(spriteBatch, Color.Blue, _debugCopTexture, 2);
            }
        }
    }
}