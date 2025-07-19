using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.RectangleCollider;
using PrisonBreak.Config;
using PrisonBreak.Systems;

namespace PrisonBreak.ECS;

public class ComponentMovementSystem : IGameSystem
{
    private ComponentEntityManager _entityManager;
    private EventBus _eventBus;
    private Random _random = new Random();

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

            // Apply velocity to position
            transform.Position += movement.Velocity * deltaTime;

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