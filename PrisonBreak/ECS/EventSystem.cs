using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace PrisonBreak.ECS;

public class EventBus
{
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();
    
    public void Subscribe<T>(Action<T> handler)
    {
        var eventType = typeof(T);
        if (!_handlers.ContainsKey(eventType))
            _handlers[eventType] = new List<Delegate>();
            
        _handlers[eventType].Add(handler);
    }
    
    public void Unsubscribe<T>(Action<T> handler)
    {
        var eventType = typeof(T);
        if (_handlers.TryGetValue(eventType, out var handlers))
        {
            handlers.Remove(handler);
        }
    }
    
    public void Send<T>(T eventData)
    {
        var eventType = typeof(T);
        if (_handlers.TryGetValue(eventType, out var handlers))
        {
            foreach (var handler in handlers)
            {
                try
                {
                    ((Action<T>)handler)(eventData);
                }
                catch (Exception ex)
                {
                    // Log error but continue processing other handlers
                    Console.WriteLine($"Error handling event {eventType.Name}: {ex.Message}");
                }
            }
        }
    }
    
    public void Clear()
    {
        _handlers.Clear();
    }
}

// Game Events
public struct PlayerInputEvent
{
    public int EntityId;
    public Vector2 MovementDirection;
    public bool SpeedBoost;
    
    public PlayerInputEvent(int entityId, Vector2 direction, bool speedBoost)
    {
        EntityId = entityId;
        MovementDirection = direction;
        SpeedBoost = speedBoost;
    }
}

public struct EntityCollisionEvent
{
    public int EntityAId;
    public int EntityBId;
    public Vector2 CollisionPoint;
    public Vector2 CollisionNormal;
    
    public EntityCollisionEvent(int entityA, int entityB, Vector2 point, Vector2 normal)
    {
        EntityAId = entityA;
        EntityBId = entityB;
        CollisionPoint = point;
        CollisionNormal = normal;
    }
}

public struct PlayerCopCollisionEvent
{
    public int PlayerId;
    public int CopId;
    public Vector2 CollisionPosition;
    
    public PlayerCopCollisionEvent(int playerId, int copId, Vector2 position)
    {
        PlayerId = playerId;
        CopId = copId;
        CollisionPosition = position;
    }
}

public struct BoundaryCollisionEvent
{
    public int EntityId;
    public Vector2 CollisionPoint;
    public Vector2 CollisionNormal;
    public Rectangle Boundary;
    
    public BoundaryCollisionEvent(int entityId, Vector2 point, Vector2 normal, Rectangle boundary)
    {
        EntityId = entityId;
        CollisionPoint = point;
        CollisionNormal = normal;
        Boundary = boundary;
    }
}

public struct EntitySpawnEvent
{
    public int EntityId;
    public Vector2 Position;
    public string EntityType;
    
    public EntitySpawnEvent(int entityId, Vector2 position, string entityType)
    {
        EntityId = entityId;
        Position = position;
        EntityType = entityType;
    }
}

public struct EntityDestroyEvent
{
    public int EntityId;
    public Vector2 Position;
    public string Reason;
    
    public EntityDestroyEvent(int entityId, Vector2 position, string reason)
    {
        EntityId = entityId;
        Position = position;
        Reason = reason;
    }
}

public struct TeleportEvent
{
    public int EntityId;
    public Vector2 FromPosition;
    public Vector2 ToPosition;
    
    public TeleportEvent(int entityId, Vector2 from, Vector2 to)
    {
        EntityId = entityId;
        FromPosition = from;
        ToPosition = to;
    }
}

public struct ItemAddedEvent
{
    public int PlayerId;
    public Entity ItemEntity;
    public int SlotIndex;
    
    public ItemAddedEvent(int playerId, Entity itemEntity, int slotIndex)
    {
        PlayerId = playerId;
        ItemEntity = itemEntity;
        SlotIndex = slotIndex;
    }
}

public struct ItemRemovedEvent
{
    public int PlayerId;
    public Entity ItemEntity;
    public int SlotIndex;
    
    public ItemRemovedEvent(int playerId, Entity itemEntity, int slotIndex)
    {
        PlayerId = playerId;
        ItemEntity = itemEntity;
        SlotIndex = slotIndex;
    }
}

public struct InventoryFullEvent
{
    public int PlayerId;
    public Entity AttemptedItem;
    
    public InventoryFullEvent(int playerId, Entity attemptedItem)
    {
        PlayerId = playerId;
        AttemptedItem = attemptedItem;
    }
}

public struct InteractionInputEvent
{
    public int EntityId;
    
    public InteractionInputEvent(int entityId)
    {
        EntityId = entityId;
    }
}

public struct InteractionEvent
{
    public int PlayerId;
    public Entity InteractableEntity;
    public string InteractionType;
    
    public InteractionEvent(int playerId, Entity interactableEntity, string interactionType)
    {
        PlayerId = playerId;
        InteractableEntity = interactableEntity;
        InteractionType = interactionType;
    }
}