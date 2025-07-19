using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PrisonBreak.Systems;

public interface IGameSystem
{
    void Initialize();
    void Update(GameTime gameTime);
    void Draw(SpriteBatch spriteBatch);
    void Shutdown();
}