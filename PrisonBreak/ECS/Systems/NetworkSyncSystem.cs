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
/// System responsible for synchronizing networked entities (positions, movement) across clients
/// </summary>
public class NetworkSyncSystem : IGameSystem
{
    private ComponentEntityManager _entityManager;
    private EventBus _eventBus;
    private NetworkManager _networkManager;
    
    // Sync timing
    private double _lastSyncTime = 0;
    private const double SYNC_INTERVAL = 1.0 / 20.0; // 20Hz sync rate
    
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
            Console.WriteLine("[NetworkSyncSystem] Initialized with NetworkManager");
        }
        catch (InvalidOperationException)
        {
            _networkManager = null;
            Console.WriteLine("[NetworkSyncSystem] No NetworkManager found - sync disabled");
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
            SyncNetworkedEntities();
            _lastSyncTime = gameTime.TotalGameTime.TotalSeconds;
        }
    }
    
    public void Draw(SpriteBatch spriteBatch)
    {
        // NetworkSyncSystem doesn't render anything
    }
    
    public void Shutdown()
    {
        // Clean up if needed
    }
    
    /// <summary>
    /// Find networked entities that need position sync and send updates
    /// </summary>
    private void SyncNetworkedEntities()
    {
        // Get all entities that have NetworkComponent and need transform sync
        var networkedEntities = _entityManager.GetEntitiesWith<NetworkComponent, TransformComponent>()
            .Where(e => e.GetComponent<NetworkComponent>().SyncTransform)
            .ToList();
            
        foreach (var entity in networkedEntities)
        {
            var networkComp = entity.GetComponent<NetworkComponent>();
            var transform = entity.GetComponent<TransformComponent>();
            
            
            // Only send updates for entities we own
            if (networkComp.Authority == NetworkConfig.NetworkAuthority.Client && 
                networkComp.OwnerId == _networkManager.GetLocalPlayerId())
            {
                // Create and send transform message using networkId instead of entity.Id
                var transformMessage = new TransformMessage(networkComp.NetworkId, transform);
                SendNetworkMessage(transformMessage);
            }
        }
    }
    
    /// <summary>
    /// Send network message via NetworkManager
    /// </summary>
    private void SendNetworkMessage(TransformMessage message)
    {
        _networkManager.SendTransformUpdate(message);
    }
}