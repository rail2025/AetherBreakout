using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace AetherBreakout.Game
{
    public class GameSession
    {
        public GameState CurrentGameState { get; private set; }
        public int Score { get; private set; }
        public int Lives { get; private set; }
        public int Level { get; private set; }
        public float BallSpeedMultiplier { get; set; }

        public Paddle? ThePaddle;
        public Ball? TheBall;
        public GameBoard? TheGameBoard;
        public static readonly Vector2 GameBoardSize = new(100, 133.3f);

        public GameSession(Configuration config)
        {
            this.BallSpeedMultiplier = config.BallSpeedMultiplier;
            CurrentGameState = GameState.MainMenu;
        }

        public void StartNewGame()
        {
            Score = 0;
            Lives = 3;
            Level = 1;

            var paddleSize = new Vector2(25, 4);
            var paddlePosition = new Vector2((GameBoardSize.X - paddleSize.X) / 2, GameBoardSize.Y - paddleSize.Y - 10);
            ThePaddle = new Paddle(paddlePosition, paddleSize);

            TheGameBoard = new GameBoard();
            TheGameBoard.CreateLevel(1);

            ResetBall();

            CurrentGameState = GameState.InGame;
        }

        private void ResetBall()
        {
            if (ThePaddle == null) return;

            float ballRadius = 2.5f;
            var ballPosition = new Vector2(
                ThePaddle.Position.X + ThePaddle.Size.X / 2,
                ThePaddle.Position.Y - ballRadius - 1
            );
            var ballVelocity = new Vector2(50, -50) * BallSpeedMultiplier;
            TheBall = new Ball(ballPosition, ballVelocity, ballRadius);
        }

        public void Update(float deltaTime)
        {
            if (CurrentGameState != GameState.InGame || TheBall == null || ThePaddle == null || TheGameBoard == null) return;

            TheBall.Position += TheBall.Velocity * deltaTime;

            // Wall Collision
            if (TheBall.Position.X <= 0) { TheBall.Velocity.X *= -1; TheBall.Position.X = 0; }
            if (TheBall.Position.X + TheBall.Radius * 2 >= GameBoardSize.X) { TheBall.Velocity.X *= -1; TheBall.Position.X = GameBoardSize.X - TheBall.Radius * 2; }
            if (TheBall.Position.Y <= 0) { TheBall.Velocity.Y *= -1; TheBall.Position.Y = 0; }

            // Bottom Wall (lose life)
            if (TheBall.Position.Y + TheBall.Radius * 2 >= GameBoardSize.Y)
            {
                Lives--;
                if (Lives > 0) ResetBall();
                else CurrentGameState = GameState.GameOver;
                return;
            }

            // Paddle Collision
            var ballRect = new RectangleF(TheBall.Position, new Vector2(TheBall.Radius * 2, TheBall.Radius * 2));
            var paddleRect = new RectangleF(ThePaddle.Position, ThePaddle.Size);
            if (ballRect.IntersectsWith(paddleRect))
            {
                TheBall.Position.Y = ThePaddle.Position.Y - (TheBall.Radius * 2);
                TheBall.Velocity.Y *= -1;
            }

            // Brick Collision
            foreach (var brick in TheGameBoard.Bricks.Where(b => b.IsActive))
            {
                var brickRect = new RectangleF(brick.Position, brick.Size);
                if (ballRect.IntersectsWith(brickRect))
                {
                    brick.IsActive = false;
                    TheBall.Velocity.Y *= -1;
                    Score += 10;
                    break;
                }
            }

            TheGameBoard.Bricks.RemoveAll(b => !b.IsActive);

            // Stage Clear Condition
            if (!TheGameBoard.Bricks.Any())
            {
                Level++;
                Score += 1000;
                TheGameBoard.CreateLevel(Level);
                ResetBall();
            }
        }

        public void GoToMainMenu()
        {
            ThePaddle = null; TheBall = null; TheGameBoard = null;
            CurrentGameState = GameState.MainMenu;
        }

        public void MovePaddle(float newX)
        {
            if (ThePaddle == null) return;
            ThePaddle.Position.X = Math.Clamp(newX, 0, GameBoardSize.X - ThePaddle.Size.X);
        }
    }

    public struct RectangleF
    {
        private float _x, _y, _right, _bottom;
        public RectangleF(Vector2 position, Vector2 size) { _x = position.X; _y = position.Y; _right = position.X + size.X; _bottom = position.Y + size.Y; }
        public bool IntersectsWith(RectangleF other) { return _x < other._right && _right > other._x && _y < other._bottom && _bottom > other._y; }
    }
}