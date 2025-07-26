using System;
using Microsoft.Xna.Framework;
using PrisonBreak.Core.Graphics;
using PrisonBreak.Core.Physics;

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

// Animation control
public struct AnimationComponent
{
    public AnimatedSprite AnimatedSprite;
    public bool IsPlaying;
    public bool Loop;
    public double ElapsedTime;

    public AnimationComponent(AnimatedSprite animatedSprite)
    {
        AnimatedSprite = animatedSprite;
        IsPlaying = true;
        Loop = true;
        ElapsedTime = 0.0;
    }
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

public struct WallTag
{
    public string WallType;

    public WallTag(string wallType = "wall")
    {
        WallType = wallType;
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

public struct PlayerTypeComponent
{
    public PlayerType Type;
    public float SpeedMultiplier;    // Cops might be faster
    public string AnimationName;     // Animation to use for this player type
    public int InventorySlots => Type switch
    {
        PlayerType.Prisoner => 3,
        PlayerType.Cop => 4,
        _ => 3
    };

    public PlayerTypeComponent(PlayerType type, string animationName = null)
    {
        Type = type;
        SpeedMultiplier = type == PlayerType.Cop ? 1.2f : 1.0f;
        AnimationName = animationName ?? GetDefaultAnimation(type);

    }

    private static string GetDefaultAnimation(PlayerType type)
    {
        return type switch
        {
            PlayerType.Cop => "cop-animation",
            PlayerType.Prisoner => "prisoner-animation",
            _ => "prisoner-animation"
        };
    }
}

public enum PlayerType
{
    Prisoner,
    Cop,
}

public struct InventoryComponent
{
    public int MaxSlots;
    public Entity[] Items;
    public int ItemCount;
    
    public InventoryComponent(int maxSlots)
    {
        MaxSlots = maxSlots;
        Items = new Entity[maxSlots];
        ItemCount = 0;
        
        // Items array is already initialized with null values by default
    }
}

public struct ItemComponent
{
    public string ItemName;
    public string ItemType; // TODO: this should be not a string probably
    public bool IsStackable;
    public int StackSize;
}

public struct InventorySlotUIComponent
{
    public int PlayerId;           // Unique per player (1, 2, 3, etc.)
    public int SlotIndex;          // Slot position (0, 1, 2, etc.)
    public Entity ContainedItem;   // Current item in slot (null if empty)
    public bool IsVisible;         // Show/hide this slot
    public bool IsLocalPlayer;     // For UI positioning differences

    public InventorySlotUIComponent(int playerId, int slotIndex, bool isLocalPlayer = true)
    {
        PlayerId = playerId;
        SlotIndex = slotIndex;
        ContainedItem = null;
        IsVisible = true;
        IsLocalPlayer = isLocalPlayer;
    }
}

public struct InteractableComponent
{
    public string InteractionType;    // "chest", "pickup", "door"
    public float InteractionRange;    // Distance for interaction
    public bool IsActive;            // Can be interacted with
    public string DisplayText;       // "Press E to open chest"

    public InteractableComponent(string interactionType, float interactionRange = 64f, string displayText = null)
    {
        InteractionType = interactionType;
        InteractionRange = interactionRange;
        IsActive = true;
        DisplayText = displayText ?? $"Press E to {interactionType}";
    }
}

public struct ContainerComponent
{
    public int MaxItems;
    public Entity[] ContainedItems;
    public int ItemCount;
    public string ContainerType;     // "chest", "locker", "crate"

    public ContainerComponent(int maxItems, string containerType = "chest")
    {
        MaxItems = maxItems;
        ContainedItems = new Entity[maxItems];
        ItemCount = 0;
        ContainerType = containerType;
    }
}

// Network Components

/// <summary>
/// Marks an entity for network synchronization
/// Contains network ID and sync timing information
/// </summary>
public struct NetworkComponent
{
    public int NetworkId;           // Unique network identifier across all clients
    public bool IsOwned;            // Does this client own/control this entity?
    public float SyncInterval;      // How often to sync (seconds) - e.g., 0.05f = 20 FPS
    public float LastSyncTime;      // When this entity was last synced
    public bool SyncTransform;      // Should position/rotation be synced?
    public bool SyncMovement;       // Should velocity be synced?
    public bool SyncComponents;     // Should other components be synced?

    public NetworkComponent(int networkId, bool isOwned = false, float syncInterval = 0.05f)
    {
        NetworkId = networkId;
        IsOwned = isOwned;
        SyncInterval = syncInterval;
        LastSyncTime = 0f;
        SyncTransform = true;
        SyncMovement = true;
        SyncComponents = false;
    }
}

/// <summary>
/// Defines ownership and authority over an entity
/// Determines who can modify the entity's state
/// </summary>
public struct AuthorityComponent
{
    public int OwnerId;             // Player/client ID that owns this entity (-1 = server/host)
    public bool IsServerOwned;      // Host has authority over this entity
    public bool AllowClientPrediction; // Can clients predict this entity's movement?

    public AuthorityComponent(int ownerId, bool isServerOwned = false, bool allowClientPrediction = false)
    {
        OwnerId = ownerId;
        IsServerOwned = isServerOwned;
        AllowClientPrediction = allowClientPrediction;
    }

    /// <summary>
    /// Create server-owned authority (host controls this entity)
    /// </summary>
    public static AuthorityComponent ServerOwned => new(-1, true, false);

    /// <summary>
    /// Create player-owned authority (specific player controls this entity)
    /// </summary>
    public static AuthorityComponent PlayerOwned(int playerId, bool allowPrediction = true) 
        => new(playerId, false, allowPrediction);
}