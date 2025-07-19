using Microsoft.Xna.Framework;

namespace MonoGameLibrary.CollisionHelper;

public static class CollisionHelper
{
    /// <summary>
    /// Checks if a rectangle intersects with a circle
    /// </summary>
    /// <param name="rectangle">The rectangle to check</param>
    /// <param name="circleCenter">The center point of the circle</param>
    /// <param name="circleRadius">The radius of the circle</param>
    /// <returns>True if they intersect, false otherwise</returns>
    public static bool RectangleIntersectsCircle(Rectangle rectangle, Vector2 circleCenter, float circleRadius)
    {
        // Find the closest point on the rectangle to the circle's center
        Vector2 closestPoint = new Vector2(
            MathHelper.Clamp(circleCenter.X, rectangle.X, rectangle.X + rectangle.Width),
            MathHelper.Clamp(circleCenter.Y, rectangle.Y, rectangle.Y + rectangle.Height)
        );

        // Calculate the distance from the circle's center to the closest point
        float distance = Vector2.Distance(circleCenter, closestPoint);

        // Check if the distance is less than or equal to the radius
        return distance <= circleRadius;
    }

    /// <summary>
    /// Alternative method using squared distance (slightly more efficient)
    /// </summary>
    public static bool RectangleIntersectsCircleFast(Rectangle rectangle, Vector2 circleCenter, float circleRadius)
    {
        // Find the closest point on the rectangle to the circle's center
        Vector2 closestPoint = new Vector2(
            MathHelper.Clamp(circleCenter.X, rectangle.X, rectangle.X + rectangle.Width),
            MathHelper.Clamp(circleCenter.Y, rectangle.Y, rectangle.Y + rectangle.Height)
        );

        // Calculate the squared distance (avoids expensive square root operation)
        float distanceSquared = Vector2.DistanceSquared(circleCenter, closestPoint);
        float radiusSquared = circleRadius * circleRadius;

        // Check if the squared distance is less than or equal to the squared radius
        return distanceSquared <= radiusSquared;
    }
}
