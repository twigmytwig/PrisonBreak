using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using PrisonBreak.Multiplayer.Core;
using PrisonBreak.Multiplayer.Messages;

namespace PrisonBreak.Core.Networking;

/// <summary>
/// Client sends to host to request an interaction (item pickup, chest opening, etc.)
/// </summary>
public class InteractionRequestMessage : NetworkMessage
{
    public int PlayerId { get; set; }
    public int TargetNetworkId { get; set; }
    public string InteractionType { get; set; } // "pickup", "chest_open", "chest_close"
    public Vector2 PlayerPosition { get; set; } // For host validation
    
    public InteractionRequestMessage() : base(NetworkConfig.MessageType.InteractionRequest) { }
    
    public InteractionRequestMessage(int playerId, int targetNetworkId, string interactionType, Vector2 playerPosition) 
        : base(NetworkConfig.MessageType.InteractionRequest)
    {
        PlayerId = playerId;
        TargetNetworkId = targetNetworkId;
        InteractionType = interactionType;
        PlayerPosition = playerPosition;
    }
    
    protected override void SerializeData(NetDataWriter writer)
    {
        writer.Put(PlayerId);
        writer.Put(TargetNetworkId);
        writer.Put(InteractionType);
        writer.Put(PlayerPosition.X);
        writer.Put(PlayerPosition.Y);
    }
    
    protected override void DeserializeData(NetDataReader reader)
    {
        PlayerId = reader.GetInt();
        TargetNetworkId = reader.GetInt();
        InteractionType = reader.GetString();
        PlayerPosition = new Vector2(reader.GetFloat(), reader.GetFloat());
    }
}

/// <summary>
/// Host sends to client when an interaction request is rejected
/// </summary>
public class InteractionRejectedMessage : NetworkMessage
{
    public int PlayerId { get; set; }
    public int TargetNetworkId { get; set; }
    public string Reason { get; set; } // "out_of_range", "item_taken", "inventory_full", "invalid_target"
    
    public InteractionRejectedMessage() : base(NetworkConfig.MessageType.InteractionRejected) { }
    
    public InteractionRejectedMessage(int playerId, int targetNetworkId, string reason) 
        : base(NetworkConfig.MessageType.InteractionRejected)
    {
        PlayerId = playerId;
        TargetNetworkId = targetNetworkId;
        Reason = reason;
    }
    
    protected override void SerializeData(NetDataWriter writer)
    {
        writer.Put(PlayerId);
        writer.Put(TargetNetworkId);
        writer.Put(Reason);
    }
    
    protected override void DeserializeData(NetDataReader reader)
    {
        PlayerId = reader.GetInt();
        TargetNetworkId = reader.GetInt();
        Reason = reader.GetString();
    }
}

/// <summary>
/// Host broadcasts to all clients when an item is successfully picked up
/// </summary>
public class ItemPickupMessage : NetworkMessage
{
    public int PlayerId { get; set; }
    public int ItemNetworkId { get; set; }
    public int SlotIndex { get; set; }
    public bool Success { get; set; }
    public Vector2 ItemPosition { get; set; } // For removing item from world
    public string ItemType { get; set; } // "key", etc. for item recreation
    
    public ItemPickupMessage() : base(NetworkConfig.MessageType.ItemPickup) { }
    
    public ItemPickupMessage(int playerId, int itemNetworkId, int slotIndex, bool success, Vector2 itemPosition, string itemType) 
        : base(NetworkConfig.MessageType.ItemPickup)
    {
        PlayerId = playerId;
        ItemNetworkId = itemNetworkId;
        SlotIndex = slotIndex;
        Success = success;
        ItemPosition = itemPosition;
        ItemType = itemType;
    }
    
    protected override void SerializeData(NetDataWriter writer)
    {
        writer.Put(PlayerId);
        writer.Put(ItemNetworkId);
        writer.Put(SlotIndex);
        writer.Put(Success);
        writer.Put(ItemPosition.X);
        writer.Put(ItemPosition.Y);
        writer.Put(ItemType);
    }
    
    protected override void DeserializeData(NetDataReader reader)
    {
        PlayerId = reader.GetInt();
        ItemNetworkId = reader.GetInt();
        SlotIndex = reader.GetInt();
        Success = reader.GetBool();
        ItemPosition = new Vector2(reader.GetFloat(), reader.GetFloat());
        ItemType = reader.GetString();
    }
}

/// <summary>
/// Host broadcasts to all clients when any inventory change occurs
/// </summary>
public class InventoryUpdateMessage : NetworkMessage
{
    public int PlayerId { get; set; }
    public int SlotIndex { get; set; }
    public string ItemType { get; set; } // Null/empty = slot emptied, value = slot filled
    public string ActionType { get; set; } // "pickup", "drop", "transfer", "consume"
    public int ItemNetworkId { get; set; } // For tracking specific item instances
    
