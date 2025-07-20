using Microsoft.Xna.Framework;
using PrisonBreak.ECS;

namespace PrisonBreak.Scenes;

/// <summary>
/// Available scene types in the game
/// </summary>
public enum SceneType
{
    StartMenu,
    Gameplay,
    PauseMenu,
    GameOver,
    Settings
}

/// <summary>
/// Event for requesting scene transitions
/// </summary>
public struct SceneTransitionEvent
{
    public SceneType FromScene;
    public SceneType ToScene;
    public object TransitionData; // Optional data to pass between scenes
    
    public SceneTransitionEvent(SceneType from, SceneType to, object data = null)
    {
        FromScene = from;
        ToScene = to;
        TransitionData = data;
    }
}

/// <summary>
/// Data passed from StartMenu to Gameplay scene
/// </summary>
public struct GameStartData
{
    public PlayerType PlayerType;
    public PlayerIndex PlayerIndex;
    public string CustomAnimationName; // For future skin system
    
    public GameStartData(PlayerType playerType, PlayerIndex playerIndex, string customAnimation = null)
    {
        PlayerType = playerType;
        PlayerIndex = playerIndex;
        CustomAnimationName = customAnimation;
    }
}