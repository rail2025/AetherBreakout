using System.Numerics;

namespace AetherBreakout.Game
{
    public class Brick
    {
        public Vector2 Position;
        public Vector2 Size;
        public uint Color;
        public bool IsActive = true;

        public Brick(Vector2 position, Vector2 size, uint color)
        {
            this.Position = position;
            this.Size = size;
            this.Color = color;
        }
    }
}