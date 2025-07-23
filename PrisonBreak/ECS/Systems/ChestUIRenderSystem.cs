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


        // TODO: Draw chest inventory slots
        // TODO: Draw player inventory slots  
        // TODO: Draw transfer arrows
    }
}