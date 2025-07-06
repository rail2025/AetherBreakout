using System;
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

        public static readonly Vector2 BaseWindowSize = new(600, 800);

        public MainWindow(Plugin plugin, GameSession gameSession) : base("AetherBreakout")
        {
            this.Size = BaseWindowSize * ImGuiHelpers.GlobalScale;
            this.SizeCondition = ImGuiCond.FirstUseEver;
            this.Flags |= ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar;

            this.plugin = plugin;
            this.gameSession = gameSession;
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
            ImGui.Text("AetherBreakout");
            if (ImGui.Button("Start Game")) { gameSession.StartNewGame(); }
        }

        private void DrawGame()
        {
            var contentMin = ImGui.GetWindowContentRegionMin() + ImGui.GetWindowPos();
            var contentSize = ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin();
            var drawList = ImGui.GetWindowDrawList();

            float pixelsPerUnit = contentSize.X / GameSession.GameBoardSize.X;

            HandleInput(contentMin, pixelsPerUnit);

            var scoreText = $"{gameSession.Score:D3}";
            var livesText = $"{gameSession.Lives}";
            var levelText = $"{gameSession.Level}";
            float hudFontSize = 4 * ImGuiHelpers.GlobalScale;

            UIManager.DrawBlockyText(drawList, scoreText, contentMin + new Vector2(20, 10), hudFontSize, ImGui.GetColorU32(new Vector4(1, 1, 1, 1)));
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
            if (gameSession.TheBall != null)
            {
                var ball = gameSession.TheBall;
                var ballCenter = contentMin + ((ball.Position + new Vector2(ball.Radius, ball.Radius)) * pixelsPerUnit);
                var ballRadius = ball.Radius * pixelsPerUnit;
                drawList.AddCircleFilled(ballCenter, ballRadius, ImGui.GetColorU32(new Vector4(1, 1, 0, 1)));
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
            ImGui.Text("GAME OVER");
            if (ImGui.Button("Main Menu")) { gameSession.GoToMainMenu(); }
        }
    }
}