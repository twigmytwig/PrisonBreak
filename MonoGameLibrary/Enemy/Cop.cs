using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;

namespace MonoGameLibrary.Enemy;

public class Cop : Enemy
{
    private Vector2 _velocity;
    private const float MOVEMENT_SPEED = 5.0f;
    
    public Vector2 Velocity => _velocity;

    public Cop() : base()
    {
    }

    public Cop(Vector2 position, AnimatedSprite sprite, bool debugMode, Vector2 scale) 
        : base(position, sprite, debugMode, scale)
    {
        AssignRandomVelocity();
    }

    public override void Update(GameTime gameTime)
    {
        // Calculate new position based on velocity
        Vector2 newPosition = _position + _velocity;
        UpdatePosition(newPosition);
        
        // Update sprite animation
        base.Update(gameTime);
    }

    public void ConstrainToBounds(Rectangle roomBounds)
    {
        Vector2 newPosition = _position;
        Vector2 normal = Vector2.Zero;

        // Check bounds and calculate reflection normal
        if (_collider.rectangleCollider.Left < roomBounds.Left)
        {
            normal.X = Vector2.UnitX.X;
            newPosition.X = roomBounds.Left;
        }
        else if (_collider.rectangleCollider.Right > roomBounds.Right)
        {
            normal.X = -Vector2.UnitX.X;
            newPosition.X = roomBounds.Right - _sprite.Width;
        }

        if (_collider.rectangleCollider.Top < roomBounds.Top)
        {
            normal.Y = Vector2.UnitY.Y;
            newPosition.Y = roomBounds.Top;
        }
        else if (_collider.rectangleCollider.Bottom > roomBounds.Bottom)
        {
            normal.Y = -Vector2.UnitY.Y;
            newPosition.Y = roomBounds.Bottom - _sprite.Height;
        }

        // If we hit a boundary, reflect velocity and update position
        if (normal != Vector2.Zero)
        {
            _velocity = Vector2.Reflect(_velocity, normal);
            UpdatePosition(newPosition);
        }
    }

    public void TeleportToRandomPosition(Rectangle roomBounds, float tileWidth, float tileHeight)
    {
        // Calculate available tile positions within bounds
        int columns = (int)(roomBounds.Width / tileWidth);
        int rows = (int)(roomBounds.Height / tileHeight);
        
        int column = Random.Shared.Next(1, columns - 1);
        int row = Random.Shared.Next(1, rows - 1);

        Vector2 newPosition = new Vector2(
            roomBounds.Left + column * tileWidth, 
            roomBounds.Top + row * tileHeight
        );
        
        UpdatePosition(newPosition);
        AssignRandomVelocity();
    }

    private void AssignRandomVelocity()
    {
        // Generate a random angle
        float angle = (float)(Random.Shared.NextDouble() * Math.PI * 2);

        // Convert angle to a direction vector
        float x = (float)Math.Cos(angle);
        float y = (float)Math.Sin(angle);
        Vector2 direction = new Vector2(x, y);

        // Multiply the direction vector by the movement speed
        _velocity = direction * MOVEMENT_SPEED;
    }
}