using Microsoft.Xna.Framework;
using PrisonBreak.ECS;

namespace PrisonBreak.Scenes;

/// <summary>
/// Available scene types in the game
/// </summary>
public enum SceneType
{
    StartMenu,
    Lobby,      // NEW: Multiplayer lobby
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

/// <summary>
/// Data passed from StartMenu to Lobby scene
/// </summary>
public struct LobbyStartData
{
    public bool IsHost;              // True if hosting, false if joining
    public string HostIP;            // IP to connect to (if joining)
    public int Port;                 // Port to use
    public string PlayerName;        // Local player name
    public PlayerType InitialPlayerType; // Initial character selection
    
    public LobbyStartData(bool isHost, string playerName, PlayerType initialType, string hostIP = "127.0.0.1", int port = 9050)
    {
        IsHost = isHost;
        HostIP = hostIP;
        Port = port;
        PlayerName = playerName;
        InitialPlayerType = initialType;
    }
}

/// <summary>
/// Data passed from Lobby to Gameplay scene
/// </summary>
public class MultiplayerGameStartData
{
    public PlayerType PlayerType { get; set; }
    public PlayerIndex PlayerIndex { get; set; }
    public bool IsMultiplayer { get; set; }       // True for multiplayer, false for single player
    public bool IsHost { get; set; }              // True if this client is the host
    public int LocalPlayerId { get; set; }        // Network player ID
    public object NetworkManager { get; set; }    // NetworkManager instance to persist
    public object NetworkEventBridge { get; set; } // NetworkEventBridge instance to persist
    
    public MultiplayerGameStartData(PlayerType playerType, PlayerIndex playerIndex, bool isMultiplayer, bool isHost, int localPlayerId)
    {
        PlayerType = playerType;
        PlayerIndex = playerIndex;
        IsMultiplayer = isMultiplayer;
        IsHost = isHost;
        LocalPlayerId = localPlayerId;
    }
}