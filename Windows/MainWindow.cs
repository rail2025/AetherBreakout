using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Interface.Utility;
using ImGuiNET;
using AetherBreakout.Game;
using AetherBreakout.UI;

namespace AetherBreakout.Windows
{
    public class MainWindow : Window, IDisposable
    {
        private readonly Plugin plugin;
        private readonly GameSession gameSession;
        private readonly TextureManager textureManager;

        public static readonly Vector2 BaseWindowSize = new(600, 800);

        public MainWindow(Plugin plugin, GameSession gameSession, TextureManager textureManager) : base("AetherBreakout")
        {
            this.SizeCondition = ImGuiCond.FirstUseEver;
            this.Flags |= ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar;
            this.plugin = plugin;
            this.gameSession = gameSession;
            this.textureManager = textureManager;
        }

        public override void PreDraw()
        {
            this.Size = BaseWindowSize * ImGuiHelpers.GlobalScale;
        }

        public void Dispose() { }

        public override void Draw()
        {
            gameSession.Update(ImGui.GetIO().DeltaTime);

            switch (gameSession.CurrentGameState)
            {
                case GameState.MainMenu:
                    DrawMainMenu();
                    break;
                case GameState.InGame:
                    DrawGame();
                    break;
                case GameState.GameOver:
                    DrawGameOver();
                    break;
            }
        }

        private void DrawMainMenu()
        {
            var windowSize = ImGui.GetWindowSize();
            var scale = ImGuiHelpers.GlobalScale;

            // --- Center Title Text ---
            var titleText = "AetherBreakout";
            ImGui.SetWindowFontScale(2.5f);
            var titleTextSize = ImGui.CalcTextSize(titleText);
            var titlePos = new Vector2((windowSize.X - titleTextSize.X) * 0.5f, windowSize.Y * 0.3f);
            ImGui.SetCursorPos(titlePos);
            ImGui.Text(titleText);
            ImGui.SetWindowFontScale(1f);

            // --- Center Buttons ---
            var buttonSize = new Vector2(140 * scale, 40 * scale);
            var buttonX = (windowSize.X - buttonSize.X) * 0.5f;
            var currentY = titlePos.Y + titleTextSize.Y + 40 * scale;

            // Start Game Button
            ImGui.SetCursorPos(new Vector2(buttonX, currentY));
            if (ImGui.Button("Start Game", buttonSize))
            {
                gameSession.StartNewGame();
            }
            currentY += buttonSize.Y + (10 * scale);

            // Settings Button
            ImGui.SetCursorPos(new Vector2(buttonX, currentY));
            if (ImGui.Button("Settings", buttonSize))
            {
                plugin.ToggleConfigUI();
            }
            currentY += buttonSize.Y + (10 * scale);

            // About Button
            ImGui.SetCursorPos(new Vector2(buttonX, currentY));
            if (ImGui.Button("About", buttonSize))
            {
                plugin.ToggleAboutUI();
            }
        }

