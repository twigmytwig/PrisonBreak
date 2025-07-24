using System.Collections.Generic;

namespace PrisonBreak.ECS;

public static class ItemDatabase
{
    public struct ItemDefinition
    {
        public string ItemId;
        public string ItemName;
        public string ItemType;
        public string AtlasRegionName;
        public bool IsStackable;
        public int StackSize;
        
        public ItemDefinition(string itemId, string itemName, string itemType, string atlasRegionName, bool isStackable = false, int stackSize = 1)
        {
            ItemId = itemId;
            ItemName = itemName;
            ItemType = itemType;
            AtlasRegionName = atlasRegionName;
            IsStackable = isStackable;
            StackSize = stackSize;
        }
    }
    
    private static readonly Dictionary<string, ItemDefinition> _items = new()
    {
        ["key"] = new ItemDefinition(
            itemId: "key",
            itemName: "Key", 
            itemType: "tool",
            atlasRegionName: "key",
            isStackable: false,
            stackSize: 1
        )
        
        // Future items can be added here:
        // ["lockpick"] = new ItemDefinition("lockpick", "Lockpick", "tool", "lockpick", false, 1),
        // ["weapon"] = new ItemDefinition("weapon", "Weapon", "weapon", "weapon", false, 1),
        // ["food"] = new ItemDefinition("food", "Food", "consumable", "food", true, 5),
    };
    
    public static ItemDefinition? GetItem(string itemId)
    {
        return _items.TryGetValue(itemId, out var item) ? item : null;
    }
    
    public static bool ItemExists(string itemId)
    {
        return _items.ContainsKey(itemId);
    }
    
    public static IEnumerable<ItemDefinition> GetAllItems()
    {
        return _items.Values;
    }
    
    public static IEnumerable<string> GetAllItemIds()
    {
        return _items.Keys;
    }
}