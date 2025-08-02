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

// AI state network message wraps existing AIComponent
public class AIStateMessage : NetworkMessage
{
    public int Behavior; // AIBehavior as int for serialization
    public Vector2 PatrolDirection;
    public float StateTimer;
    public Vector2 TargetPosition;
    public int EntityTargetId;
    
    // Default constructor for deserialization
    public AIStateMessage() : base(NetworkConfig.MessageType.AIState) { }
    
    // Constructor with data
    public AIStateMessage(int entityId, AIComponent aiComponent)
        : base(NetworkConfig.MessageType.AIState, entityId)
    {
        Behavior = (int)aiComponent.Behavior;
        PatrolDirection = aiComponent.PatrolDirection;
        StateTimer = aiComponent.StateTimer;
        TargetPosition = aiComponent.TargetPosition;
        EntityTargetId = aiComponent.EntityTargetId;
    }
    
    protected override void SerializeData(NetDataWriter writer)
    {
        writer.Put(Behavior);
        writer.Put(PatrolDirection.X);
        writer.Put(PatrolDirection.Y);
        writer.Put(StateTimer);
        writer.Put(TargetPosition.X);
        writer.Put(TargetPosition.Y);
        writer.Put(EntityTargetId);
    }
    
    protected override void DeserializeData(NetDataReader reader)
    {
        Behavior = reader.GetInt();
        PatrolDirection = new Vector2(reader.GetFloat(), reader.GetFloat());
        StateTimer = reader.GetFloat();
        TargetPosition = new Vector2(reader.GetFloat(), reader.GetFloat());
        EntityTargetId = reader.GetInt();
    }
    
    // Helper to convert back to component
    public AIComponent ToComponent()
    {
        return new AIComponent((AIBehavior)Behavior)
        {
            PatrolDirection = PatrolDirection,
            StateTimer = StateTimer,
            TargetPosition = TargetPosition,
            EntityTargetId = EntityTargetId
        };
    }
}

// Entity spawn network message for creating entities on clients
public class EntitySpawnMessage : NetworkMessage
{
    public string EntityType; // "cop", "player", "item", etc.
    public Vector2 Position;
    public int NetworkEntityId; // Deterministic ID for network sync
    public string AdditionalData; // JSON data for entity-specific properties
    public Rectangle RoomBounds; // Room bounds for physics constraints
    
    // Default constructor for deserialization
    public EntitySpawnMessage() : base(NetworkConfig.MessageType.EntitySpawn) { }
    
    // Constructor for AI cop spawning
    public EntitySpawnMessage(int networkEntityId, string entityType, Vector2 position, Rectangle roomBounds, string additionalData = "")
        : base(NetworkConfig.MessageType.EntitySpawn, networkEntityId)
    {
        NetworkEntityId = networkEntityId;
        EntityType = entityType;
        Position = position;
        RoomBounds = roomBounds;
        AdditionalData = additionalData ?? "";
    }
    
    protected override void SerializeData(NetDataWriter writer)
    {
        writer.Put(NetworkEntityId);
        writer.Put(EntityType);
        writer.Put(Position.X);
        writer.Put(Position.Y);
        writer.Put(RoomBounds.X);
        writer.Put(RoomBounds.Y);
        writer.Put(RoomBounds.Width);
        writer.Put(RoomBounds.Height);
        writer.Put(AdditionalData);
    }
    
    protected override void DeserializeData(NetDataReader reader)
    {
        NetworkEntityId = reader.GetInt();
        EntityType = reader.GetString();
        Position = new Vector2(reader.GetFloat(), reader.GetFloat());
        RoomBounds = new Rectangle(reader.GetInt(), reader.GetInt(), reader.GetInt(), reader.GetInt());
        AdditionalData = reader.GetString();
    }
}

// Player-cop collision network message for authoritative collision handling
public class CollisionMessage : NetworkMessage
{
    public int PlayerId;
    public int CopId;
    public Vector2 CollisionPosition;
    public Vector2 NewCopPosition; // Where the cop teleported to
    public Vector2 NewPatrolDirection; // New AI patrol direction
    
    // Default constructor for deserialization
    public CollisionMessage() : base(NetworkConfig.MessageType.Collision) { }
    
    // Constructor for collision event
    public CollisionMessage(int playerId, int copId, Vector2 collisionPosition, Vector2 newCopPosition, Vector2 newPatrolDirection)
        : base(NetworkConfig.MessageType.Collision, copId)
    {
        PlayerId = playerId;
        CopId = copId;
        CollisionPosition = collisionPosition;
        NewCopPosition = newCopPosition;
        NewPatrolDirection = newPatrolDirection;
    }
    
    protected override void SerializeData(NetDataWriter writer)
    {
        writer.Put(PlayerId);
        writer.Put(CopId);
        writer.Put(CollisionPosition.X);
        writer.Put(CollisionPosition.Y);
        writer.Put(NewCopPosition.X);
        writer.Put(NewCopPosition.Y);
        writer.Put(NewPatrolDirection.X);
        writer.Put(NewPatrolDirection.Y);
    }
    
    protected override void DeserializeData(NetDataReader reader)
    {
        PlayerId = reader.GetInt();
        CopId = reader.GetInt();
        CollisionPosition = new Vector2(reader.GetFloat(), reader.GetFloat());
        NewCopPosition = new Vector2(reader.GetFloat(), reader.GetFloat());
        NewPatrolDirection = new Vector2(reader.GetFloat(), reader.GetFloat());
    }
}