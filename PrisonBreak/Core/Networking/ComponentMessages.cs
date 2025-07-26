using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using PrisonBreak.ECS;
using PrisonBreak.Multiplayer.Core;
using PrisonBreak.Multiplayer.Messages;

namespace PrisonBreak.Core.Networking;

// Transform network message wraps existing TransformComponent
public class TransformMessage : NetworkMessage
{
    public Vector2 Position;
    public float Rotation;
    public Vector2 Scale;
    
    // Default constructor for deserialization
    public TransformMessage() : base(NetworkConfig.MessageType.Transform) { }
    
    // Constructor with data
    public TransformMessage(int entityId, TransformComponent transform) 
        : base(NetworkConfig.MessageType.Transform, entityId)
    {
        Position = transform.Position;
        Rotation = transform.Rotation;
        Scale = transform.Scale;
    }
    
    protected override void SerializeData(NetDataWriter writer)
    {
        writer.Put(Position.X);
        writer.Put(Position.Y);
        writer.Put(Rotation);
        writer.Put(Scale.X);
        writer.Put(Scale.Y);
    }
    
    protected override void DeserializeData(NetDataReader reader)
    {
        Position = new Vector2(reader.GetFloat(), reader.GetFloat());
        Rotation = reader.GetFloat();
        Scale = new Vector2(reader.GetFloat(), reader.GetFloat());
    }
    
    // Helper to convert back to component
    public TransformComponent ToComponent()
    {
        return new TransformComponent(Position)
        {
            Rotation = Rotation,
            Scale = Scale
        };
    }
}

// Movement network message wraps existing MovementComponent  
public class MovementMessage : NetworkMessage
{
    public Vector2 Velocity;
    public float MaxSpeed;
    public float Acceleration;
    public float Friction;
    
    // Default constructor for deserialization
    public MovementMessage() : base(NetworkConfig.MessageType.Movement) { }
    
    // Constructor with data
    public MovementMessage(int entityId, MovementComponent movement)
        : base(NetworkConfig.MessageType.Movement, entityId)
    {
        Velocity = movement.Velocity;
        MaxSpeed = movement.MaxSpeed;
        Acceleration = movement.Acceleration;
        Friction = movement.Friction;
    }
    
    protected override void SerializeData(NetDataWriter writer)
    {
        writer.Put(Velocity.X);
        writer.Put(Velocity.Y);
        writer.Put(MaxSpeed);
        writer.Put(Acceleration);
        writer.Put(Friction);
    }
    
    protected override void DeserializeData(NetDataReader reader)
    {
        Velocity = new Vector2(reader.GetFloat(), reader.GetFloat());
        MaxSpeed = reader.GetFloat();
        Acceleration = reader.GetFloat();
        Friction = reader.GetFloat();
    }
    
    // Helper to convert back to component
    public MovementComponent ToComponent()
    {
        return new MovementComponent(MaxSpeed)
        {
            Velocity = Velocity,
            Acceleration = Acceleration,
            Friction = Friction
        };
    }
}

// Player input network message wraps existing PlayerInputEvent
public class PlayerInputMessage : NetworkMessage
{
    public Vector2 MovementDirection;
    public bool SpeedBoost;
    
    // Default constructor for deserialization
    public PlayerInputMessage() : base(NetworkConfig.MessageType.PlayerInput) { }
    
    // Constructor with data from event
    public PlayerInputMessage(PlayerInputEvent inputEvent)
        : base(NetworkConfig.MessageType.PlayerInput, inputEvent.EntityId)
    {
        MovementDirection = inputEvent.MovementDirection;
        SpeedBoost = inputEvent.SpeedBoost;
    }
    
    protected override void SerializeData(NetDataWriter writer)
    {
        writer.Put(MovementDirection.X);
        writer.Put(MovementDirection.Y);
        writer.Put(SpeedBoost);
    }
    
    protected override void DeserializeData(NetDataReader reader)
    {
        MovementDirection = new Vector2(reader.GetFloat(), reader.GetFloat());
        SpeedBoost = reader.GetBool();
    }
    
    // Helper to convert back to event
    public PlayerInputEvent ToEvent()
    {
        return new PlayerInputEvent(EntityId, MovementDirection, SpeedBoost);
    }
}