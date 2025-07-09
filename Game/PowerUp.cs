using System.Numerics;

namespace AetherBreakout.Game
{
    public class PowerUp
    {
        public Vector2 Position;
        public PowerUpType Type;
        public string Text;
        public Vector2 Size;
        public bool IsText;

        public PowerUp(Vector2 position, PowerUpType type)
        {
            this.Position = position;
            this.Type = type;
            this.Size = new Vector2(10, 5); // Default size
            this.Text = ""; // Initialize Text to a non-null value

            switch (type)
            {
                case PowerUpType.WidenPaddle:
                    this.IsText = false;
                    this.Size = new Vector2(10, 5);
                    break;
                case PowerUpType.SplitBall:
                    this.IsText = true;
                    this.Text = "3x";
                    break;
                case PowerUpType.BigBall:
                    this.IsText = true;
                    this.Text = "BIG BALLS";
                    break;
                case PowerUpType.Juggernaut:
                    this.IsText = true;
                    this.Text = "I'M THE JUGGERNAUT";
                    break;
            }
        }
    }
}