using System;
using Microsoft.Xna.Framework;
using PrisonBreak.ECS;

namespace PrisonBreak.Network;

// Base message type for all network communications
[Serializable]
public abstract class NetworkMessage
{
    public long Timestamp { get; set; } = DateTime.UtcNow.Ticks;
    public abstract string MessageType { get; }
}

// Message Types for different network operations

[Serializable]
public class PlayerJoinMessage : NetworkMessage
{
    public override string MessageType => "PlayerJoin";
    public int PlayerId { get; set; }
    public string PlayerName { get; set; }
    public PlayerType SelectedType { get; set; }
}

[Serializable]
public class PlayerLeaveMessage : NetworkMessage
{
    public override string MessageType => "PlayerLeave";
    public int PlayerId { get; set; }
    public string Reason { get; set; }
}

[Serializable]
public class PlayerCharacterUpdateMessage : NetworkMessage
{
    public override string MessageType => "PlayerCharacterUpdate";
    public int PlayerId { get; set; }
    public PlayerType SelectedType { get; set; }
}

[Serializable]
public class EntityStateMessage : NetworkMessage
{
    public override string MessageType => "EntityState";
    public int EntityId { get; set; }
    public int NetworkId { get; set; }
    
    // Transform data
    public Vector2 Position { get; set; }
    public float Rotation { get; set; }
    public Vector2 Scale { get; set; }
    
    // Movement data (optional)
    public Vector2? Velocity { get; set; }
    public float? MaxSpeed { get; set; }
    
    // Player type data (optional)
    public PlayerType? PlayerType { get; set; }
    public string AnimationName { get; set; }
}

[Serializable]
public class PlayerInputMessage : NetworkMessage
{
    public override string MessageType => "PlayerInput";
    public int PlayerId { get; set; }
    public Vector2 MovementInput { get; set; }
    public bool ActionPressed { get; set; }
    public bool InteractionPressed { get; set; }
}

[Serializable]
public class InventoryActionMessage : NetworkMessage
{
    public override string MessageType => "InventoryAction";
    public int PlayerId { get; set; }
    public InventoryActionType Action { get; set; }
    public int ItemId { get; set; }
    public int SlotIndex { get; set; }
    public int? ContainerId { get; set; } // For chest/container interactions
}

[Serializable]
public class GameStateMessage : NetworkMessage
{
    public override string MessageType => "GameState";
    public GameStateType State { get; set; }
    public string AdditionalData { get; set; }
}

[Serializable]
public class LobbyStateMessage : NetworkMessage
{
    public override string MessageType => "LobbyState";
    public LobbyPlayer[] ConnectedPlayers { get; set; }
    public bool CanStart { get; set; }
}

[Serializable]
public class ConnectionRequestMessage : NetworkMessage
{
    public override string MessageType => "ConnectionRequest";
    public string PlayerName { get; set; }
    public string GameVersion { get; set; } = "0.1.0";
}

[Serializable]
public class ConnectionResponseMessage : NetworkMessage
{
    public override string MessageType => "ConnectionResponse";
    public bool Accepted { get; set; }
    public string Reason { get; set; }
    public int AssignedPlayerId { get; set; }
}

// Supporting enums and data structures

public enum InventoryActionType
{
    Pickup,
    Drop,
    Transfer,
    Use
}

public enum GameStateType
{
    Lobby,
    Starting,
    InProgress,
    Paused,
    Ended
}

[Serializable]
public struct LobbyPlayer
{
    public int PlayerId { get; set; }
    public string Name { get; set; }
    public PlayerType SelectedType { get; set; }
    public bool IsReady { get; set; }
    public bool IsHost { get; set; }
}

// Network event data for integration with game EventBus
public class NetworkPlayerJoinEvent
{
    public int PlayerId { get; }
    public string PlayerName { get; }
    public PlayerType SelectedType { get; }

    public NetworkPlayerJoinEvent(int playerId, string playerName, PlayerType selectedType)
    {
        PlayerId = playerId;
        PlayerName = playerName;
        SelectedType = selectedType;
    }
}

public class NetworkPlayerLeaveEvent
{
    public int PlayerId { get; }
    public string Reason { get; }

    public NetworkPlayerLeaveEvent(int playerId, string reason)
    {
        PlayerId = playerId;
        Reason = reason;
    }
}

public class NetworkEntityUpdateEvent
{
    public EntityStateMessage StateData { get; }

    public NetworkEntityUpdateEvent(EntityStateMessage stateData)
    {
        StateData = stateData;
    }
}

public class NetworkInventoryActionEvent
{
    public InventoryActionMessage ActionData { get; }

    public NetworkInventoryActionEvent(InventoryActionMessage actionData)
    {
        ActionData = actionData;
    }
}

public class NetworkConnectionEvent
{
    public bool IsConnected { get; }
    public string Host { get; }
    public int Port { get; }
    public string Message { get; }

    public NetworkConnectionEvent(bool isConnected, string host, int port, string message)
    {
        IsConnected = isConnected;
        Host = host;
        Port = port;
        Message = message;
    }
}