using System.Numerics;

namespace AetherBreakout.Game
{
    public class Ball
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Radius;

        public Ball(Vector2 position, Vector2 velocity, float radius)
        {
            this.Position = position;
            this.Velocity = velocity;
            this.Radius = radius;
        }
    }
}