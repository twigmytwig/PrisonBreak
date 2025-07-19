using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Input;
using MonoGameLibrary.Player;
using MonoGameLibrary.Enemy;
using PrisonBreak.Initializer;

namespace PrisonBreak;

public class Game1 : Core
{
    // Defines the slime animated sprite.
    private Player _prisoner;

    // Defines the cop enemy.
    private Cop _cop;

    // Speed multiplier when moving.
    private const float MOVEMENT_SPEED = 5.0f;

    // Defines the tilemap to draw.
    private Tilemap _tilemap;

    // Defines the bounds of the room that the slime and bat are contained within.
    private Rectangle _roomBounds;

    // 1x1 white texture for drawing debug rectangles
    private Texture2D _debugTexture;
    private Texture2D _debugCopTexture;

    public Game1() : base("Dungeon Slime", 1920, 1080, false)
    {

    }

    protected override void Initialize()
    {
        base.Initialize();

        Rectangle screenBounds = GraphicsDevice.PresentationParameters.Bounds;

        _roomBounds = new Rectangle(
             (int)_tilemap.TileWidth,
             (int)_tilemap.TileHeight,
             screenBounds.Width - (int)_tilemap.TileWidth * 2,
             screenBounds.Height - (int)_tilemap.TileHeight * 2
         );

    }

    protected override void LoadContent()
    {
        // Create the texture atlas from the XML configuration file
        TextureAtlas atlas = TextureAtlas.FromFile(Content, "images/atlas-definition.xml");

        // Create the tilemap from the XML configuration file.
        _tilemap = Tilemap.FromFile(Content, "images/tilemap-definition.xml");
        _tilemap.Scale = new Vector2(4.0f, 4.0f);

        int centerRow = _tilemap.Rows / 2;
        int centerColumn = _tilemap.Columns / 2;

        _prisoner = InitializeGameObjects.InitPlayer(
            isDebug: true,
            pos: new Vector2(centerColumn * _tilemap.TileWidth, centerRow * _tilemap.TileHeight),
            atlas: atlas,
            animationName: "prisoner-animation",
            scale: new Vector2(4.0f, 4.0f));

        // Create the cop enemy from the atlas.
        AnimatedSprite copSprite = atlas.CreateAnimatedSprite("cop-animation");
        _cop = new Cop(
            new Vector2(_roomBounds.Left, _roomBounds.Top),
            copSprite,
            true,
            new Vector2(4.0f, 4.0f)
        );

    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        // Check for keyboard input and handle it.
        CheckKeyboardInput();

        // Check for gamepad input and handle it.
        CheckGamePadInput();

        // Keep prisoner within room bounds
        ConstrainPrisonerToBounds();

        // Update cop movement and handle bounds collision
        _cop.Update(gameTime);
        _cop.ConstrainToBounds(_roomBounds);

        // Check for player-cop collision
        if (_prisoner.GetBounds().Intersects(_cop.GetBounds()))
        {
            // Teleport cop to random position
            _cop.TeleportToRandomPosition(_roomBounds, _tilemap.TileWidth, _tilemap.TileHeight);
        }

        _prisoner.Update(gameTime);
        base.Update(gameTime);
    }


    private void CheckKeyboardInput()
    {
        // If the space key is held down, the movement speed increases by 1.5
        float speed = MOVEMENT_SPEED;
        if (Input.Keyboard.IsKeyDown(Keys.Space))
        {
            speed *= 1.5f;
        }

        // If the W or Up keys are down, move the slime up on the screen.
        if (Input.Keyboard.IsKeyDown(Keys.W) || Input.Keyboard.IsKeyDown(Keys.Up))
        {
            _prisoner.UpdatePosition(new Vector2(_prisoner.Position.X, _prisoner.Position.Y - speed));
        }

        // if the S or Down keys are down, move the slime down on the screen.
        if (Input.Keyboard.IsKeyDown(Keys.S) || Input.Keyboard.IsKeyDown(Keys.Down))
        {
            _prisoner.UpdatePosition(new Vector2(_prisoner.Position.X, _prisoner.Position.Y + speed));
        }

        // If the A or Left keys are down, move the slime left on the screen.
        if (Input.Keyboard.IsKeyDown(Keys.A) || Input.Keyboard.IsKeyDown(Keys.Left))
        {
            _prisoner.UpdatePosition(new Vector2(_prisoner.Position.X - speed, _prisoner.Position.Y));
        }

        // If the D or Right keys are down, move the slime right on the screen.
        if (Input.Keyboard.IsKeyDown(Keys.D) || Input.Keyboard.IsKeyDown(Keys.Right))
        {
            _prisoner.UpdatePosition(new Vector2(_prisoner.Position.X + speed, _prisoner.Position.Y));
        }
    }

