using System.Numerics;

namespace AetherBreakout.Game
{
    public class Paddle
    {
        public Vector2 Position;
        public Vector2 Size;
        public Vector2 Velocity;

        public Paddle(Vector2 position, Vector2 size)
        {
            this.Position = position;
            this.Size = size;
            this.Velocity = Vector2.Zero;
        }
    }
}