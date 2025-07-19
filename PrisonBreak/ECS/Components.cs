using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.RectangleCollider;

namespace PrisonBreak.ECS;

// Core Transform Component - Position, rotation, scale
public struct TransformComponent
{
    public Vector2 Position;
    public float Rotation;
    public Vector2 Scale;
    
    public TransformComponent(Vector2 position)
    {
        Position = position;
        Rotation = 0f;
        Scale = Vector2.One;
    }
    
    public TransformComponent(Vector2 position, Vector2 scale)
    {
        Position = position;
        Rotation = 0f;
        Scale = scale;
    }
}

// Visual representation
public struct SpriteComponent
{
    public AnimatedSprite Sprite;
    public bool Visible;
    public Color Tint;
    
    public SpriteComponent(AnimatedSprite sprite)
    {
        Sprite = sprite;
        Visible = true;
        Tint = Color.White;
    }
}

// Movement and physics
public struct MovementComponent
{
    public Vector2 Velocity;
    public float MaxSpeed;
    public float Acceleration;
    public float Friction;
    
    public MovementComponent(float maxSpeed)
    {
        Velocity = Vector2.Zero;
        MaxSpeed = maxSpeed;
        Acceleration = 500f;
        Friction = 0.95f;
    }
}

// Collision detection
public struct CollisionComponent
{
    public RectangleCollider Collider;
    public bool IsSolid;
    public string Layer;
    
    public CollisionComponent(RectangleCollider collider)
    {
        Collider = collider;
        IsSolid = true;
        Layer = "Default";
    }
}

// Player input handling
public struct PlayerInputComponent
{
    public PlayerIndex PlayerIndex;
    public float SpeedBoostMultiplier;
    public bool IsActive;
    
    public PlayerInputComponent(PlayerIndex playerIndex)
    {
        PlayerIndex = playerIndex;
        SpeedBoostMultiplier = 1.5f;
        IsActive = true;
    }
}

// AI behavior
public struct AIComponent
{
    public AIBehavior Behavior;
    public Vector2 PatrolDirection;
    public float StateTimer;
    public Vector2 TargetPosition;
    public int EntityTargetId;
    
    public AIComponent(AIBehavior behavior)
    {
        Behavior = behavior;
        PatrolDirection = Vector2.UnitX;
        StateTimer = 0f;
        TargetPosition = Vector2.Zero;
        EntityTargetId = -1;
    }
}

public enum AIBehavior
{
    None,
    Patrol,
    Chase,
    Flee,
    Wander,
    Guard
}

// Debug information
public struct DebugComponent
{
    public bool ShowCollisionBounds;
    public Color CollisionColor;
    public int CollisionThickness;
    
    public DebugComponent(bool showBounds = true)
    {
        ShowCollisionBounds = showBounds;
        CollisionColor = Color.Red;
        CollisionThickness = 2;
    }
}

// Tag components for entity types
public struct PlayerTag
{
    public int PlayerId;
    
    public PlayerTag(int playerId)
    {
        PlayerId = playerId;
    }
}

public struct CopTag
{
    public int CopId;
    
    public CopTag(int copId)
    {
        CopId = copId;
    }
}

// Room bounds constraint
public struct BoundsConstraintComponent
{
    public Rectangle Bounds;
    public bool ConstrainToHorizontal;
    public bool ConstrainToVertical;
    public bool ReflectVelocityOnCollision;
    
    public BoundsConstraintComponent(Rectangle bounds)
    {
        Bounds = bounds;
        ConstrainToHorizontal = true;
        ConstrainToVertical = true;
        ReflectVelocityOnCollision = false;
    }
}