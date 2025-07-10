using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Interface.Utility;
using ImGuiNET;
using AetherBreakout.Game;
using AetherBreakout.UI;
using AetherBreakout.Audio;

namespace AetherBreakout.Windows
{
    public class MainWindow : Window, IDisposable
    {
        private readonly Plugin plugin;
        private readonly GameSession gameSession;
        private readonly TextureManager textureManager;
        private readonly AudioManager audioManager;
        private bool isTitleMusicPlaying = false;

        public static readonly Vector2 BaseWindowSize = new(600, 800);

        public MainWindow(Plugin plugin, GameSession gameSession, TextureManager textureManager, AudioManager audioManager) : base("AetherBreakout")
        {
            this.SizeCondition = ImGuiCond.FirstUseEver;
            this.Flags |= ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar;
            this.plugin = plugin;
            this.gameSession = gameSession;
            this.textureManager = textureManager;
            this.audioManager = audioManager;
        }

        public override void PreDraw()
        {
            this.Size = BaseWindowSize * ImGuiHelpers.GlobalScale;
        }

        public void Dispose() { }

        public override void OnOpen()
        {
            if (gameSession.CurrentGameState == GameState.MainMenu && !isTitleMusicPlaying)
            {
                audioManager.PlayMusic("titlemusic.mp3", true);
                isTitleMusicPlaying = true;
            }
        }

        public override void OnClose()
        {
            audioManager.StopMusic();
            isTitleMusicPlaying = false;
        }

        public override void Draw()
        {
            gameSession.Update(ImGui.GetIO().DeltaTime);

            switch (gameSession.CurrentGameState)
            {
                case GameState.MainMenu:
                    if (!isTitleMusicPlaying)
                    {
                        audioManager.PlayMusic("titlemusic.mp3", true);
                        isTitleMusicPlaying = true;
                    }
                    DrawMainMenu();
                    break;
                case GameState.InGame:
                    isTitleMusicPlaying = false;
                    DrawGame();
                    break;
                case GameState.GameOver:
                    isTitleMusicPlaying = false;
                    DrawGameOver();
                    break;
            }
        }

        private void DrawMainMenu()
        {
            var windowPos = ImGui.GetWindowPos();
            var windowSize = ImGui.GetWindowSize();
            var scale = ImGuiHelpers.GlobalScale;

            if (textureManager.BackgroundTexture != null)
            {
                ImGui.GetWindowDrawList().AddImage(textureManager.BackgroundTexture.ImGuiHandle, windowPos, windowPos + windowSize);
            }

            var titleText = "AetherBreakout";
            ImGui.SetWindowFontScale(2.5f);
            var titleTextSize = ImGui.CalcTextSize(titleText);
            var titlePos = new Vector2((windowSize.X - titleTextSize.X) * 0.5f, windowSize.Y * 0.3f);
            ImGui.SetCursorPos(titlePos);
            ImGui.Text(titleText);
            ImGui.SetWindowFontScale(1f);

            var buttonSize = new Vector2(140 * scale, 40 * scale);
            var buttonX = (windowSize.X - buttonSize.X) * 0.5f;
            var currentY = titlePos.Y + titleTextSize.Y + 40 * scale;

            ImGui.SetCursorPos(new Vector2(buttonX, currentY));
            if (ImGui.Button("Start Game", buttonSize))
            {
                audioManager.StopMusic();
                gameSession.StartNewGame();
            }
            currentY += buttonSize.Y + (10 * scale);

            ImGui.SetCursorPos(new Vector2(buttonX, currentY));
            if (ImGui.Button("Settings", buttonSize))
            {
                plugin.ToggleConfigUI();
            }
            currentY += buttonSize.Y + (10 * scale);

            ImGui.SetCursorPos(new Vector2(buttonX, currentY));
            if (ImGui.Button("About", buttonSize))
            {
                plugin.ToggleAboutUI();
            }

            var checkboxPos = new Vector2(windowSize.X - 120 * scale, windowSize.Y - 40 * scale);
            ImGui.SetCursorPos(checkboxPos);
            var isMuted = plugin.Configuration.IsBgmMuted;
            if (ImGui.Checkbox("Mute Music", ref isMuted))
            {
                plugin.Configuration.IsBgmMuted = isMuted;
                plugin.Configuration.Save();
            }
        }