    private void ConstrainPrisonerToBounds()
    {
        Vector2 newPosition = _prisoner.Position;
        bool positionChanged = false;

        // Calculate offsets based on collider being 50% width, 100% height, centered horizontally
        float colliderXOffset = _prisoner.Sprite.Width * 0.25f;

        if (_prisoner.Collider.rectangleCollider.Left < _roomBounds.Left)
        {
            newPosition.X = _roomBounds.Left - colliderXOffset;
            positionChanged = true;
        }
        else if (_prisoner.Collider.rectangleCollider.Right > _roomBounds.Right)
        {
            newPosition.X = _roomBounds.Right - _prisoner.Sprite.Width + colliderXOffset;
            positionChanged = true;
        }

        if (_prisoner.Collider.rectangleCollider.Top < _roomBounds.Top)
        {
            newPosition.Y = _roomBounds.Top;
            positionChanged = true;
        }
        else if (_prisoner.Collider.rectangleCollider.Bottom > _roomBounds.Bottom)
        {
            newPosition.Y = _roomBounds.Bottom - _prisoner.Sprite.Height;
            positionChanged = true;
        }

        // Update position if it was constrained
        if (positionChanged)
        {
            _prisoner.UpdatePosition(newPosition);
        }
    }

    private void CheckGamePadInput()
    {
        GamePadInfo gamePadOne = Input.GamePads[(int)PlayerIndex.One];

        // If the A button is held down, the movement speed increases by 1.5
        // and the gamepad vibrates as feedback to the player.
        float speed = MOVEMENT_SPEED;
        if (gamePadOne.IsButtonDown(Buttons.A))
        {
            speed *= 1.5f;
            GamePad.SetVibration(PlayerIndex.One, 1.0f, 1.0f);
        }
        else
        {
            GamePad.SetVibration(PlayerIndex.One, 0.0f, 0.0f);
        }

        // Check thumbstick first since it has priority over which gamepad input
        // is movement.  It has priority since the thumbstick values provide a
        // more granular analog value that can be used for movement.
        if (gamePadOne.LeftThumbStick != Vector2.Zero)
        {
            Vector2 newPos = new Vector2(_prisoner.Position.X + gamePadOne.LeftThumbStick.X * speed, _prisoner.Position.Y - gamePadOne.LeftThumbStick.Y * speed);
            _prisoner.UpdatePosition(newPos);
        }
        else
        {
            // If DPadUp is down, move the slime up on the screen.
            if (gamePadOne.IsButtonDown(Buttons.DPadUp))
            {
                _prisoner.UpdatePosition(new Vector2(_prisoner.Position.X, _prisoner.Position.Y - speed));
            }

            // If DPadDown is down, move the slime down on the screen.
            if (gamePadOne.IsButtonDown(Buttons.DPadDown))
            {
                _prisoner.UpdatePosition(new Vector2(_prisoner.Position.X, _prisoner.Position.Y + speed));
            }

            // If DPapLeft is down, move the slime left on the screen.
            if (gamePadOne.IsButtonDown(Buttons.DPadLeft))
            {
                _prisoner.UpdatePosition(new Vector2(_prisoner.Position.X - speed, _prisoner.Position.Y));
            }

            // If DPadRight is down, move the slime right on the screen.
            if (gamePadOne.IsButtonDown(Buttons.DPadRight))
            {
                _prisoner.UpdatePosition(new Vector2(_prisoner.Position.X + speed, _prisoner.Position.Y));
            }
        }
    }


    protected override void Draw(GameTime gameTime)
    {
        // Create debug textures once when GraphicsDevice is ready
        if (_debugTexture == null)
        {
            _debugTexture = new Texture2D(GraphicsDevice, 1, 1);
            _debugTexture.SetData(new[] { Color.White });

            _debugCopTexture = new Texture2D(GraphicsDevice, 1, 1);
            _debugCopTexture.SetData(new[] { Color.White });
        }

        // Clear the back buffer.
        GraphicsDevice.Clear(Color.CornflowerBlue);

        // Begin the sprite batch to prepare for rendering.
        SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

        // Draw the tilemap.
        _tilemap.Draw(SpriteBatch);

        // Draw the prisoner sprite at its position
        _prisoner.Draw(SpriteBatch);

        // Draw the cop sprite.
        _cop.Draw(SpriteBatch);

        if (_prisoner.DebugMode)
        {
            _prisoner.Collider.Draw(SpriteBatch, Color.Red, _debugTexture, 2);
            _cop.Collider.Draw(SpriteBatch, Color.Blue, _debugCopTexture, 2);
        }

        // Always end the sprite batch when finished.
        SpriteBatch.End();

        base.Draw(gameTime);
    }
}
