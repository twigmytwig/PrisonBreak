using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Input;
using MonoGameLibrary.Player;
using MonoGameLibrary.RectangleCollider;
using PrisonBreak.Initializer;

namespace PrisonBreak;

public class Game1 : Core
{
    // Defines the slime animated sprite.
    private Player _prisoner;

    // Defines the bat animated sprite.
    private AnimatedSprite _cop;

    // Speed multiplier when moving.
    private const float MOVEMENT_SPEED = 5.0f;

    // Tracks the position of the bat.
    private Vector2 _copPosition;

    // Tracks the velocity of the bat.
    private Vector2 _copVelocity;

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

        // Initial bat position will be in the top left corner of the room
        _copPosition = new Vector2(_roomBounds.Left, _roomBounds.Top);

        // Assign the initial random velocity to the bat.
        AssignRandomCopVelocity();
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

        // Create the bat animated sprite from the atlas.
        _cop = atlas.CreateAnimatedSprite("cop-animation");
        _cop.Scale = new Vector2(4.0f, 4.0f);
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        // Update the slime animated sprite.
        //_prisoner.Update(gameTime);

        // Update the bat animated sprite.
        //_cop.Update(gameTime);

        // Check for keyboard input and handle it.
        CheckKeyboardInput();

        // Check for gamepad input and handle it.
        CheckGamePadInput();

        // Creating a smaller bounding rectangle for the prisoner (about 70% of sprite size for better gameplay)
        //_prisoner.UpdateCollider();

        // Keep prisoner within room bounds
        ConstrainPrisonerToBounds();

        // Calculate the new position of the bat based on the velocity
        Vector2 newCopPosition = _copPosition + _copVelocity;

        // Create a bounding circle for the bat
        //TODO: MAKE THIS ENTITY SIMILAR TO PLAYER
        int copspriteWidth = (int)(_cop.Width);
        int copspriteHeight = (int)(_cop.Height);
        int copcollisionWidth = (int)(32 * 4 * 0.5f);
        int copcollisionHeight = (int)(32 * 4 * 1f);
        _cop.RectangleCollider = new RectangleCollider(
            (int)(_copPosition.X + (copspriteWidth - copcollisionWidth) / 2),
            (int)(_copPosition.Y + (copspriteHeight - copcollisionHeight) / 2),
            copcollisionWidth,
            copcollisionHeight,
            true);

        Vector2 normal = Vector2.Zero;

        // Use distance based checks to determine if the bat is within the
        // bounds of the game screen, and if it is outside that screen edge,
        // reflect it about the screen edge normal
        if (_cop.RectangleCollider.rectangleCollider.Left < _roomBounds.Left)
        {
            normal.X = Vector2.UnitX.X;
            newCopPosition.X = _roomBounds.Left;
        }
        else if (_cop.RectangleCollider.rectangleCollider.Right > _roomBounds.Right)
        {
            normal.X = -Vector2.UnitX.X;
            newCopPosition.X = _roomBounds.Right - _cop.Width;
        }

        if (_cop.RectangleCollider.rectangleCollider.Top < _roomBounds.Top)
        {
            normal.Y = Vector2.UnitY.Y;
            newCopPosition.Y = _roomBounds.Top;
        }
        else if (_cop.RectangleCollider.rectangleCollider.Bottom > _roomBounds.Bottom)
        {
            normal.Y = -Vector2.UnitY.Y;
            newCopPosition.Y = _roomBounds.Bottom - _cop.Height;
        }

        // If the normal is anything but Vector2.Zero, this means the bat had
        // moved outside the screen edge so we should reflect it about the
        // normal.
        if (normal != Vector2.Zero)
        {
            _copVelocity = Vector2.Reflect(_copVelocity, normal);
        }

        _copPosition = newCopPosition;


        if (_prisoner._collider.rectangleCollider.Intersects(_cop.RectangleCollider.rectangleCollider))
        {
            int column = Random.Shared.Next(1, _tilemap.Columns - 1);
            int row = Random.Shared.Next(1, _tilemap.Rows - 1);

            // Change the bat position by setting the x and y values equal to
            // the column and row multiplied by the width and height.
            _copPosition = new Vector2(column * _cop.Width, row * _cop.Height);

            // Assign a new random velocity to the bat
            AssignRandomCopVelocity();
        }

