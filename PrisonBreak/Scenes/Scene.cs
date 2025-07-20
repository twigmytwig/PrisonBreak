using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using PrisonBreak.Managers;
using PrisonBreak.ECS;

namespace PrisonBreak.Scenes;

/// <summary>
/// Base class for all game scenes (Menu, Gameplay, Pause, etc.)
/// Each scene manages its own systems, entities, and resources
/// </summary>
public abstract class Scene : IDisposable
{
    protected SystemManager SystemManager { get; private set; }
    protected ComponentEntityManager EntityManager { get; private set; }
    protected EventBus EventBus { get; private set; }
    protected ContentManager Content { get; private set; }
    
    public string Name { get; protected set; }
    public bool IsInitialized { get; private set; }
    public bool IsContentLoaded { get; private set; }
    
    protected Scene(string name, EventBus eventBus)
    {
        Name = name;
        EventBus = eventBus;
        SystemManager = new SystemManager();
        EntityManager = new ComponentEntityManager(eventBus);
        IsInitialized = false;
        IsContentLoaded = false;
    }
    
    /// <summary>
    /// Initialize the scene - set up systems and initial state
    /// </summary>
    public virtual void Initialize()
    {
        if (IsInitialized) return;
        
        SetupSystems();
        SystemManager.Initialize();
        IsInitialized = true;
    }
    
    /// <summary>
    /// Load content and assets for this scene
    /// </summary>
    public virtual void LoadContent(ContentManager content)
    {
        if (IsContentLoaded) return;
        
        Content = content;
        EntityManager.Initialize(content);
        LoadSceneContent();
        IsContentLoaded = true;
    }
    
    /// <summary>
    /// Update the scene logic
    /// </summary>
    public virtual void Update(GameTime gameTime)
    {
        if (!IsInitialized || !IsContentLoaded) return;
        
        SystemManager.Update(gameTime);
    }
    
    /// <summary>
    /// Draw the scene
    /// </summary>
    public virtual void Draw(SpriteBatch spriteBatch)
    {
        if (!IsInitialized || !IsContentLoaded) return;
        
        SystemManager.Draw(spriteBatch);
    }
    
    /// <summary>
    /// Called when entering this scene
    /// </summary>
    public virtual void OnEnter()
    {
        // Override in derived classes for scene entry logic
    }
    
    /// <summary>
    /// Called when leaving this scene
    /// </summary>
    public virtual void OnExit()
    {
        // Override in derived classes for scene exit logic
    }
    
    /// <summary>
    /// Unload scene content and resources
    /// </summary>
    public virtual void UnloadContent()
    {
        if (!IsContentLoaded) return;
        
        SystemManager.Shutdown();
        EntityManager.Clear();
        Content = null;
        IsContentLoaded = false;
    }
    
    /// <summary>
    /// Set up the systems for this scene - override in derived classes
    /// </summary>
    protected abstract void SetupSystems();
    
    /// <summary>
    /// Load scene-specific content - override in derived classes
    /// </summary>
    protected virtual void LoadSceneContent()
    {
        // Default implementation - override if needed
    }
    
    public virtual void Dispose()
    {
        UnloadContent();
        SystemManager = null;
        EntityManager = null;
        EventBus = null;
        IsInitialized = false;
    }
}