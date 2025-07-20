using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PrisonBreak.Core.Physics;
using PrisonBreak.Config;
using PrisonBreak.Systems;

namespace PrisonBreak.ECS.Systems;

public class ComponentMovementSystem : IGameSystem
{
    private ComponentEntityManager _entityManager;
    private EventBus _eventBus;
    private Random _random = new Random();
    private bool[,] _collisionMap;
    private float _tileSize;
    private Vector2 _mapOffset;

    public void SetEntityManager(ComponentEntityManager entityManager)
    {
        _entityManager = entityManager;
    }

    public void SetEventBus(EventBus eventBus)
    {
        _eventBus = eventBus;

        // Subscribe to input events
        _eventBus.Subscribe<PlayerInputEvent>(OnPlayerInput);
    }
    
    public void SetCollisionMap(Core.Graphics.Tilemap tilemap, Vector2 offset)
    {
        if (tilemap == null) return;
        
        _tileSize = tilemap.TileWidth;
        _mapOffset = offset;
        
        // Create collision map from tilemap
        _collisionMap = new bool[tilemap.Columns, tilemap.Rows];
        
        // Solid tile IDs (02 = prison bars, 03 = walls)
        int[] solidTileIds = { 2, 3 };
        
        for (int row = 0; row < tilemap.Rows; row++)
        {
            for (int col = 0; col < tilemap.Columns; col++)
            {
                int tileId = tilemap.GetTileId(col, row);
                _collisionMap[col, row] = Array.Exists(solidTileIds, id => id == tileId);
            }
        }
        
        Console.WriteLine($"Created collision map: {tilemap.Columns}x{tilemap.Rows}, TileSize: {_tileSize}");
    }

    public void Initialize()
    {
        // Initialize any AI entities with random directions
        var aiEntities = _entityManager.GetEntitiesWith<AIComponent, MovementComponent>();
        foreach (var entity in aiEntities)
        {
            ref var ai = ref entity.GetComponent<AIComponent>();
            ai.PatrolDirection = GetRandomDirection();
        }
    }

    public void Update(GameTime gameTime)
    {
        if (_entityManager == null) return;

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Update all entities with movement components
        var movingEntities = _entityManager.GetEntitiesWith<MovementComponent, TransformComponent>();

        foreach (var entity in movingEntities)
        {
            ref var movement = ref entity.GetComponent<MovementComponent>();
            ref var transform = ref entity.GetComponent<TransformComponent>();

            // Calculate intended new position
            Vector2 intendedPosition = transform.Position + movement.Velocity * deltaTime;
            Vector2 safePosition = intendedPosition;

            // Check for wall collisions if entity has collision component
            if (entity.HasComponent<CollisionComponent>())
            {
                safePosition = GetSafePosition(entity, transform.Position, intendedPosition);
            }

            // Apply the safe position
            transform.Position = safePosition;

            // Apply friction
            movement.Velocity *= movement.Friction;

            // Update collider position if entity has collision
            if (entity.HasComponent<CollisionComponent>())
            {
                UpdateColliderPosition(entity, transform.Position);
            }
        }

        // Update AI entities
        UpdateAIEntities(gameTime);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        // Movement system doesn't draw anything
    }

    public void Shutdown()
    {
        if (_eventBus != null)
        {
            _eventBus.Unsubscribe<PlayerInputEvent>(OnPlayerInput);
        }
    }

    private void OnPlayerInput(PlayerInputEvent inputEvent)
    {
        var entity = _entityManager.GetEntity(inputEvent.EntityId);
        if (entity == null || !entity.HasComponent<MovementComponent>()) return;

        ref var movement = ref entity.GetComponent<MovementComponent>();
        ref var inputComponent = ref entity.GetComponent<PlayerInputComponent>();

        // Calculate target velocity based on input
        float speed = GameConfig.BaseMovementSpeed;
        if (inputEvent.SpeedBoost)
        {
            speed *= inputComponent.SpeedBoostMultiplier;
        }

        // Set velocity directly for responsive player movement
        movement.Velocity = inputEvent.MovementDirection * speed;
    }

    private void UpdateAIEntities(GameTime gameTime)
    {
        var aiEntities = _entityManager.GetEntitiesWith<AIComponent, MovementComponent, TransformComponent>();

        foreach (var entity in aiEntities)
        {
            ref var ai = ref entity.GetComponent<AIComponent>();
            ref var movement = ref entity.GetComponent<MovementComponent>();
            ref var transform = ref entity.GetComponent<TransformComponent>();

            ai.StateTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

            switch (ai.Behavior)
            {
                case AIBehavior.Patrol:
                    HandlePatrolBehavior(ref ai, ref movement, transform.Position);
                    break;

                case AIBehavior.Wander:
                    HandleWanderBehavior(ref ai, ref movement);
                    break;

                case AIBehavior.Chase:
                    HandleChaseBehavior(ref ai, ref movement, transform.Position);
                    break;

                case AIBehavior.Guard:
                    HandleGuardBehavior(ref ai, ref movement, transform.Position);
                    break;
            }
        }
    }

