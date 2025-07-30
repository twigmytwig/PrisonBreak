using LiteNetLib.Utils;
using PrisonBreak.Multiplayer.Core;

namespace PrisonBreak.Multiplayer.Messages;

// Base interface for all network messages using LiteNetLib serialization
public interface INetworkMessage : INetSerializable
{
    NetworkConfig.MessageType Type { get; }
    int EntityId { get; }
}

// Abstract base class following existing patterns from the project
public abstract class NetworkMessage : INetworkMessage
{
    // Concrete implementation - set once in constructor
    public NetworkConfig.MessageType Type { get; protected set; }
    public int EntityId { get; set; }
    
    protected NetworkMessage(NetworkConfig.MessageType type, int entityId = -1)
    {
        Type = type;
        EntityId = entityId;
    }
    
    // INetSerializable implementation - calls abstract methods for message-specific data
    public void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)Type);
        writer.Put(EntityId);
        SerializeData(writer);  // Concrete classes implement this
    }
    
    public void Deserialize(NetDataReader reader)
    {
        // Type is already read by message router before this is called
        EntityId = reader.GetInt();
        DeserializeData(reader);  // Concrete classes implement this
    }
    
    // Abstract methods for concrete message types to implement
    protected abstract void SerializeData(NetDataWriter writer);
    protected abstract void DeserializeData(NetDataReader reader);
}

// Server welcomes a newly connected client with their assigned player ID
public class WelcomeMessage : NetworkMessage
{
    public int AssignedPlayerId;
    public string WelcomeText;
    
    // Default constructor for deserialization
    public WelcomeMessage() : base(NetworkConfig.MessageType.Welcome) { }
    
    // Constructor with data
    public WelcomeMessage(int assignedPlayerId, string welcomeText = "Welcome to the lobby!") 
        : base(NetworkConfig.MessageType.Welcome)
    {
        AssignedPlayerId = assignedPlayerId;
        WelcomeText = welcomeText;
    }
    
    protected override void SerializeData(NetDataWriter writer)
    {
        writer.Put(AssignedPlayerId);
        writer.Put(WelcomeText);
    }
    
    protected override void DeserializeData(NetDataReader reader)
    {
        AssignedPlayerId = reader.GetInt();
        WelcomeText = reader.GetString();
    }
}

// Special message type for messages that the pure networking library can't deserialize
// NetworkManager will handle the actual deserialization of these messages
public class UnknownNetworkMessage : INetworkMessage
{
    public NetworkConfig.MessageType Type { get; private set; }
    public int EntityId { get; set; }
    public byte[] RawData { get; private set; }
    
    public UnknownNetworkMessage(NetworkConfig.MessageType type, byte[] rawData)
    {
        Type = type;
        EntityId = -1; // Unknown messages don't have entity IDs in this context
        RawData = rawData;
    }
    
    public void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)Type);
        writer.Put(EntityId);
        writer.Put(RawData.Length);
        writer.Put(RawData);
    }
    
    public void Deserialize(NetDataReader reader)
    {
        // Type is already read by message router
        EntityId = reader.GetInt();
        int dataLength = reader.GetInt();
        RawData = reader.GetBytesWithLength();
    }
}