        private void DrawGame()
        {
            var contentMin = ImGui.GetWindowContentRegionMin() + ImGui.GetWindowPos();
            var contentSize = ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin();
            var drawList = ImGui.GetWindowDrawList();
            var scale = ImGuiHelpers.GlobalScale;

            float pixelsPerUnit = contentSize.X / GameSession.GameBoardSize.X;

            HandleInput(contentMin, pixelsPerUnit);

            var highScoreText = $"{plugin.Configuration.HighScore:D3}";
            var scoreText = $"{gameSession.Score:D3}";
            var livesText = $"{gameSession.Lives}";
            var levelText = $"{gameSession.Level}";
            float hudFontSize = 4 * scale;
            UIManager.DrawBlockyText(drawList, highScoreText, contentMin + new Vector2(20, 10), hudFontSize, ImGui.GetColorU32(new Vector4(0.8f, 0.8f, 0.8f, 1)));
            float highScoreWidth = UIManager.GetBlockyTextWidth(highScoreText, hudFontSize);
            var scorePos = contentMin + new Vector2(20 + highScoreWidth + 40 * scale, 10);
            UIManager.DrawBlockyText(drawList, scoreText, scorePos, hudFontSize, ImGui.GetColorU32(new Vector4(1, 1, 1, 1)));
            UIManager.DrawBlockyText(drawList, livesText, contentMin + new Vector2(contentSize.X - 120 * scale, 10), hudFontSize, ImGui.GetColorU32(new Vector4(1, 1, 1, 1)));
            UIManager.DrawBlockyText(drawList, levelText, contentMin + new Vector2(contentSize.X - 60 * scale, 10), hudFontSize, ImGui.GetColorU32(new Vector4(1, 1, 1, 1)));

            if (gameSession.TheGameBoard != null)
            {
                foreach (var brick in gameSession.TheGameBoard.Bricks.Where(b => b.IsActive))
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
            if (gameSession.Balls.Any() && textureManager.BallTexture?.ImGuiHandle != null)
            {
                foreach (var ball in gameSession.Balls)
                {
                    var ballTopLeft = contentMin + (ball.Position * pixelsPerUnit);
                    var ballSize = new Vector2(ball.Radius * 2, ball.Radius * 2) * pixelsPerUnit;
                    drawList.AddImage(textureManager.BallTexture.ImGuiHandle, ballTopLeft, ballTopLeft + ballSize);
                }
            }

            // Corrected Power-Up Drawing Logic
            foreach (var powerUp in gameSession.ActivePowerUps.ToList())
            {
                var powerUpPos = contentMin + (powerUp.Position * pixelsPerUnit);
                if (powerUp.IsText)
                {
                    float textSize = 3 * scale;
                    UIManager.DrawBlockyText(drawList, powerUp.Text, powerUpPos, textSize, ImGui.GetColorU32(new Vector4(1, 1, 1, 1)));
                }
                else
                {
                    var powerUpSize = powerUp.Size * pixelsPerUnit;
                    drawList.AddRectFilled(powerUpPos, powerUpPos + powerUpSize, ImGui.GetColorU32(new Vector4(1, 1, 1, 1)));
                }
            }

            ImGui.SetCursorScreenPos(contentMin + new Vector2(10 * scale, contentSize.Y - 30 * scale));
            ImGui.PushItemWidth(120 * scale);
            var musicVolume = plugin.Configuration.MusicVolume;
            if (ImGui.SliderFloat("##MusicVolume", ref musicVolume, 0.0f, 1.0f, ""))
            {
                plugin.Configuration.MusicVolume = musicVolume;
                plugin.Configuration.Save();
            }
            ImGui.PopItemWidth();

            ImGui.SetCursorScreenPos(contentMin + new Vector2(contentSize.X - 110 * scale, contentSize.Y - 30 * scale));
            var isMuted = plugin.Configuration.IsBgmMuted;
            if (ImGui.Checkbox("Mute", ref isMuted))
            {
                plugin.Configuration.IsBgmMuted = isMuted;
                plugin.Configuration.Save();
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
            var windowPos = ImGui.GetWindowPos();
            var windowSize = ImGui.GetWindowSize();
            var scale = ImGuiHelpers.GlobalScale;

            if (textureManager.GameOverTexture != null)
            {
                ImGui.GetWindowDrawList().AddImage(textureManager.GameOverTexture.ImGuiHandle, windowPos, windowPos + windowSize);
            }

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