    private void HandlePatrolBehavior(ref AIComponent ai, ref MovementComponent movement, Vector2 currentPosition)
    {
        // Simple patrol - move in a direction until hitting something or time limit
        movement.Velocity = ai.PatrolDirection * movement.MaxSpeed;

        // Change direction every 3-5 seconds
        if (ai.StateTimer > 3.0f + _random.NextSingle() * 2.0f)
        {
            ai.PatrolDirection = GetRandomDirection();
            ai.StateTimer = 0f;
        }
    }

    private void HandleWanderBehavior(ref AIComponent ai, ref MovementComponent movement)
    {
        // Random wandering with occasional direction changes
        if (ai.StateTimer > 1.0f + _random.NextSingle() * 2.0f)
        {
            // Change direction
            ai.PatrolDirection = GetRandomDirection();
            ai.StateTimer = 0f;
        }

        movement.Velocity = ai.PatrolDirection * (movement.MaxSpeed * 0.5f); // Slower wandering
    }

    private void HandleChaseBehavior(ref AIComponent ai, ref MovementComponent movement, Vector2 currentPosition)
    {
        // Find target entity (player)
        var targetEntity = _entityManager.GetEntity(ai.EntityTargetId);
        if (targetEntity?.HasComponent<TransformComponent>() == true)
        {
            var targetTransform = targetEntity.GetComponent<TransformComponent>();
            Vector2 direction = targetTransform.Position - currentPosition;

            if (direction.Length() > 1.0f)
            {
                direction.Normalize();
                movement.Velocity = direction * movement.MaxSpeed * 1.2f; // Faster when chasing
            }
        }
        else
        {
            // No target, fall back to patrol
            ai.Behavior = AIBehavior.Patrol;
            ai.StateTimer = 0f;
        }
    }

    private void HandleGuardBehavior(ref AIComponent ai, ref MovementComponent movement, Vector2 currentPosition)
    {
        // Return to guard position
        Vector2 direction = ai.TargetPosition - currentPosition;
        float distance = direction.Length();

        if (distance > 10f) // Return to guard position if too far
        {
            direction.Normalize();
            movement.Velocity = direction * movement.MaxSpeed * 0.7f;
        }
        else
        {
            movement.Velocity = Vector2.Zero; // Stop when at guard position
        }
    }

    private Vector2 GetRandomDirection()
    {
        float angle = _random.NextSingle() * MathF.PI * 2f;
        return new Vector2(MathF.Cos(angle), MathF.Sin(angle));
    }
    
    private bool IsTileSolid(int tileX, int tileY)
    {
        if (_collisionMap == null) return false;
        
        // Check bounds
        if (tileX < 0 || tileY < 0 || tileX >= _collisionMap.GetLength(0) || tileY >= _collisionMap.GetLength(1))
            return true; // Treat out-of-bounds as solid
            
        return _collisionMap[tileX, tileY];
    }
    
    private Vector2 WorldToTile(Vector2 worldPos)
    {
        return new Vector2(
            (worldPos.X - _mapOffset.X) / _tileSize,
            (worldPos.Y - _mapOffset.Y) / _tileSize
        );
    }
    
    private Vector2 TileToWorld(Vector2 tilePos)
    {
        return new Vector2(
            tilePos.X * _tileSize + _mapOffset.X,
            tilePos.Y * _tileSize + _mapOffset.Y
        );
    }

    private Vector2 GetSafePosition(Entity movingEntity, Vector2 currentPosition, Vector2 intendedPosition)
    {
        // Only check collision for players (entities with PlayerTag)
        // Cops and AI entities can use the reactive collision system
        if (!movingEntity.HasComponent<PlayerTag>() || _collisionMap == null)
        {
            return intendedPosition;
        }

        // Get player collision bounds and calculate offset from position
        var collision = movingEntity.GetComponent<CollisionComponent>();
        var worldBounds = collision.Collider.rectangleCollider;
        
        // Calculate the offset between player position and collision bounds
        Vector2 colliderOffset = new Vector2(
            worldBounds.X - currentPosition.X,
            worldBounds.Y - currentPosition.Y
        );
        
        // Create relative bounds (size + offset)
        Rectangle relativeBounds = new Rectangle(
            (int)colliderOffset.X,
            (int)colliderOffset.Y,
            worldBounds.Width,
            worldBounds.Height
        );
        
        // Use tile-based collision detection
        return GetSafeMovementTileBased(currentPosition, intendedPosition, relativeBounds);
    }
    
