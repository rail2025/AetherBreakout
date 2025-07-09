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
        public List<Ball> Balls { get; private set; } = new();
        public GameBoard? TheGameBoard;
        public static readonly Vector2 GameBoardSize = new(100, 133.3f);
        public List<PowerUp> ActivePowerUps { get; private set; } = new();

        private readonly Configuration configuration;

        private PowerUpType? activeBallPowerUpType = null;
        private PowerUpType? activePaddlePowerUpType = null;

        private float originalPaddleWidth;

        private int bricksDestroyedThisStage = 0;
        private int? powerUpSpawnTarget1 = null;
        private int? powerUpSpawnTarget2 = null;
        private bool powerUp1Spawned = false;
        private bool powerUp2Spawned = false;

        public GameSession(Configuration config)
        {
            this.configuration = config;
            this.BallSpeedMultiplier = config.BallSpeedMultiplier;
            CurrentGameState = GameState.MainMenu;
        }

        public void StartNewGame()
        {
            Score = 0;
            Lives = 3;
            Level = 1;
            this.BallSpeedMultiplier = 1.0f;

            var paddleSize = new Vector2(25, 4);
            var paddlePosition = new Vector2((GameBoardSize.X - paddleSize.X) / 2, GameBoardSize.Y - paddleSize.Y - 10);
            ThePaddle = new Paddle(paddlePosition, paddleSize);
            this.originalPaddleWidth = ThePaddle.Size.X; // Store original width

            TheGameBoard = new GameBoard();

            StartLevel();
            CurrentGameState = GameState.InGame;
        }

        private void StartLevel()
        {
            TheGameBoard?.CreateLevel(Level);
            bricksDestroyedThisStage = 0;
            powerUp1Spawned = false;
            powerUp2Spawned = false;
            SetPowerUpSpawnTargets();
            ResetBall();
            DeactivateAllPowerUps();
        }

        private void SetPowerUpSpawnTargets()
        {
            var random = new Random();
            powerUpSpawnTarget1 = random.Next(3, 11); // First spawn between 3rd and 10th brick

            if (Level >= 5)
            {
                powerUpSpawnTarget2 = random.Next(25, 36); // Second spawn between 25th and 35th
            }
            else
            {
                powerUpSpawnTarget2 = null;
            }
        }


        private void ResetBall()
        {
            if (ThePaddle == null) return;
            Balls.Clear();
            float ballRadius = 2.5f;
            var ballPosition = new Vector2(
                ThePaddle.Position.X + ThePaddle.Size.X / 2 - ballRadius,
                ThePaddle.Position.Y - ballRadius * 2 - 1
            );
            var ballVelocity = new Vector2(50, -50) * BallSpeedMultiplier;
            Balls.Add(new Ball(ballPosition, ballVelocity, ballRadius));
        }

        public void Update(float deltaTime)
        {
            if (CurrentGameState != GameState.InGame || ThePaddle == null || TheGameBoard == null) return;

            foreach (var ball in Balls.ToList())
            {
                ball.Position += ball.Velocity * deltaTime;

                // Wall Collision
                if (ball.Position.X <= 0) { ball.Velocity.X *= -1; ball.Position.X = 0; }
                if (ball.Position.X + ball.Radius * 2 >= GameBoardSize.X) { ball.Velocity.X *= -1; ball.Position.X = GameBoardSize.X - ball.Radius * 2; }
                if (ball.Position.Y <= 0) { ball.Velocity.Y *= -1; ball.Position.Y = 0; }

                // Bottom Wall (lose life)
                if (ball.Position.Y + ball.Radius * 2 >= GameBoardSize.Y)
                {
                    Balls.Remove(ball);
                    if (!Balls.Any())
                    {
                        Lives--;
                        if (Lives > 0)
                        {
                            ResetBall();
                        }
                        else
                        {
                            if (this.Score > this.configuration.HighScore)
                            {
                                this.configuration.HighScore = this.Score;
                                this.configuration.Save();
                            }
                            CurrentGameState = GameState.GameOver;
                        }
                    }
                    continue;
                }

                // Paddle Collision
                var ballRect = new RectangleF(ball.Position, new Vector2(ball.Radius * 2, ball.Radius * 2));
                var paddleRect = new RectangleF(ThePaddle.Position, ThePaddle.Size);
                if (ballRect.IntersectsWith(paddleRect))
                {
                    ball.Position.Y = ThePaddle.Position.Y - (ball.Radius * 2);
                    ball.Velocity.Y *= -1;
                }

                // Brick Collision
                foreach (var brick in TheGameBoard.Bricks.Where(b => b.IsActive))
                {
                    var brickRect = new RectangleF(brick.Position, brick.Size);
                    if (ballRect.IntersectsWith(brickRect))
                    {
                        if (!ball.IsJuggernaut)
                        {
                            ball.Velocity.Y *= -1;
                        }
                        brick.IsActive = false;
                        Score += 10;
                        bricksDestroyedThisStage++;
                        CheckForPowerUpSpawn(brick.Position);
                        break;
                    }
                }
            }
            // Update and check for power-up collection
            foreach (var powerUp in ActivePowerUps.ToList())
            {
                powerUp.Position.Y += 50 * deltaTime;
                var powerUpRect = new RectangleF(powerUp.Position, powerUp.Size);
                var paddleRect = new RectangleF(ThePaddle.Position, ThePaddle.Size);

                if (powerUpRect.IntersectsWith(paddleRect))
                {
                    ActivatePowerUp(powerUp);
                    ActivePowerUps.Remove(powerUp);
                }
                else if (powerUp.Position.Y > GameBoardSize.Y)
                {
                    ActivePowerUps.Remove(powerUp);
                }
            }

            TheGameBoard.Bricks.RemoveAll(b => !b.IsActive);

            // Stage Clear Condition
            if (!TheGameBoard.Bricks.Any())
            {
                Level++;
                this.BallSpeedMultiplier *= 1.01f;
                StartLevel();
            }
        }

        private void CheckForPowerUpSpawn(Vector2 position)
        {
            if (bricksDestroyedThisStage == powerUpSpawnTarget1 && !powerUp1Spawned)
            {
                TrySpawnPowerUp(position);
                powerUp1Spawned = true;
            }
            else if (bricksDestroyedThisStage == powerUpSpawnTarget2 && !powerUp2Spawned)
            {
                TrySpawnPowerUp(position);
                powerUp2Spawned = true;
            }
        }


        private void TrySpawnPowerUp(Vector2 position)
        {
            var random = new Random();
            var powerUpType = (PowerUpType)random.Next(Enum.GetValues(typeof(PowerUpType)).Length);
            ActivePowerUps.Add(new PowerUp(position, powerUpType));
        }

        private void ActivatePowerUp(PowerUp powerUp)
        {
            switch (powerUp.Type)
            {
                case PowerUpType.WidenPaddle:
                    activePaddlePowerUpType = powerUp.Type;
                    if (ThePaddle != null) ThePaddle.Size.X = this.originalPaddleWidth * 3;
                    break;

                case PowerUpType.SplitBall:
                case PowerUpType.BigBall:
                case PowerUpType.Juggernaut:
                    DeactivateBallPowerUp();
                    activeBallPowerUpType = powerUp.Type;
                    switch (powerUp.Type)
                    {
                        case PowerUpType.SplitBall:
                            if (Balls.Any())
                            {
                                var originalBall = Balls.First();
                                Balls.Add(new Ball(originalBall.Position, new Vector2(originalBall.Velocity.X, originalBall.Velocity.Y), originalBall.Radius));
                                Balls.Add(new Ball(originalBall.Position, new Vector2(-originalBall.Velocity.X, originalBall.Velocity.Y), originalBall.Radius));
                            }
                            break;
                        case PowerUpType.BigBall:
                            foreach (var ball in Balls) ball.Radius *= 3;
                            break;
                        case PowerUpType.Juggernaut:
                            foreach (var ball in Balls) ball.IsJuggernaut = true;
                            break;
                    }
                    break;
            }
        }

        private void DeactivateAllPowerUps()
        {
            DeactivateBallPowerUp();
            DeactivatePaddlePowerUp();
        }

        private void DeactivateBallPowerUp()
        {
            if (activeBallPowerUpType == null) return;

            switch (activeBallPowerUpType)
            {
                case PowerUpType.SplitBall:
                    if (Balls.Count > 1)
                    {
                        var firstBall = Balls.First();
                        Balls.Clear();
                        Balls.Add(firstBall);
                    }
                    break;
                case PowerUpType.BigBall:
                    foreach (var ball in Balls) ball.Radius /= 3;
                    break;
                case PowerUpType.Juggernaut:
                    foreach (var ball in Balls) ball.IsJuggernaut = false;
                    break;
            }
            activeBallPowerUpType = null;
        }

        private void DeactivatePaddlePowerUp()
        {
            if (activePaddlePowerUpType == null) return;

            switch (activePaddlePowerUpType)
            {
                case PowerUpType.WidenPaddle:
                    if (ThePaddle != null) ThePaddle.Size.X = this.originalPaddleWidth;
                    break;
            }
            activePaddlePowerUpType = null;
        }

        public void GoToMainMenu()
        {
            if (this.Score > this.configuration.HighScore)
            {
                this.configuration.HighScore = this.Score;
                this.configuration.Save();
            }

            ThePaddle = null;
            Balls.Clear();
            TheGameBoard = null;
            DeactivateAllPowerUps();
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