        _prisoner._sprite.Update(gameTime);
        _cop.Update(gameTime);
        base.Update(gameTime);
    }

    private void AssignRandomCopVelocity()
    {
        // Generate a random angle
        float angle = (float)(Random.Shared.NextDouble() * Math.PI * 2);

        // Convert angle to a direction vector
        float x = (float)Math.Cos(angle);
        float y = (float)Math.Sin(angle);
        Vector2 direction = new Vector2(x, y);

        // Multiply the direction vector by the movement speed
        _copVelocity = direction * MOVEMENT_SPEED;
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
            _prisoner.UpdatePosition(new Vector2(_prisoner._position.X, _prisoner._position.Y - speed));
        }

        // if the S or Down keys are down, move the slime down on the screen.
        if (Input.Keyboard.IsKeyDown(Keys.S) || Input.Keyboard.IsKeyDown(Keys.Down))
        {
            _prisoner.UpdatePosition(new Vector2(_prisoner._position.X, _prisoner._position.Y + speed));
        }

        // If the A or Left keys are down, move the slime left on the screen.
        if (Input.Keyboard.IsKeyDown(Keys.A) || Input.Keyboard.IsKeyDown(Keys.Left))
        {
            _prisoner.UpdatePosition(new Vector2(_prisoner._position.X - speed, _prisoner._position.Y));
        }

        // If the D or Right keys are down, move the slime right on the screen.
        if (Input.Keyboard.IsKeyDown(Keys.D) || Input.Keyboard.IsKeyDown(Keys.Right))
        {
            _prisoner.UpdatePosition(new Vector2(_prisoner._position.X + speed, _prisoner._position.Y));
        }
    }

    private void ConstrainPrisonerToBounds()
    {
        Vector2 newPosition = _prisoner._position;
        bool positionChanged = false;

        // Calculate offsets based on collider being 50% width, 100% height, centered horizontally
        float colliderXOffset = _prisoner._sprite.Width * 0.25f;

        if (_prisoner._collider.rectangleCollider.Left < _roomBounds.Left)
        {
            newPosition.X = _roomBounds.Left - colliderXOffset;
            positionChanged = true;
        }
        else if (_prisoner._collider.rectangleCollider.Right > _roomBounds.Right)
        {
            newPosition.X = _roomBounds.Right - _prisoner._sprite.Width + colliderXOffset;
            positionChanged = true;
        }

        if (_prisoner._collider.rectangleCollider.Top < _roomBounds.Top)
        {
            newPosition.Y = _roomBounds.Top;
            positionChanged = true;
        }
        else if (_prisoner._collider.rectangleCollider.Bottom > _roomBounds.Bottom)
        {
            newPosition.Y = _roomBounds.Bottom - _prisoner._sprite.Height;
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
            Vector2 newPos = new Vector2(_prisoner._position.X + gamePadOne.LeftThumbStick.X * speed, _prisoner._position.Y - gamePadOne.LeftThumbStick.Y * speed);
            _prisoner.UpdatePosition(newPos);
        }
        else
        {
            // If DPadUp is down, move the slime up on the screen.
            if (gamePadOne.IsButtonDown(Buttons.DPadUp))
            {
                _prisoner.UpdatePosition(new Vector2(_prisoner._position.X, _prisoner._position.Y - speed));
            }

            // If DPadDown is down, move the slime down on the screen.
            if (gamePadOne.IsButtonDown(Buttons.DPadDown))
            {
                _prisoner.UpdatePosition(new Vector2(_prisoner._position.X, _prisoner._position.Y + speed));
            }

            // If DPapLeft is down, move the slime left on the screen.
            if (gamePadOne.IsButtonDown(Buttons.DPadLeft))
            {
                _prisoner.UpdatePosition(new Vector2(_prisoner._position.X - speed, _prisoner._position.Y));
            }

            // If DPadRight is down, move the slime right on the screen.
            if (gamePadOne.IsButtonDown(Buttons.DPadRight))
            {
                _prisoner.UpdatePosition(new Vector2(_prisoner._position.X + speed, _prisoner._position.Y));
            }
        }
    }


    protected override void Draw(GameTime gameTime)
    {
        // Create debug texture on first draw call when GraphicsDevice is ready
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
        _prisoner._sprite.Draw(SpriteBatch, _prisoner._position);

        // Draw the bat sprite.
        _cop.Draw(SpriteBatch, _copPosition);

        if (_prisoner._collider.DebugMode)
        {
            _prisoner._collider.Draw(SpriteBatch, Color.Red, _debugTexture, 2);
            _cop.RectangleCollider.Draw(SpriteBatch, Color.Blue, _debugCopTexture, 2);
        }

        // Always end the sprite batch when finished.
        SpriteBatch.End();

        base.Draw(gameTime);
    }
}
