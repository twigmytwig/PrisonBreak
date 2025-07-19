using Microsoft.Xna.Framework;

namespace PrisonBreak.Config;

public static class GameConfig
{
    public const string WindowTitle = "Dungeon Slime";
    public const int WindowWidth = 1920;
    public const int WindowHeight = 1080;
    public const bool StartFullscreen = false;

    public const float BaseMovementSpeed = 200.0f;
    public const float SpeedBoostMultiplier = 1.5f;
    
    public const float SpriteScale = 4.0f;
    public const float ColliderWidthRatio = 0.5f;
    public const float ColliderHeightRatio = 1.0f;
    public const float ColliderXOffsetRatio = 0.25f;

    public static readonly Color BackgroundColor = Color.CornflowerBlue;
    public static readonly Color PlayerColliderColor = Color.Red;
    public static readonly Color CopColliderColor = Color.Blue;
    
    public const int ColliderDebugThickness = 2;
}