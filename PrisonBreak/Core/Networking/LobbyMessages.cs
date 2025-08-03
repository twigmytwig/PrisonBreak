using LiteNetLib.Utils;
using PrisonBreak.ECS;
using PrisonBreak.Multiplayer.Core;
using PrisonBreak.Multiplayer.Messages;

namespace PrisonBreak.Core.Networking;

// Player joins the lobby
public class PlayerJoinLobbyMessage : NetworkMessage
{
    public int PlayerId;
    public string PlayerName;
    
    // Default constructor for deserialization
    public PlayerJoinLobbyMessage() : base(NetworkConfig.MessageType.PlayerJoinLobby) { }
    
    // Constructor with data
    public PlayerJoinLobbyMessage(int playerId, string playerName) 
        : base(NetworkConfig.MessageType.PlayerJoinLobby)
    {
        PlayerId = playerId;
        PlayerName = playerName;
    }
    
    protected override void SerializeData(NetDataWriter writer)
    {
        writer.Put(PlayerId);
        writer.Put(PlayerName);
    }
    
    protected override void DeserializeData(NetDataReader reader)
    {
        PlayerId = reader.GetInt();
        PlayerName = reader.GetString();
    }
}

// Player leaves the lobby
public class PlayerLeaveLobbyMessage : NetworkMessage
{
    public int PlayerId;
    public string Reason;
    
    // Default constructor for deserialization
    public PlayerLeaveLobbyMessage() : base(NetworkConfig.MessageType.PlayerLeaveLobby) { }
    
    // Constructor with data
    public PlayerLeaveLobbyMessage(int playerId, string reason = "Left lobby") 
        : base(NetworkConfig.MessageType.PlayerLeaveLobby)
    {
        PlayerId = playerId;
        Reason = reason;
    }
    
    protected override void SerializeData(NetDataWriter writer)
    {
        writer.Put(PlayerId);
        writer.Put(Reason);
    }
    
    protected override void DeserializeData(NetDataReader reader)
    {
        PlayerId = reader.GetInt();
        Reason = reader.GetString();
    }
}

// Player selects character type (Prisoner/Cop)
public class PlayerCharacterSelectMessage : NetworkMessage
{
    public int PlayerId;
    public PlayerType SelectedPlayerType;
    
    // Default constructor for deserialization
    public PlayerCharacterSelectMessage() : base(NetworkConfig.MessageType.PlayerCharacterSelect) { }
    
    // Constructor with data
    public PlayerCharacterSelectMessage(int playerId, PlayerType selectedPlayerType) 
        : base(NetworkConfig.MessageType.PlayerCharacterSelect)
    {
        PlayerId = playerId;
        SelectedPlayerType = selectedPlayerType;
    }
    
    protected override void SerializeData(NetDataWriter writer)
    {
        writer.Put(PlayerId);
        writer.Put((byte)SelectedPlayerType);
    }
    
    protected override void DeserializeData(NetDataReader reader)
    {
        PlayerId = reader.GetInt();
        SelectedPlayerType = (PlayerType)reader.GetByte();
    }
}

// Player toggles ready state
public class PlayerReadyStateMessage : NetworkMessage
{
    public int PlayerId;
    public bool IsReady;
    public PlayerType SelectedPlayerType;
    
    // Default constructor for deserialization
    public PlayerReadyStateMessage() : base(NetworkConfig.MessageType.PlayerReadyState) { }
    
    // Constructor with data
    public PlayerReadyStateMessage(int playerId, bool isReady, PlayerType selectedPlayerType) 
        : base(NetworkConfig.MessageType.PlayerReadyState)
    {
        PlayerId = playerId;
        IsReady = isReady;
        SelectedPlayerType = selectedPlayerType;
    }
    
    protected override void SerializeData(NetDataWriter writer)
    {
        writer.Put(PlayerId);
        writer.Put(IsReady);
        writer.Put((byte)SelectedPlayerType);
    }
    
    protected override void DeserializeData(NetDataReader reader)
    {
        PlayerId = reader.GetInt();
        IsReady = reader.GetBool();
        SelectedPlayerType = (PlayerType)reader.GetByte();
    }
}

// Full lobby state synchronization (for new joiners)
public class LobbyStateMessage : NetworkMessage
{
    public LobbyPlayerData[] Players;
    
    // Default constructor for deserialization
    public LobbyStateMessage() : base(NetworkConfig.MessageType.LobbyState) { }
    
    // Constructor with data
    public LobbyStateMessage(LobbyPlayerData[] players) 
        : base(NetworkConfig.MessageType.LobbyState)
    {
        Players = players;
    }
    
    protected override void SerializeData(NetDataWriter writer)
    {
        writer.Put(Players.Length);
        foreach (var player in Players)
        {
            writer.Put(player.PlayerId);
            writer.Put(player.PlayerName);
            writer.Put(player.IsReady);
            writer.Put(player.HasSelectedPlayerType);
            if (player.HasSelectedPlayerType)
            {
                writer.Put((byte)player.SelectedPlayerType);
            }
        }
    }
    
    protected override void DeserializeData(NetDataReader reader)
    {
        int playerCount = reader.GetInt();
        Players = new LobbyPlayerData[playerCount];
        
        for (int i = 0; i < playerCount; i++)
        {
            Players[i] = new LobbyPlayerData
            {
                PlayerId = reader.GetInt(),
                PlayerName = reader.GetString(),
                IsReady = reader.GetBool(),
                HasSelectedPlayerType = reader.GetBool()
            };
            
            if (Players[i].HasSelectedPlayerType)
            {
                Players[i].SelectedPlayerType = (PlayerType)reader.GetByte();
            }
        }
    }
}

// Host signals game start
public class GameStartMessage : NetworkMessage
{
    public GameStartPlayerData[] PlayerStartData;
    
    // Default constructor for deserialization
    public GameStartMessage() : base(NetworkConfig.MessageType.GameStart) { }
    
    // Constructor with data
    public GameStartMessage(GameStartPlayerData[] playerStartData) 
        : base(NetworkConfig.MessageType.GameStart)
    {
        PlayerStartData = playerStartData;
    }
    
    protected override void SerializeData(NetDataWriter writer)
    {
        writer.Put(PlayerStartData.Length);
        foreach (var playerData in PlayerStartData)
        {
            writer.Put(playerData.PlayerId);
            writer.Put((byte)playerData.PlayerType);
            writer.Put((byte)playerData.PlayerIndex);
        }
    }
    
    protected override void DeserializeData(NetDataReader reader)
    {
        int playerCount = reader.GetInt();
        PlayerStartData = new GameStartPlayerData[playerCount];
        
        for (int i = 0; i < playerCount; i++)
        {
            PlayerStartData[i] = new GameStartPlayerData
            {
                PlayerId = reader.GetInt(),
                PlayerType = (PlayerType)reader.GetByte(),
                PlayerIndex = (Microsoft.Xna.Framework.PlayerIndex)reader.GetByte()
            };
        }
    }
}

#region Supporting Data Structures

// Data structure for lobby player information
public struct LobbyPlayerData
{
    public int PlayerId;
    public string PlayerName;
    public bool IsReady;
    public bool HasSelectedPlayerType;
    public PlayerType SelectedPlayerType;
}

// Data structure for game start player information
public struct GameStartPlayerData
{
    public int PlayerId;
    public PlayerType PlayerType;
    public Microsoft.Xna.Framework.PlayerIndex PlayerIndex;
}

#endregion