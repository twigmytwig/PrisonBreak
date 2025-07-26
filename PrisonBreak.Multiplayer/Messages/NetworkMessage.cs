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