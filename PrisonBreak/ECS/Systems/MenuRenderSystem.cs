using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PrisonBreak.Systems;

namespace PrisonBreak.ECS.Systems;

/// <summary>
/// Renders menu UI elements
/// </summary>
public class MenuRenderSystem : IGameSystem
{
    private ComponentEntityManager _entityManager;
    private EventBus _eventBus;
    private Texture2D _whitePixel;
    private SpriteFont _defaultFont;
    private bool _initialized;
    private Microsoft.Xna.Framework.Content.ContentManager _content;
    
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
        LoadFont();
    }
    
    public void Initialize()
    {
        CreateTextures();
        _initialized = true;
    }
    
    public void Update(GameTime gameTime)
    {
        // Menu render system doesn't need to update anything
    }
    
    public void Draw(SpriteBatch spriteBatch)
    {
        if (!_initialized || _entityManager == null) return;
        
        // Draw menu items (entities with MenuItemComponent)
        var menuItems = _entityManager.GetEntitiesWith<MenuItemComponent, TransformComponent>()
            .OrderBy(e => e.GetComponent<MenuItemComponent>().DrawOrder);
        
        // Removed excessive debug logging
        
        foreach (var menuItem in menuItems)
        {
            var transform = menuItem.GetComponent<TransformComponent>();
            var menuComp = menuItem.GetComponent<MenuItemComponent>();
            
            DrawMenuItem(spriteBatch, menuComp, transform);
        }
        
        // Draw text elements (entities with TextComponent)
        var textElements = _entityManager.GetEntitiesWith<TextComponent, TransformComponent>()
            .OrderBy(e => e.GetComponent<TextComponent>().DrawOrder);
        
        // Removed excessive debug logging
        
        foreach (var textElement in textElements)
        {
            var transform = textElement.GetComponent<TransformComponent>();
            var textComp = textElement.GetComponent<TextComponent>();
            
            DrawText(spriteBatch, textComp, transform);
        }
    }
    
    public void Shutdown()
    {
        _whitePixel?.Dispose();
        _initialized = false;
    }
    
    private void DrawMenuItem(SpriteBatch spriteBatch, MenuItemComponent menuItem, TransformComponent transform)
    {
        if (!menuItem.Visible || _whitePixel == null) return;
        
        // Draw background
        var bounds = new Rectangle(
            (int)transform.Position.X,
            (int)transform.Position.Y,
            menuItem.Width,
            menuItem.Height
        );
        
        Color backgroundColor = menuItem.IsSelected ? menuItem.SelectedColor : menuItem.BackgroundColor;
        spriteBatch.Draw(_whitePixel, bounds, backgroundColor);
        
        // Draw border if needed
        if (menuItem.BorderThickness > 0)
        {
            DrawBorder(spriteBatch, bounds, menuItem.BorderColor, menuItem.BorderThickness);
        }
    }
    
    private void DrawText(SpriteBatch spriteBatch, TextComponent textComp, TransformComponent transform)
    {
        if (!textComp.Visible || string.IsNullOrEmpty(textComp.Text)) return;
        
        // Use default font if no font is specified (we'll add font loading later)
        var font = textComp.Font ?? _defaultFont;
        if (font == null) return;
        
        Vector2 textSize = font.MeasureString(textComp.Text);
        Vector2 drawPosition = transform.Position;
        
        // Apply text alignment
        switch (textComp.Alignment)
        {
            case TextAlignment.Center:
                drawPosition.X -= textSize.X / 2;
                drawPosition.Y -= textSize.Y / 2;
                break;
            case TextAlignment.CenterHorizontal:
                drawPosition.X -= textSize.X / 2;
                break;
            case TextAlignment.CenterVertical:
                drawPosition.Y -= textSize.Y / 2;
                break;
        }
        
        spriteBatch.DrawString(font, textComp.Text, drawPosition, textComp.Color, 
            transform.Rotation, Vector2.Zero, transform.Scale, SpriteEffects.None, 0f);
    }
    
    private void DrawBorder(SpriteBatch spriteBatch, Rectangle bounds, Color color, int thickness)
    {
        // Top
        spriteBatch.Draw(_whitePixel, new Rectangle(bounds.X, bounds.Y, bounds.Width, thickness), color);
        // Bottom
        spriteBatch.Draw(_whitePixel, new Rectangle(bounds.X, bounds.Bottom - thickness, bounds.Width, thickness), color);
        // Left
        spriteBatch.Draw(_whitePixel, new Rectangle(bounds.X, bounds.Y, thickness, bounds.Height), color);
        // Right
        spriteBatch.Draw(_whitePixel, new Rectangle(bounds.Right - thickness, bounds.Y, thickness, bounds.Height), color);
    }
    
    private void CreateTextures()
    {
        var graphicsDevice = PrisonBreak.Core.Core.GraphicsDevice;
        if (graphicsDevice != null)
        {
            _whitePixel = new Texture2D(graphicsDevice, 1, 1);
            _whitePixel.SetData(new[] { Color.White });
        }
    }
    
    private void LoadFont()
    {
        if (_content != null)
        {
            try
            {
                _defaultFont = _content.Load<SpriteFont>("MinecraftFont");
                Console.WriteLine("Successfully loaded MinecraftFont");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load MinecraftFont: {ex.Message}");
                _defaultFont = null;
            }
        }
    }
}

/// <summary>
/// Component for menu items (buttons, panels, etc.)
/// </summary>
public struct MenuItemComponent
{
    public bool Visible;
    public bool IsSelected;
    public int Width;
    public int Height;
    public Color BackgroundColor;
    public Color SelectedColor;
    public Color BorderColor;
    public int BorderThickness;
    public int DrawOrder;
    public string ItemId; // For identifying specific menu items
    
    public MenuItemComponent(int width, int height, string itemId = "")
    {
        Visible = true;
        IsSelected = false;
        Width = width;
        Height = height;
        BackgroundColor = new Color(50, 50, 50, 200); // Dark semi-transparent
        SelectedColor = new Color(100, 100, 150, 220); // Lighter when selected
        BorderColor = Color.White;
        BorderThickness = 2;
        DrawOrder = 0;
        ItemId = itemId;
    }
}

/// <summary>
/// Component for text rendering
/// </summary>
public struct TextComponent
{
    public string Text;
    public Color Color;
    public bool Visible;
    public SpriteFont Font;
    public TextAlignment Alignment;
    public int DrawOrder;
    
    public TextComponent(string text)
    {
        Text = text;
        Color = Color.White;
        Visible = true;
        Font = null; // Will use default font
        Alignment = TextAlignment.TopLeft;
        DrawOrder = 1; // Draw text above menu items by default
    }
}

/// <summary>
/// Text alignment options
/// </summary>
public enum TextAlignment
{
    TopLeft,
    Center,
    CenterHorizontal,
    CenterVertical
}