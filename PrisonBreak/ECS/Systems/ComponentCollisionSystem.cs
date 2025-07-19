using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PrisonBreak.Core.Graphics;
using PrisonBreak.Config;
using PrisonBreak.Systems;

namespace PrisonBreak.ECS.Systems;

public class ComponentCollisionSystem : IGameSystem
{
    private ComponentEntityManager _entityManager;
    private EventBus _eventBus;
    private Rectangle _roomBounds;
    private Tilemap _tilemap;
    private Random _random = new Random();
    
    public void SetEntityManager(ComponentEntityManager entityManager)
    {
        _entityManager = entityManager;
    }
    
    public void SetEventBus(EventBus eventBus)
    {
        _eventBus = eventBus;
        
        // Subscribe to events that might affect collision
        _eventBus.Subscribe<PlayerCopCollisionEvent>(OnPlayerCopCollision);
        _eventBus.Subscribe<BoundaryCollisionEvent>(OnBoundaryCollision);
    }
    
    public void SetBounds(Rectangle roomBounds, Tilemap tilemap)
    {
        _roomBounds = roomBounds;
        _tilemap = tilemap;
        
        // Add bounds constraint to all entities that need it
        if (_entityManager != null)
        {
            var boundedEntities = _entityManager.GetEntitiesWith<TransformComponent, CollisionComponent>();
            foreach (var entity in boundedEntities)
            {
                if (!entity.HasComponent<BoundsConstraintComponent>())
                {
                    bool shouldReflect = entity.HasComponent<CopTag>(); // Cops reflect, players clamp
                    _entityManager.AddBoundsConstraint(entity, roomBounds, shouldReflect);
                }
            }
        }
    }
    
    public void Initialize()
    {
    }
    
    public void Update(GameTime gameTime)
    {
        if (_entityManager == null) return;
        
        // Process boundary collisions
        ProcessBoundaryCollisions();
        
        // Process entity-to-entity collisions
        ProcessEntityCollisions();
    }
    
    public void Draw(SpriteBatch spriteBatch)
    {
        // Collision system doesn't draw anything
    }
    
    public void Shutdown()
    {
        if (_eventBus != null)
        {
            _eventBus.Unsubscribe<PlayerCopCollisionEvent>(OnPlayerCopCollision);
            _eventBus.Unsubscribe<BoundaryCollisionEvent>(OnBoundaryCollision);
        }
    }
    
    private void ProcessBoundaryCollisions()
    {
        var boundedEntities = _entityManager.GetEntitiesWith<BoundsConstraintComponent, TransformComponent, CollisionComponent>();
        
        foreach (var entity in boundedEntities)
        {
            ref var constraint = ref entity.GetComponent<BoundsConstraintComponent>();
            ref var transform = ref entity.GetComponent<TransformComponent>();
            ref var collision = ref entity.GetComponent<CollisionComponent>();
            
            Vector2 newPosition = transform.Position;
            Vector2 collisionNormal = Vector2.Zero;
            bool positionChanged = false;
            
            var bounds = collision.Collider.rectangleCollider;
            
            // Check horizontal bounds
            if (constraint.ConstrainToHorizontal)
            {
                if (bounds.Left < constraint.Bounds.Left)
                {
                    if (entity.HasComponent<PlayerTag>())
                    {
                        // Player: clamp position with offset
                        float colliderXOffset = GetColliderXOffset(entity);
                        newPosition.X = constraint.Bounds.Left - colliderXOffset;
                    }
                    else
                    {
                        // Other entities: clamp to boundary
                        newPosition.X = constraint.Bounds.Left;
                    }
                    collisionNormal.X = 1f; // Normal pointing right
                    positionChanged = true;
                }
                else if (bounds.Right > constraint.Bounds.Right)
                {
                    if (entity.HasComponent<PlayerTag>())
                    {
                        // Player: clamp position with offset
                        float colliderXOffset = GetColliderXOffset(entity);
                        newPosition.X = constraint.Bounds.Right - GetSpriteWidth(entity) + colliderXOffset;
                    }
                    else
                    {
                        // Other entities: clamp to boundary
                        newPosition.X = constraint.Bounds.Right - GetSpriteWidth(entity);
                    }
                    collisionNormal.X = -1f; // Normal pointing left
                    positionChanged = true;
                }
            }
            
            // Check vertical bounds
            if (constraint.ConstrainToVertical)
            {
                if (bounds.Top < constraint.Bounds.Top)
                {
                    newPosition.Y = constraint.Bounds.Top;
                    collisionNormal.Y = 1f; // Normal pointing down
                    positionChanged = true;
                }
                else if (bounds.Bottom > constraint.Bounds.Bottom)
                {
                    newPosition.Y = constraint.Bounds.Bottom - GetSpriteHeight(entity);
                    collisionNormal.Y = -1f; // Normal pointing up
                    positionChanged = true;
                }
            }
            
            if (positionChanged)
            {
                // Update position
                transform.Position = newPosition;
                
                // Handle velocity reflection for entities that should bounce
                if (constraint.ReflectVelocityOnCollision && entity.HasComponent<MovementComponent>())
                {
                    ref var movement = ref entity.GetComponent<MovementComponent>();
                    movement.Velocity = Vector2.Reflect(movement.Velocity, collisionNormal);
                }
                
                // Send boundary collision event
                _eventBus.Send(new BoundaryCollisionEvent(
                    entity.Id, 
                    newPosition, 
                    collisionNormal, 
                    constraint.Bounds
                ));
            }
        }
    }
    
