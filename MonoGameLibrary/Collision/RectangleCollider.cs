using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameLibrary.RectangleCollider;

public class RectangleCollider
{
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public int CollisionHeight { get; set; }
    public int CollisionWidth { get; set; }
    public bool DebugMode { get; set; }

    public Rectangle rectangleCollider => new Rectangle(PositionX, PositionY, CollisionWidth, CollisionHeight);
    public RectangleCollider()
    {

    }
    public RectangleCollider(int x, int y, int collisionWidth, int collisionHeight, bool debugMode)
    {
        PositionX = x;
        PositionY = y;
        CollisionWidth = collisionWidth;
        CollisionHeight = collisionHeight;
        DebugMode = debugMode;
    }

    public void Draw(SpriteBatch spriteBatch, Color color, Texture2D _debugTexture, int thickness = 1)
    {
        if (DebugMode)
        {
            //Top line
            spriteBatch.Draw(_debugTexture, new Rectangle(rectangleCollider.X, rectangleCollider.Y, rectangleCollider.Width, thickness), color);
            // Draw bottom line
            spriteBatch.Draw(_debugTexture, new Rectangle(rectangleCollider.X, rectangleCollider.Y + rectangleCollider.Height - thickness, rectangleCollider.Width, thickness), color);
            // Draw left line
            spriteBatch.Draw(_debugTexture, new Rectangle(rectangleCollider.X, rectangleCollider.Y, thickness, rectangleCollider.Height), color);
            // Draw right line
            spriteBatch.Draw(_debugTexture, new Rectangle(rectangleCollider.X + rectangleCollider.Width - thickness, rectangleCollider.Y, thickness, rectangleCollider.Height), color);
        }
    }

}