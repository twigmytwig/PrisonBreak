using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PrisonBreak.Systems;

namespace PrisonBreak.Managers;

public class SystemManager
{
    private readonly List<IGameSystem> _systems;
    private bool _initialized;

    public SystemManager()
    {
        _systems = new List<IGameSystem>();
        _initialized = false;
    }

    public void AddSystem(IGameSystem system)
    {
        _systems.Add(system);
        
        if (_initialized)
        {
            system.Initialize();
        }
    }

    public void Initialize()
    {
        foreach (var system in _systems)
        {
            system.Initialize();
        }
        _initialized = true;
    }

    public void Update(GameTime gameTime)
    {
        foreach (var system in _systems)
        {
            system.Update(gameTime);
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        foreach (var system in _systems)
        {
            system.Draw(spriteBatch);
        }
    }

    public void Shutdown()
    {
        foreach (var system in _systems)
        {
            system.Shutdown();
        }
        _systems.Clear();
        _initialized = false;
    }
}