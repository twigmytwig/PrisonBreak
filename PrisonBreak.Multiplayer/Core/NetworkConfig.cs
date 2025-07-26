namespace PrisonBreak.Multiplayer.Core;

public static class NetworkConfig
{
    // Network settings
    public const int DefaultPort = 7777;
    public const int MaxPlayers = 8;
    public const float NetworkTickRate = 20f; // Hz
    public const int MaxPacketSize = 1024;
    public const int ConnectionTimeout = 5000; // ms
    public const string DiscoveryKey = "PrisonBreakGame";
    
    // Message types following existing enum patterns
    public enum MessageType : byte
    {
        // Core component messages
        Transform = 1,
        Movement = 2,
        PlayerInput = 3,
        
        // Entity management
        EntitySpawn = 10,
        EntityDestroy = 11,
        
        // Game events
        Collision = 20,
        Interaction = 21,
        
        // Inventory system
        Inventory = 30,
        ItemTransfer = 31,
        
        // Connection management
        PlayerJoin = 100,
        PlayerLeave = 101,
        GameState = 102
    }
    
    // Network authority levels
    public enum NetworkAuthority
    {
        Server,    // Host has authority
        Client,    // Client has authority (for their player)
        Shared     // Synchronized but no single authority
    }
    
    // Connection states
    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        Disconnecting
    }
    
    // Game modes (P2P design for Phase 1)
    public enum GameMode
    {
        SinglePlayer,
        LocalHost,  // Player hosts and plays
        Client      // Connects to LocalHost
    }
}