    private void ProcessEntityCollisions()
    {
        // Get all entities with collision components
        var collidableEntities = _entityManager.GetEntitiesWith<TransformComponent, CollisionComponent>().ToList();
        
        // Check collisions between players and cops
        var players = _entityManager.GetEntitiesWith<PlayerTag, CollisionComponent>();
        var cops = _entityManager.GetEntitiesWith<CopTag, CollisionComponent>();
        var walls = _entityManager.GetEntitiesWith<WallTag, CollisionComponent>();
        
        
        foreach (var player in players)
        {
            var playerBounds = player.GetComponent<CollisionComponent>().Collider.rectangleCollider;
            var playerTransform = player.GetComponent<TransformComponent>();
            
            // Check player-cop collisions
            foreach (var cop in cops)
            {
                var copBounds = cop.GetComponent<CollisionComponent>().Collider.rectangleCollider;
                var copTransform = cop.GetComponent<TransformComponent>();
                
                if (playerBounds.Intersects(copBounds))
                {
                    Vector2 collisionPoint = new Vector2(
                        (playerBounds.Center.X + copBounds.Center.X) / 2f,
                        (playerBounds.Center.Y + copBounds.Center.Y) / 2f
                    );
                    
                    // Send player-cop collision event
                    _eventBus.Send(new PlayerCopCollisionEvent(
                        player.Id, 
                        cop.Id, 
                        collisionPoint
                    ));
                    
                    // Also send generic entity collision event
                    Vector2 normal = Vector2.Normalize(copTransform.Position - playerTransform.Position);
                    _eventBus.Send(new EntityCollisionEvent(
                        player.Id, 
                        cop.Id, 
                        collisionPoint, 
                        normal
                    ));
                }
            }
            
            // Check player-wall collisions
            foreach (var wall in walls)
            {
                var wallBounds = wall.GetComponent<CollisionComponent>().Collider.rectangleCollider;
                var wallTransform = wall.GetComponent<TransformComponent>();
                
                if (playerBounds.Intersects(wallBounds))
                {
                    // Calculate collision normal (direction to push player away from wall)
                    Vector2 playerCenter = new Vector2(playerBounds.Center.X, playerBounds.Center.Y);
                    Vector2 wallCenter = new Vector2(wallBounds.Center.X, wallBounds.Center.Y);
                    Vector2 direction = playerCenter - wallCenter;
                    
                    if (direction.LengthSquared() > 0)
                    {
                        direction.Normalize();
                    }
                    else
                    {
                        direction = Vector2.UnitY; // Default direction
                    }
                    
                    // Calculate penetration depth
                    float penetrationX = (playerBounds.Width + wallBounds.Width) / 2f - Math.Abs(playerCenter.X - wallCenter.X);
                    float penetrationY = (playerBounds.Height + wallBounds.Height) / 2f - Math.Abs(playerCenter.Y - wallCenter.Y);
                    
                    // Move player out of wall
                    ref var transform = ref player.GetComponent<TransformComponent>();
                    
                    if (penetrationX < penetrationY)
                    {
                        // Horizontal collision - push horizontally
                        if (playerCenter.X < wallCenter.X)
                        {
                            transform.Position.X = wallBounds.Left - playerBounds.Width;
                        }
                        else
                        {
                            transform.Position.X = wallBounds.Right;
                        }
                    }
                    else
                    {
                        // Vertical collision - push vertically
                        if (playerCenter.Y < wallCenter.Y)
                        {
                            transform.Position.Y = wallBounds.Top - playerBounds.Height;
                        }
                        else
                        {
                            transform.Position.Y = wallBounds.Bottom;
                        }
                    }
                    
                    // Stop player movement
                    if (player.HasComponent<MovementComponent>())
                    {
                        ref var movement = ref player.GetComponent<MovementComponent>();
                        movement.Velocity = Vector2.Zero;
                    }
                }
            }
        }
    }
    