    private Vector2 GetSafeMovementTileBased(Vector2 from, Vector2 to, Rectangle playerBounds)
    {
        Vector2 movement = to - from;
        float distance = movement.Length();
        
        if (distance < 0.1f) return to; // Too small to matter
        
        // Check if current position is already colliding - if so, try to move away
        if (IsPositionCollidingWithTiles(from, playerBounds))
        {
            // Player is stuck in a wall - try to find nearest safe position
            Vector2 escapedPosition = FindNearestSafePosition(from, playerBounds);
            if (escapedPosition != from)
            {
                return escapedPosition;
            }
        }
        
        Vector2 direction = movement / distance;
        Vector2 safePosition = from;
        
        // Test movement in smaller steps for better precision
        float stepSize = Math.Min(4f, distance / 8f); // Smaller steps: 4px or 1/8 of movement
        float currentDistance = 0f;
        
        while (currentDistance < distance)
        {
            float nextDistance = Math.Min(currentDistance + stepSize, distance);
            Vector2 testPosition = from + direction * nextDistance;
            
            if (IsPositionCollidingWithTiles(testPosition, playerBounds))
            {
                // Collision detected - try sliding along walls
                Vector2 slidePosition = TrySlideMovement(safePosition, to, playerBounds);
                return slidePosition;
            }
            
            safePosition = testPosition;
            currentDistance = nextDistance;
        }
        
        return to; // No collision detected
    }
    
    private Vector2 FindNearestSafePosition(Vector2 stuckPosition, Rectangle playerBounds)
    {
        // Try small offsets in all directions to escape from being stuck
        Vector2[] offsets = {
            new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1),
            new Vector2(1, 1), new Vector2(-1, -1), new Vector2(1, -1), new Vector2(-1, 1)
        };
        
        for (int distance = 1; distance <= 8; distance++)
        {
            foreach (var offset in offsets)
            {
                Vector2 testPos = stuckPosition + offset * distance;
                if (!IsPositionCollidingWithTiles(testPos, playerBounds))
                {
                    return testPos;
                }
            }
        }
        
        return stuckPosition; // Couldn't find safe position
    }
    
    private bool IsPositionCollidingWithTiles(Vector2 position, Rectangle playerBounds)
    {
        // Calculate the rectangle the player would occupy at the test position
        // Apply the collision bounds offset to the test position
        Rectangle testBounds = new Rectangle(
            (int)position.X + playerBounds.X,
            (int)position.Y + playerBounds.Y,
            playerBounds.Width,
            playerBounds.Height
        );
        
        // Convert player bounds to tile coordinates
        Vector2 topLeft = WorldToTile(new Vector2(testBounds.Left, testBounds.Top));
        Vector2 bottomRight = WorldToTile(new Vector2(testBounds.Right - 1, testBounds.Bottom - 1));
        
        // Check all tiles the player would overlap
        for (int tileX = (int)Math.Floor(topLeft.X); tileX <= (int)Math.Floor(bottomRight.X); tileX++)
        {
            for (int tileY = (int)Math.Floor(topLeft.Y); tileY <= (int)Math.Floor(bottomRight.Y); tileY++)
            {
                if (IsTileSolid(tileX, tileY))
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    private Vector2 TrySlideMovement(Vector2 safePos, Vector2 intendedPos, Rectangle playerBounds)
    {
        Vector2 remainingMovement = intendedPos - safePos;
        
        // Try horizontal movement only
        Vector2 horizontalPos = new Vector2(intendedPos.X, safePos.Y);
        if (!IsPositionCollidingWithTiles(horizontalPos, playerBounds))
        {
            return horizontalPos;
        }
        
        // Try vertical movement only
        Vector2 verticalPos = new Vector2(safePos.X, intendedPos.Y);
        if (!IsPositionCollidingWithTiles(verticalPos, playerBounds))
        {
            return verticalPos;
        }
        
        // Can't slide - stay at safe position
        return safePos;
    }


    private void UpdateColliderPosition(Entity entity, Vector2 newPosition)
    {
        ref var collision = ref entity.GetComponent<CollisionComponent>();

        // Calculate collider offset with scaling applied
        if (entity.HasComponent<SpriteComponent>() && entity.HasComponent<TransformComponent>())
        {
            var sprite = entity.GetComponent<SpriteComponent>();
            var transform = entity.GetComponent<TransformComponent>();

            // Get sprite dimensions from current region
            float spriteWidth = 32f; // Default
            float spriteHeight = 32f; // Default
            if (sprite.Sprite?.CurrentRegion != null)
            {
                spriteWidth = sprite.Sprite.CurrentRegion.Width;
                spriteHeight = sprite.Sprite.CurrentRegion.Height;
            }

            // Apply transform scale to sprite dimensions
            float scaledWidth = spriteWidth * transform.Scale.X;
            float scaledHeight = spriteHeight * transform.Scale.Y;

            float colliderWidth = scaledWidth * GameConfig.ColliderWidthRatio;
            float colliderHeight = scaledHeight * GameConfig.ColliderHeightRatio;

            var newColliderBounds = new Rectangle(
                (int)(newPosition.X + (scaledWidth - colliderWidth) / 2),
                (int)(newPosition.Y + (scaledHeight - colliderHeight) / 2),
                (int)colliderWidth,
                (int)colliderHeight
            );

            // Update the collider's rectangle
            collision.Collider = new RectangleCollider(
                newColliderBounds.X,
                newColliderBounds.Y,
                newColliderBounds.Width,
                newColliderBounds.Height
            );
        }
    }
}