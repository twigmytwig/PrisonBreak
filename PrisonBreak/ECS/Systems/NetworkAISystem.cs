using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PrisonBreak.ECS;
using PrisonBreak.Systems;
using PrisonBreak.Managers;
using PrisonBreak.Core.Networking;
using PrisonBreak.Multiplayer.Core;

namespace PrisonBreak.ECS.Systems;

/// <summary>
/// System responsible for synchronizing AI state (position and behavior) across clients.
/// Host has authority over all AI entities - clients receive and apply AI updates.
/// </summary>
public class NetworkAISystem : IGameSystem
{
    private ComponentEntityManager _entityManager;
    private EventBus _eventBus;
    private NetworkManager _networkManager;
    
    // Sync timing
    private double _lastSyncTime = 0;
    private const double SYNC_INTERVAL = 1.0 / 10.0; // 10Hz sync rate for AI (lower than players)
    
    public void SetEntityManager(ComponentEntityManager entityManager)
    {
        _entityManager = entityManager;
    }
    
    public void SetEventBus(EventBus eventBus)
    {
        _eventBus = eventBus;
    }
    
    public void Initialize()
    {
        // Get the NetworkManager singleton
        try
        {
            _networkManager = NetworkManager.Instance;
            Console.WriteLine("[NetworkAISystem] Initialized with NetworkManager");
        }
        catch (InvalidOperationException)
        {
            _networkManager = null;
            Console.WriteLine("[NetworkAISystem] No NetworkManager found - AI sync disabled");
        }
    }
    
    public void Update(GameTime gameTime)
    {
        // Only sync in multiplayer mode
        if (_networkManager == null || _networkManager.CurrentGameMode == NetworkConfig.GameMode.SinglePlayer)
            return;
            
        // Rate-limited sync updates
        if (gameTime.TotalGameTime.TotalSeconds - _lastSyncTime >= SYNC_INTERVAL)
        {
            if (_networkManager.CurrentGameMode == NetworkConfig.GameMode.LocalHost)
            {
                // Host: Send AI state to all clients
                SyncAIStateToClients();
            }
            // Clients: Receive and apply AI updates (handled in NetworkManager message processing)
            
            _lastSyncTime = gameTime.TotalGameTime.TotalSeconds;
        }
    }
    
    public void Draw(SpriteBatch spriteBatch)
    {
        // NetworkAISystem doesn't render anything
    }
    
    public void Shutdown()
    {
        // Clean up if needed
    }
    
    /// <summary>
    /// Host: Find AI entities and send their state to all clients
    /// </summary>
    private void SyncAIStateToClients()
    {
        // Get all AI entities (cops with AI behavior, but NOT player cops)
        var aiEntities = _entityManager.GetEntitiesWith<AIComponent, TransformComponent, CopTag>()
            .Where(e => !e.HasComponent<PlayerTag>()) // Exclude player cops
            .ToList();
            
        foreach (var entity in aiEntities)
        {
            var aiComponent = entity.GetComponent<AIComponent>();
            var transform = entity.GetComponent<TransformComponent>();
            var copTag = entity.GetComponent<CopTag>();
            
            // Send both transform and AI state for each cop
            // Use cop ID as network identifier for AI entities
            var transformMessage = new TransformMessage(copTag.CopId, transform);
            var aiStateMessage = new AIStateMessage(copTag.CopId, aiComponent);
            
            SendNetworkMessage(transformMessage);
            SendNetworkMessage(aiStateMessage);
        }
    }
    
    /// <summary>
    /// Send network message via NetworkManager
    /// </summary>
    private void SendNetworkMessage(TransformMessage message)
    {
        _networkManager.SendTransformUpdate(message);
    }
    
    /// <summary>
    /// Send AI state message via NetworkManager
    /// </summary>
    private void SendNetworkMessage(AIStateMessage message)
    {
        _networkManager.SendAIStateUpdate(message);
    }
    
    /// <summary>
    /// Client: Apply received AI state update to local entity
    /// Called by NetworkManager when AI state message is received
    /// </summary>
    public void ApplyAIStateUpdate(AIStateMessage message)
    {
        // Find the AI entity by cop ID (exclude player cops)
        var aiEntities = _entityManager.GetEntitiesWith<CopTag, AIComponent>()
            .Where(e => !e.HasComponent<PlayerTag>()); // Exclude player cops
        var targetEntity = aiEntities.FirstOrDefault(e => e.GetComponent<CopTag>().CopId == message.EntityId);
        
        if (targetEntity != null)
        {
            // Update the AI component with received state
            ref var aiComponent = ref targetEntity.GetComponent<AIComponent>();
            var receivedAI = message.ToComponent();
            
            aiComponent.Behavior = receivedAI.Behavior;
            aiComponent.PatrolDirection = receivedAI.PatrolDirection;
            aiComponent.StateTimer = receivedAI.StateTimer;
            aiComponent.TargetPosition = receivedAI.TargetPosition;
            aiComponent.EntityTargetId = receivedAI.EntityTargetId;
            
            Console.WriteLine($"[NetworkAISystem] Applied AI state update for cop {message.EntityId}: {aiComponent.Behavior}");
        }
        else
        {
            Console.WriteLine($"[NetworkAISystem] Warning: Could not find AI entity with cop ID {message.EntityId}");
        }
    }
    
    /// <summary>
    /// Client: Apply received transform update to AI entity using interpolation
    /// Called by NetworkManager when transform message is received for AI entities
    /// </summary>
    public void ApplyAITransformUpdate(TransformMessage message, GameTime gameTime)
    {
        // Find the AI entity by cop ID (exclude player cops)
        var aiEntities = _entityManager.GetEntitiesWith<CopTag, TransformComponent>()
            .Where(e => !e.HasComponent<PlayerTag>()); // Exclude player cops
        var targetEntity = aiEntities.FirstOrDefault(e => e.GetComponent<CopTag>().CopId == message.EntityId);
        
        if (targetEntity != null)
        {
            var receivedTransform = message.ToComponent();
            
            // Check if entity has interpolation component for smooth movement
            if (targetEntity.HasComponent<InterpolationComponent>())
            {
                // Use interpolation for smooth AI movement
                var interpolationSystem = _networkManager.GetNetworkInterpolationSystem();
                if (interpolationSystem != null)
                {
                    interpolationSystem.SetInterpolationTarget(targetEntity, receivedTransform.Position, receivedTransform.Rotation, gameTime);
                    Console.WriteLine($"[NetworkAISystem] Set interpolation target for cop {message.EntityId}: {receivedTransform.Position}");
                }
            }
            else
            {
                // Fallback to direct position update for AI entities without interpolation
                ref var transform = ref targetEntity.GetComponent<TransformComponent>();
                transform.Position = receivedTransform.Position;
                transform.Rotation = receivedTransform.Rotation;
                transform.Scale = receivedTransform.Scale;
                
                Console.WriteLine($"[NetworkAISystem] Applied direct transform update for cop {message.EntityId}: {transform.Position}");
            }
        }
        else
        {
            Console.WriteLine($"[NetworkAISystem] Warning: Could not find AI entity with cop ID {message.EntityId}");
        }
    }
}