    private void OnPlayerCopCollision(PlayerCopCollisionEvent collisionEvent)
    {
        // Handle player-cop collision by teleporting the cop
        var cop = _entityManager.GetEntity(collisionEvent.CopId);
        if (cop == null) return;
        
        ref var transform = ref cop.GetComponent<TransformComponent>();
        Vector2 oldPosition = transform.Position;
        
        // Teleport cop to random position
        Vector2 newPosition = GetRandomPosition();
        transform.Position = newPosition;
        
        // Reset AI state
        if (cop.HasComponent<AIComponent>())
        {
            ref var ai = ref cop.GetComponent<AIComponent>();
            ai.StateTimer = 0f;
            ai.PatrolDirection = GetRandomDirection();
        }
        
        // Send teleport event
        _eventBus.Send(new TeleportEvent(cop.Id, oldPosition, newPosition));
    }
    
    private void OnBoundaryCollision(BoundaryCollisionEvent collisionEvent)
    {
        // Could add visual/audio effects here
        // For now, just log collision
        //Console.WriteLine($"Entity {collisionEvent.EntityId} hit boundary at {collisionEvent.CollisionPoint}");
    }
    
    private float GetColliderXOffset(Entity entity)
    {
        if (entity.HasComponent<SpriteComponent>())
        {
            var width = GetSpriteWidth(entity);
            return width * GameConfig.ColliderXOffsetRatio;
        }
        return 0f;
    }
    
    private float GetSpriteWidth(Entity entity)
    {
        if (entity.HasComponent<SpriteComponent>() && entity.HasComponent<TransformComponent>())
        {
            var sprite = entity.GetComponent<SpriteComponent>();
            var transform = entity.GetComponent<TransformComponent>();
            
            if (sprite.Sprite?.CurrentRegion != null)
            {
                // Apply transform scale to sprite width
                return sprite.Sprite.CurrentRegion.Width * transform.Scale.X;
            }
        }
        return 32f; // Default size
    }
    
    private float GetSpriteHeight(Entity entity)
    {
        if (entity.HasComponent<SpriteComponent>() && entity.HasComponent<TransformComponent>())
        {
            var sprite = entity.GetComponent<SpriteComponent>();
            var transform = entity.GetComponent<TransformComponent>();
            
            if (sprite.Sprite?.CurrentRegion != null)
            {
                // Apply transform scale to sprite height
                return sprite.Sprite.CurrentRegion.Height * transform.Scale.Y;
            }
        }
        return 32f; // Default size
    }
    
    private Vector2 GetRandomPosition()
    {
        if (_tilemap == null) return Vector2.Zero;
        
        int columns = (int)(_roomBounds.Width / _tilemap.TileWidth);
        int rows = (int)(_roomBounds.Height / _tilemap.TileHeight);
        
        int column = _random.Next(1, columns - 1);
        int row = _random.Next(1, rows - 1);

        return new Vector2(
            _roomBounds.Left + column * _tilemap.TileWidth, 
            _roomBounds.Top + row * _tilemap.TileHeight
        );
    }
    
    private Vector2 GetRandomDirection()
    {
        float angle = _random.NextSingle() * MathF.PI * 2f;
        return new Vector2(MathF.Cos(angle), MathF.Sin(angle));
    }
}