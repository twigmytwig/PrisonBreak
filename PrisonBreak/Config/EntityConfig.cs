using Microsoft.Xna.Framework;

namespace PrisonBreak.Config;

public static class EntityConfig
{
    public static class Player
    {
        public const string AnimationName = "prisoner-animation";
        public const bool DebugMode = true;
        public static readonly Vector2 Scale = new Vector2(GameConfig.SpriteScale, GameConfig.SpriteScale);
    }

    public static class Cop
    {
        public const string AnimationName = "cop-animation";
        public const bool DebugMode = true;
        public static readonly Vector2 Scale = new Vector2(GameConfig.SpriteScale, GameConfig.SpriteScale);
        public const float MovementSpeed = 150.0f;
    }

    public static class Tilemap
    {
        public const string ConfigFile = "images/tilemap-definition.xml";
        public static readonly Vector2 Scale = new Vector2(GameConfig.SpriteScale, GameConfig.SpriteScale);
    }

    public static class TextureAtlas
    {
        public const string ConfigFile = "images/atlas-definition.xml";
    }

    public static class UIAtlas
    {
        public const string ConfigFile = "images/ui-atlas-definition.xml";
    }

    public static class OverlayAtlas
    {
        public const string ConfigFile = "images/overlay-atlas-definition.xml";
    }
}