        private void DrawGame()
        {
            var contentMin = ImGui.GetWindowContentRegionMin() + ImGui.GetWindowPos();
            var contentSize = ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin();
            var drawList = ImGui.GetWindowDrawList();

            float pixelsPerUnit = contentSize.X / GameSession.GameBoardSize.X;

            HandleInput(contentMin, pixelsPerUnit);

            // HUD Elements
            var highScoreText = $"{plugin.Configuration.HighScore:D3}";
            var scoreText = $"{gameSession.Score:D3}";
            var livesText = $"{gameSession.Lives}";
            var levelText = $"{gameSession.Level}";
            float hudFontSize = 4 * ImGuiHelpers.GlobalScale;

            // Draw High Score
            UIManager.DrawBlockyText(drawList, highScoreText, contentMin + new Vector2(20, 10), hudFontSize, ImGui.GetColorU32(new Vector4(0.8f, 0.8f, 0.8f, 1))); // Grey for high score

            // Draw Current Score
            float highScoreWidth = UIManager.GetBlockyTextWidth(highScoreText, hudFontSize);
            var scorePos = contentMin + new Vector2(20 + highScoreWidth + 40 * ImGuiHelpers.GlobalScale, 10);
            UIManager.DrawBlockyText(drawList, scoreText, scorePos, hudFontSize, ImGui.GetColorU32(new Vector4(1, 1, 1, 1)));

            // Draw Lives and Level
            UIManager.DrawBlockyText(drawList, livesText, contentMin + new Vector2(contentSize.X - 120 * ImGuiHelpers.GlobalScale, 10), hudFontSize, ImGui.GetColorU32(new Vector4(1, 1, 1, 1)));
            UIManager.DrawBlockyText(drawList, levelText, contentMin + new Vector2(contentSize.X - 60 * ImGuiHelpers.GlobalScale, 10), hudFontSize, ImGui.GetColorU32(new Vector4(1, 1, 1, 1)));


            // Draw Game Objects
            if (gameSession.TheGameBoard != null)
            {
                foreach (var brick in gameSession.TheGameBoard.Bricks)
                {
                    var brickPos = contentMin + (brick.Position * pixelsPerUnit);
                    var brickSize = brick.Size * pixelsPerUnit;
                    drawList.AddRectFilled(brickPos, brickPos + brickSize, brick.Color);
                }
            }
            if (gameSession.ThePaddle != null)
            {
                var paddle = gameSession.ThePaddle;
                var paddlePos = contentMin + (paddle.Position * pixelsPerUnit);
                var paddleSize = paddle.Size * pixelsPerUnit;
                drawList.AddRectFilled(paddlePos, paddlePos + paddleSize, ImGui.GetColorU32(new Vector4(1, 1, 1, 1)));
            }

            // Draw Ball
            if (gameSession.Balls.Any() && textureManager.BallTexture?.ImGuiHandle != null)
            {
                foreach (var ball in gameSession.Balls)
                {
                    var ballTopLeft = contentMin + (ball.Position * pixelsPerUnit);
                    var ballSize = new Vector2(ball.Radius * 2, ball.Radius * 2) * pixelsPerUnit;
                    drawList.AddImage(textureManager.BallTexture.ImGuiHandle, ballTopLeft, ballTopLeft + ballSize);
                }
            }
            else if (gameSession.Balls.Any()) // Fallback to circle if texture fails to load
            {
                foreach (var ball in gameSession.Balls)
                {
                    var ballCenter = contentMin + ((ball.Position + new Vector2(ball.Radius, ball.Radius)) * pixelsPerUnit);
                    var ballRadius = ball.Radius * pixelsPerUnit;
                    drawList.AddCircleFilled(ballCenter, ballRadius, ImGui.GetColorU32(new Vector4(1, 1, 0, 1)));
                }
            }

            // Draw Power-ups
            foreach (var powerUp in gameSession.ActivePowerUps.ToList())
            {
                var powerUpPos = contentMin + (powerUp.Position * pixelsPerUnit);
                if (powerUp.IsText)
                {
                    float textSize = 3 * ImGuiHelpers.GlobalScale;
                    float textWidth = UIManager.GetBlockyTextWidth(powerUp.Text, textSize);

                    // Adjust position if text would be drawn off-screen
                    if (powerUpPos.X + textWidth > contentMin.X + contentSize.X)
                    {
                        powerUpPos.X = (contentMin.X + contentSize.X) - textWidth;
                    }

                    UIManager.DrawBlockyText(drawList, powerUp.Text, powerUpPos, textSize, ImGui.GetColorU32(new Vector4(1, 1, 1, 1)));
                }
                else
                {
                    var powerUpSize = powerUp.Size * pixelsPerUnit;
                    drawList.AddRectFilled(powerUpPos, powerUpPos + powerUpSize, ImGui.GetColorU32(new Vector4(1, 1, 1, 1)));
                }
            }
        }

        private void HandleInput(Vector2 contentMin, float pixelsPerUnit)
        {
            if (!ImGui.IsWindowHovered()) return;
            var mousePos = ImGui.GetMousePos();
            float mouseGameX = (mousePos.X - contentMin.X) / pixelsPerUnit;
            float newPaddleX = mouseGameX - (gameSession.ThePaddle?.Size.X / 2 ?? 0);
            gameSession.MovePaddle(newPaddleX);
        }

        private void DrawGameOver()
        {
            var windowSize = ImGui.GetWindowSize();
            var scale = ImGuiHelpers.GlobalScale;

            // --- Center "GAME OVER" Text ---
            var gameOverText = "GAME OVER";
            ImGui.SetWindowFontScale(2.5f);
            var gameOverTextSize = ImGui.CalcTextSize(gameOverText);
            var gameOverTextPos = new Vector2(
                (windowSize.X - gameOverTextSize.X) * 0.5f,
                windowSize.Y * 0.4f
            );
            ImGui.SetCursorPos(gameOverTextPos);
            ImGui.Text(gameOverText);
            ImGui.SetWindowFontScale(1f);

            // --- Center "Main Menu" Button ---
            var buttonText = "Main Menu";
            var buttonSize = new Vector2(120 * scale, 30 * scale);
            var buttonPos = new Vector2(
                (windowSize.X - buttonSize.X) * 0.5f,
                gameOverTextPos.Y + gameOverTextSize.Y + 20 * scale
            );
            ImGui.SetCursorPos(buttonPos);
            if (ImGui.Button(buttonText, buttonSize))
            {
                gameSession.GoToMainMenu();
            }
        }
    }
}