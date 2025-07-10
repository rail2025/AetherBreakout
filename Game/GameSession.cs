using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AetherBreakout.Audio;

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
        private readonly AudioManager audioManager;
        private readonly Random random = new Random();

        private PowerUpType? activeBallPowerUpType = null;
        private PowerUpType? activePaddlePowerUpType = null;

        private float originalPaddleWidth;
        private Vector2 previousPaddlePosition;
        private const float OriginalBallRadius = 2.5f;

        private int bricksDestroyedThisStage = 0;
        private int? powerUpSpawnTarget1 = null;
        private int? powerUpSpawnTarget2 = null;
        private bool powerUp1Spawned = false;
        private bool powerUp2Spawned = false;

        public GameSession(Configuration config, AudioManager audioManager)
        {
            this.configuration = config;
            this.audioManager = audioManager;
            this.BallSpeedMultiplier = config.BallSpeedMultiplier <= 0.0f ? 1.0f : config.BallSpeedMultiplier;
            this.configuration.BallSpeedMultiplier = this.BallSpeedMultiplier;
            CurrentGameState = GameState.MainMenu;
        }

        public void StartNewGame()
        {
            Score = 0;
            Lives = 3;
            Level = 1;
            this.BallSpeedMultiplier = this.configuration.BallSpeedMultiplier;

            var paddleSize = new Vector2(25, 4);
            var paddlePosition = new Vector2((GameBoardSize.X - paddleSize.X) / 2, GameBoardSize.Y - paddleSize.Y - 10);
            ThePaddle = new Paddle(paddlePosition, paddleSize);
            this.originalPaddleWidth = ThePaddle.Size.X;
            this.previousPaddlePosition = ThePaddle.Position;

            TheGameBoard = new GameBoard();

            StartLevel();
            this.audioManager.PlayMusic("bgm1.mp3", false);
            CurrentGameState = GameState.InGame;
        }

        private void StartLevel()
        {
            DeactivateAllPowerUps();
            TheGameBoard?.CreateLevel(Level);
            bricksDestroyedThisStage = 0;
            powerUp1Spawned = false;
            powerUp2Spawned = false;
            SetPowerUpSpawnTargets();
            ResetBall();
        }

        private void SetPowerUpSpawnTargets()
        {
            powerUpSpawnTarget1 = random.Next(3, 11);
            if (Level >= 5)
            {
                powerUpSpawnTarget2 = random.Next(25, 36);
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
            var ballPosition = new Vector2(
                ThePaddle.Position.X + ThePaddle.Size.X / 2 - OriginalBallRadius,
                ThePaddle.Position.Y - OriginalBallRadius * 2 - 1
            );
            var ballVelocity = new Vector2(50, -50) * BallSpeedMultiplier;
            Balls.Add(new Ball(ballPosition, ballVelocity, OriginalBallRadius));
        }

        public void Update(float deltaTime)
        {
            if (CurrentGameState != GameState.InGame || ThePaddle == null || TheGameBoard == null) return;

            // Calculate paddle velocity for the frame
            if (deltaTime > 0)
            {
                ThePaddle.Velocity = (ThePaddle.Position - this.previousPaddlePosition) / deltaTime;
                this.previousPaddlePosition = ThePaddle.Position;
            }

            foreach (var ball in Balls.ToList())
            {
                ball.Position += ball.Velocity * deltaTime;

                if (ball.Position.X <= 0) { ball.Velocity.X *= -1; ball.Position.X = 0; audioManager.PlaySfx("bounce.wav"); }
                if (ball.Position.X + ball.Radius * 2 >= GameBoardSize.X) { ball.Velocity.X *= -1; ball.Position.X = GameBoardSize.X - ball.Radius * 2; audioManager.PlaySfx("bounce.wav"); }
                if (ball.Position.Y <= 0) { ball.Velocity.Y *= -1; ball.Position.Y = 0; audioManager.PlaySfx("bounce.wav"); }

                if (ball.Position.Y + ball.Radius * 2 >= GameBoardSize.Y)
                {
                    Balls.Remove(ball);
                    if (!Balls.Any())
                    {
                        Lives--;
                        if (Lives <= 0)
                        {
                            if (this.Score > this.configuration.HighScore)
                            {
                                this.configuration.HighScore = this.Score;
                                this.configuration.Save();
                            }
                            CurrentGameState = GameState.GameOver;
                            audioManager.FadeMusic(0f, 1.5f, () => audioManager.StopMusic());
                        }
                        else
                        {
                            DeactivateAllPowerUps();
                            ResetBall();
                        }
                    }
                    continue;
                }

                var ballRect = new RectangleF(ball.Position, new Vector2(ball.Radius * 2, ball.Radius * 2));
                var paddleRect = new RectangleF(ThePaddle.Position, ThePaddle.Size);

                // --- Enhanced Paddle Collision Logic ---
                if (ballRect.IntersectsWith(paddleRect))
                {
                    // Prevent ball from getting stuck inside the paddle
                    ball.Position.Y = ThePaddle.Position.Y - (ball.Radius * 2);

                    // Always bounce upwards
                    ball.Velocity.Y = -Math.Abs(ball.Velocity.Y);

                    // Calculate where the ball hit the paddle (-1 for left edge, 1 for right edge)
                    var paddleCenter = ThePaddle.Position.X + ThePaddle.Size.X / 2;
                    var ballImpactX = ball.Position.X + ball.Radius;
                    var offset = ballImpactX - paddleCenter;
                    var normalizedOffset = offset / (ThePaddle.Size.X / 2);

                    // Add velocity based on impact position
                    float positionInfluence = 50f;
                    ball.Velocity.X += normalizedOffset * positionInfluence;

                    // Add velocity from the paddle's own movement
                    float paddleInfluence = 0.4f;
                    ball.Velocity.X += ThePaddle.Velocity.X * paddleInfluence;

                    // Clamp the horizontal velocity to prevent extreme angles
                    ball.Velocity.X = Math.Clamp(ball.Velocity.X, -80f, 80f);

                    audioManager.PlaySfx("bounce.wav");
                }

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
                        audioManager.PlaySfx("pop.wav");
                        bricksDestroyedThisStage++;
                        CheckForPowerUpSpawn(brick.Position);
                        break;
                    }
                }
            }

            foreach (var powerUp in ActivePowerUps.ToList())
            {
                powerUp.Position.Y += 25 * deltaTime;
                var powerUpRect = new RectangleF(powerUp.Position, powerUp.Size);
                if (ThePaddle != null)
                {
                    var paddleRect = new RectangleF(ThePaddle.Position, ThePaddle.Size);
                    if (powerUpRect.IntersectsWith(paddleRect))
                    {
                        ActivatePowerUp(powerUp);
                        ActivePowerUps.Remove(powerUp);
                        audioManager.PlaySfx("powerup.wav");
                    }
                    else if (powerUp.Position.Y > GameBoardSize.Y)
                    {
                        ActivePowerUps.Remove(powerUp);
                    }
                }
            }

            if (TheGameBoard != null && !TheGameBoard.Bricks.Any(b => b.IsActive))
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
            var powerUpType = (PowerUpType)random.Next(Enum.GetValues(typeof(PowerUpType)).Length);
            ActivePowerUps.Add(new PowerUp(position, powerUpType));
        }

        private void ActivatePowerUp(PowerUp powerUp)
        {
            if (powerUp.Type == PowerUpType.WidenPaddle)
            {
                DeactivatePaddlePowerUp();
                activePaddlePowerUpType = powerUp.Type;
                if (ThePaddle != null) ThePaddle.Size.X = this.originalPaddleWidth * 3;
            }
            else
            {
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
                    foreach (var ball in Balls) ball.Radius = OriginalBallRadius;
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
            audioManager.StopMusic();
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