    public InventoryUpdateMessage() : base(NetworkConfig.MessageType.InventoryUpdate) { }
    
    public InventoryUpdateMessage(int playerId, int slotIndex, string itemType, string actionType, int itemNetworkId) 
        : base(NetworkConfig.MessageType.InventoryUpdate)
    {
        PlayerId = playerId;
        SlotIndex = slotIndex;
        ItemType = itemType;
        ActionType = actionType;
        ItemNetworkId = itemNetworkId;
    }
    
    protected override void SerializeData(NetDataWriter writer)
    {
        writer.Put(PlayerId);
        writer.Put(SlotIndex);
        writer.Put(ItemType ?? "");
        writer.Put(ActionType);
        writer.Put(ItemNetworkId);
    }
    
    protected override void DeserializeData(NetDataReader reader)
    {
        PlayerId = reader.GetInt();
        SlotIndex = reader.GetInt();
        ItemType = reader.GetString();
        if (string.IsNullOrEmpty(ItemType)) ItemType = null;
        ActionType = reader.GetString();
        ItemNetworkId = reader.GetInt();
    }
}

/// <summary>
/// Simple chest interaction message with complete inventory state synchronization
/// </summary>
public class ChestInteractionMessage : NetworkMessage
{
    public int PlayerId { get; set; }
    public int ChestNetworkId { get; set; }
    public string Action { get; set; } // "open", "close", "transfer_to_chest", "transfer_to_player"
    public int SlotIndex { get; set; } // For transfer operations (slot that was clicked)
    public bool Success { get; set; }
    public string ErrorReason { get; set; } // For failed operations
    
    // Complete inventory states after transfer (for synchronization)
    public string[] PlayerInventoryItems { get; set; } // Item names in player inventory (null = empty slot)
    public string[] ChestInventoryItems { get; set; } // Item names in chest inventory (null = empty slot)
    
    public ChestInteractionMessage() : base(NetworkConfig.MessageType.ChestInteraction) 
    { 
        PlayerInventoryItems = new string[0];
        ChestInventoryItems = new string[0];
    }
    
    public ChestInteractionMessage(int playerId, int chestNetworkId, string action, int slotIndex = -1, bool success = true, string errorReason = null) 
        : base(NetworkConfig.MessageType.ChestInteraction)
    {
        PlayerId = playerId;
        ChestNetworkId = chestNetworkId;
        Action = action;
        SlotIndex = slotIndex;
        Success = success;
        ErrorReason = errorReason;
        PlayerInventoryItems = new string[0];
        ChestInventoryItems = new string[0];
    }
    
    protected override void SerializeData(NetDataWriter writer)
    {
        writer.Put(PlayerId);
        writer.Put(ChestNetworkId);
        writer.Put(Action);
        writer.Put(SlotIndex);
        writer.Put(Success);
        writer.Put(ErrorReason ?? "");
        
        // Serialize player inventory
        writer.Put(PlayerInventoryItems.Length);
        for (int i = 0; i < PlayerInventoryItems.Length; i++)
        {
            writer.Put(PlayerInventoryItems[i] ?? "");
        }
        
        // Serialize chest inventory
        writer.Put(ChestInventoryItems.Length);
        for (int i = 0; i < ChestInventoryItems.Length; i++)
        {
            writer.Put(ChestInventoryItems[i] ?? "");
        }
    }
    
    protected override void DeserializeData(NetDataReader reader)
    {
        PlayerId = reader.GetInt();
        ChestNetworkId = reader.GetInt();
        Action = reader.GetString();
        SlotIndex = reader.GetInt();
        Success = reader.GetBool();
        ErrorReason = reader.GetString();
        if (string.IsNullOrEmpty(ErrorReason)) ErrorReason = null;
        
        // Deserialize player inventory
        int playerInventoryCount = reader.GetInt();
        PlayerInventoryItems = new string[playerInventoryCount];
        for (int i = 0; i < playerInventoryCount; i++)
        {
            string item = reader.GetString();
            PlayerInventoryItems[i] = string.IsNullOrEmpty(item) ? null : item;
        }
        
        // Deserialize chest inventory
        int chestInventoryCount = reader.GetInt();
        ChestInventoryItems = new string[chestInventoryCount];
        for (int i = 0; i < chestInventoryCount; i++)
        {
            string item = reader.GetString();
            ChestInventoryItems[i] = string.IsNullOrEmpty(item) ? null : item;
        }
    }
}