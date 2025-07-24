using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PrisonBreak.Core.Graphics;
using PrisonBreak.Config;
using PrisonBreak.Systems;

namespace PrisonBreak.ECS.Systems;

/// <summary>
/// Renders the chest UI overlay when a chest is open
/// </summary>
public class ChestUIRenderSystem : IGameSystem
{
    private ComponentEntityManager _entityManager;
    private EventBus _eventBus;
    private Microsoft.Xna.Framework.Content.ContentManager _content;

    // Atlas sprites for UI and overlays
    private TextureAtlas _uiAtlas;
    private TextureAtlas _overlayAtlas;

    // State
    private bool _isChestUIOpen = false;
    private Entity _currentChestEntity = null;
    private Entity _currentPlayerEntity = null;

    // Selection state
    private int _selectedSlotIndex = 0;
    private bool _isPlayerInventorySelected = true;

    public void SetEntityManager(ComponentEntityManager entityManager)
    {
        _entityManager = entityManager;
    }

    public void SetEventBus(EventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public void SetContent(Microsoft.Xna.Framework.Content.ContentManager content)
    {
        _content = content;
        // Load atlases immediately when content is set
        LoadUIAtlases();
    }

    public void Initialize()
    {
        // Atlas loading happens in SetContent() when content manager is available

        // Subscribe to chest UI events
        _eventBus?.Subscribe<ChestUIOpenEvent>(OnChestUIOpen);
        _eventBus?.Subscribe<ChestUICloseEvent>(OnChestUIClose);
        _eventBus?.Subscribe<InventorySlotSelectedEvent>(OnSlotSelected);
    }

    public void Update(GameTime gameTime)
    {
        // No frame-by-frame logic needed
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        // Only draw if chest UI is open
        if (!_isChestUIOpen || _currentChestEntity == null)
            return;

        DrawChestUIOverlay(spriteBatch);
    }

    public void Shutdown()
    {
        // Unsubscribe from events
        _eventBus?.Unsubscribe<ChestUIOpenEvent>(OnChestUIOpen);
        _eventBus?.Unsubscribe<ChestUICloseEvent>(OnChestUIClose);
        _eventBus?.Unsubscribe<InventorySlotSelectedEvent>(OnSlotSelected);
    }

    private void LoadUIAtlases()
    {
        if (_content == null)
        {
            Console.WriteLine("ChestUIRenderSystem: Content manager is null");
            return;
        }

        try
        {
            _uiAtlas = TextureAtlas.FromFile(_content, EntityConfig.UIAtlas.ConfigFile);
            Console.WriteLine("ChestUIRenderSystem: Loaded UI atlas successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ChestUIRenderSystem: Failed to load UI atlas: {ex.Message}");
        }

        try
        {
            _overlayAtlas = TextureAtlas.FromFile(_content, EntityConfig.OverlayAtlas.ConfigFile);
            Console.WriteLine("ChestUIRenderSystem: Loaded overlay atlas successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ChestUIRenderSystem: Failed to load overlay atlas: {ex.Message}");
        }
    }

    private void OnChestUIOpen(ChestUIOpenEvent evt)
    {
        _isChestUIOpen = true;
        _currentChestEntity = evt.ChestEntity;
        _currentPlayerEntity = evt.PlayerEntity;
        Console.WriteLine($"ChestUIRenderSystem: Chest UI opened for chest {evt.ChestEntity.Id}");
    }

    private void OnChestUIClose(ChestUICloseEvent evt)
    {
        _isChestUIOpen = false;
        _currentChestEntity = null;
        _currentPlayerEntity = null;
        Console.WriteLine($"ChestUIRenderSystem: Chest UI closed for chest {evt.ChestEntity.Id}");
    }

    private void OnSlotSelected(InventorySlotSelectedEvent evt)
    {
        _selectedSlotIndex = evt.SlotIndex;
        _isPlayerInventorySelected = evt.IsPlayerInventory;
        Console.WriteLine($"ChestUIRenderSystem: Selected slot {evt.SlotIndex} in {(evt.IsPlayerInventory ? "player" : "chest")} inventory");
    }

    private void DrawChestUIOverlay(SpriteBatch spriteBatch)
    {
        if (_overlayAtlas == null || _uiAtlas == null)
        {
            Console.WriteLine("ChestUIRenderSystem: Atlas not loaded, skipping overlay draw");
            return;
        }

        // Get screen center for positioning
        var graphicsDevice = PrisonBreak.Core.Core.GraphicsDevice;
        if (graphicsDevice == null)
        {
            Console.WriteLine("ChestUIRenderSystem: GraphicsDevice is null");
            return;
        }

        int screenWidth = graphicsDevice.PresentationParameters.BackBufferWidth;
        int screenHeight = graphicsDevice.PresentationParameters.BackBufferHeight;
        Vector2 screenCenter = new Vector2(screenWidth / 2, screenHeight / 2);

        // Draw the semi-transparent overlay background (48x48 scaled up to 192x192)
        try
        {
            var overlaySprite = _overlayAtlas.CreateAnimatedSprite("chest-overlay");
            if (overlaySprite != null)
            {
                Vector2 overlayScale = new Vector2(16f, 16f); // Scale 48x48 to 192x192
                Vector2 scaledSize = new Vector2(48 * overlayScale.X, 48 * overlayScale.Y); // 192x192
                Vector2 overlayPosition = screenCenter - scaledSize / 2; // Center the scaled overlay
                overlaySprite.CurrentRegion.Draw(spriteBatch, overlayPosition, Color.White,
                    0f, Vector2.Zero, overlayScale, SpriteEffects.None, 0.9f);
                Console.WriteLine($"[DEBUG] Drew chest overlay at {overlayPosition} with scale {overlayScale}");
            }
            else
            {
                Console.WriteLine("ChestUIRenderSystem: Failed to create chest-overlay sprite");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ChestUIRenderSystem: Error drawing overlay: {ex.Message}");
        }


        // Draw chest and player inventory slots
        DrawInventorySlots(spriteBatch, screenCenter);
    }

    private void DrawInventorySlots(SpriteBatch spriteBatch, Vector2 screenCenter)
    {
        if (_uiAtlas == null || _currentChestEntity == null || _currentPlayerEntity == null)
            return;

        try
        {
            // Configuration for slot rendering
            const int slotScaleSize = 4; // 16x16 sprites scaled 2x for visibility
            const int slotSpacing = 65; // Space between slots

            // Get chest and player inventory data
            var chestContainer = _currentChestEntity.HasComponent<ContainerComponent>()
                ? _currentChestEntity.GetComponent<ContainerComponent>()
                : new ContainerComponent(0);

            var playerInventory = _currentPlayerEntity.HasComponent<InventoryComponent>()
                ? _currentPlayerEntity.GetComponent<InventoryComponent>()
                : new InventoryComponent(0);

            // Draw chest inventory title and slots
            Vector2 chestInventoryStart = screenCenter + new Vector2(-(16 * slotScaleSize * 5), -(16 * slotScaleSize * 2));
            bool isChestSelected = !_isPlayerInventorySelected;
            DrawInventoryGrid(spriteBatch, chestInventoryStart, chestContainer.ContainedItems,
                chestContainer.MaxItems, slotScaleSize, slotSpacing, isChestSelected);

            // Draw player inventory title and slots  
            Vector2 playerInventoryStart = screenCenter + new Vector2(-100, 20);
            bool isPlayerSelected = _isPlayerInventorySelected;
            DrawInventoryGrid(spriteBatch, playerInventoryStart, playerInventory.Items,
                playerInventory.MaxSlots, slotScaleSize, slotSpacing, isPlayerSelected);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ChestUIRenderSystem: Error drawing inventory slots: {ex.Message}");
        }
    }

    private void DrawInventoryGrid(SpriteBatch spriteBatch, Vector2 startPosition,
        Entity[] items, int maxSlots, int slotScaleSize, int slotSpacing, bool isSelectedInventory)
    {
        if (_uiAtlas == null || items == null)
            return;

        // Draw inventory slots
        for (int i = 0; i < maxSlots; i++)
        {
            Vector2 slotPosition = startPosition + new Vector2(i * slotSpacing, 0);

            // Determine slot color based on selection
            Color slotColor = Color.White;
            if (isSelectedInventory && i == _selectedSlotIndex)
            {
                slotColor = Color.Yellow; // Highlight selected slot
            }

            // Draw empty slot background
            var slotSprite = _uiAtlas.CreateAnimatedSprite("inventory-slot");
            if (slotSprite != null)
            {
                Vector2 slotScale = new Vector2(slotScaleSize, slotScaleSize); // Scale 16x16 to 32x32
                slotSprite.CurrentRegion.Draw(spriteBatch, slotPosition, slotColor,
                    0f, Vector2.Zero, slotScale, SpriteEffects.None, 0.85f);
            }

            // Draw item icon if slot contains an item
            if (i < items.Length && items[i] != null && items[i].HasComponent<ItemComponent>())
            {
                var itemComponent = items[i].GetComponent<ItemComponent>();
                DrawItemIcon(spriteBatch, slotPosition, itemComponent, slotScaleSize);
            }
        }
    }

    private void DrawItemIcon(SpriteBatch spriteBatch, Vector2 position, ItemComponent item, int slotScaleSize)
    {
        if (_uiAtlas == null)
            return;

        try
        {
            // Get sprite name based on item type - currently only "key" is available
            string spriteName = GetItemSpriteName(item);
            var itemSprite = _uiAtlas.CreateAnimatedSprite(spriteName);

            if (itemSprite != null)
            {
                Vector2 itemScale = new Vector2(slotScaleSize, slotScaleSize); // Match slot scale
                itemSprite.CurrentRegion.Draw(spriteBatch, position, Color.White,
                    0f, Vector2.Zero, itemScale, SpriteEffects.None, 0.8f);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ChestUIRenderSystem: Error drawing item icon: {ex.Message}");
        }
    }

    private static string GetItemSpriteName(ItemComponent item)
    {
        // Map item names to sprite names in the UI atlas
        return item.ItemName.ToLower() switch
        {
            "key" => "key",
            _ => "inventory-slot" // Fallback to empty slot if sprite not found
        };
    }
}