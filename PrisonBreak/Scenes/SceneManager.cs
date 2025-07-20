using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using PrisonBreak.ECS;

namespace PrisonBreak.Scenes;

/// <summary>
/// Manages scene transitions and lifecycle
/// </summary>
public class SceneManager : IDisposable
{
    private readonly Dictionary<SceneType, Scene> _scenes;
    private readonly EventBus _eventBus;
    private Scene _currentScene;
    private Scene _nextScene;
    private bool _transitionInProgress;
    private ContentManager _content;
    
    public Scene CurrentScene => _currentScene;
    public SceneType CurrentSceneType { get; private set; }
    
    public SceneManager(EventBus eventBus)
    {
        _eventBus = eventBus;
        _scenes = new Dictionary<SceneType, Scene>();
        _transitionInProgress = false;
        
        // Subscribe to scene transition events
        _eventBus.Subscribe<SceneTransitionEvent>(OnSceneTransitionRequested);
    }
    
    /// <summary>
    /// Register a scene with the manager
    /// </summary>
    public void RegisterScene(SceneType sceneType, Scene scene)
    {
        if (_scenes.ContainsKey(sceneType))
        {
            Console.WriteLine($"Warning: Scene {sceneType} is already registered. Replacing existing scene.");
            _scenes[sceneType]?.Dispose();
        }
        
        _scenes[sceneType] = scene;
        Console.WriteLine($"Registered scene: {sceneType} ({scene.Name})");
    }
    
    /// <summary>
    /// Initialize the scene manager and set the initial scene
    /// </summary>
    public void Initialize(SceneType initialSceneType)
    {
        if (!_scenes.ContainsKey(initialSceneType))
        {
            throw new InvalidOperationException($"Scene {initialSceneType} is not registered");
        }
        
        _currentScene = _scenes[initialSceneType];
        CurrentSceneType = initialSceneType;
        _currentScene.Initialize();
        _currentScene.OnEnter();
        
        Console.WriteLine($"SceneManager initialized with scene: {initialSceneType}");
    }
    
    /// <summary>
    /// Load content for the current scene
    /// </summary>
    public void LoadContent(ContentManager content)
    {
        _content = content;
        _currentScene?.LoadContent(content);
    }
    
    /// <summary>
    /// Update the current scene and handle transitions
    /// </summary>
    public void Update(GameTime gameTime)
    {
        // Handle pending scene transition
        if (_transitionInProgress && _nextScene != null)
        {
            CompleteTransition();
        }
        
        _currentScene?.Update(gameTime);
    }
    
    /// <summary>
    /// Draw the current scene
    /// </summary>
    public void Draw(SpriteBatch spriteBatch)
    {
        _currentScene?.Draw(spriteBatch);
    }
    
    /// <summary>
    /// Request a scene transition (usually called via event)
    /// </summary>
    public void TransitionToScene(SceneType targetSceneType, object transitionData = null)
    {
        if (!_scenes.ContainsKey(targetSceneType))
        {
            Console.WriteLine($"Error: Scene {targetSceneType} is not registered");
            return;
        }
        
        if (_transitionInProgress)
        {
            Console.WriteLine($"Warning: Transition already in progress. Ignoring transition to {targetSceneType}");
            return;
        }
        
        if (CurrentSceneType == targetSceneType)
        {
            Console.WriteLine($"Warning: Already in scene {targetSceneType}. Ignoring transition.");
            return;
        }
        
        Console.WriteLine($"Starting transition: {CurrentSceneType} -> {targetSceneType}");
        
        _nextScene = _scenes[targetSceneType];
        _transitionInProgress = true;
        
        // Initialize and load content for next scene if needed
        if (!_nextScene.IsInitialized)
        {
            _nextScene.Initialize();
        }
        
        // Store transition data for the next scene to use
        if (transitionData != null && _nextScene is ITransitionDataReceiver receiver)
        {
            receiver.ReceiveTransitionData(transitionData);
        }
    }
    
    /// <summary>
    /// Complete the scene transition
    /// </summary>
    private void CompleteTransition()
    {
        if (_nextScene == null) return;
        
        var previousSceneType = CurrentSceneType;
        
        // Exit current scene
        _currentScene?.OnExit();
        
        // Switch to next scene
        _currentScene = _nextScene;
        CurrentSceneType = _scenes.First(kvp => kvp.Value == _nextScene).Key;
        
        // Load content for new scene if we have a content manager
        if (_content != null && !_currentScene.IsContentLoaded)
        {
            _currentScene.LoadContent(_content);
        }
        
        // Enter new scene
        _currentScene.OnEnter();
        
        // Reset transition state
        _nextScene = null;
        _transitionInProgress = false;
        
        Console.WriteLine($"Scene transition completed: {previousSceneType} -> {CurrentSceneType}");
    }
    
    /// <summary>
    /// Handle scene transition events from the event bus
    /// </summary>
    private void OnSceneTransitionRequested(SceneTransitionEvent transitionEvent)
    {
        TransitionToScene(transitionEvent.ToScene, transitionEvent.TransitionData);
    }
    
    /// <summary>
    /// Get a specific scene (useful for initialization)
    /// </summary>
    public T GetScene<T>(SceneType sceneType) where T : Scene
    {
        if (_scenes.TryGetValue(sceneType, out var scene))
        {
            return scene as T;
        }
        return null;
    }
    
    public void Dispose()
    {
        _eventBus?.Unsubscribe<SceneTransitionEvent>(OnSceneTransitionRequested);
        
        foreach (var scene in _scenes.Values)
        {
            scene?.Dispose();
        }
        
        _scenes.Clear();
        _currentScene = null;
        _nextScene = null;
    }
}

/// <summary>
/// Interface for scenes that can receive transition data
/// </summary>
public interface ITransitionDataReceiver
{
    void ReceiveTransitionData(object data);
}