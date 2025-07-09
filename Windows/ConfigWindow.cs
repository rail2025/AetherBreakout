using System;
using System.Numerics;
using AetherBreakout.Game;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace AetherBreakout.Windows
{
    public class ConfigWindow : Window, IDisposable
    {
        private readonly Configuration configuration;
        private readonly GameSession gameSession;

        public ConfigWindow(Configuration configuration, GameSession gameSession) : base("AetherBreakout Settings")
        {
            this.SizeCondition = ImGuiCond.FirstUseEver;
            this.configuration = configuration;
            this.gameSession = gameSession;
        }

        public override void PreDraw()
        {
            this.Size = new Vector2(232, 75) * ImGuiHelpers.GlobalScale;
        }

        public void Dispose() { }

        public override void Draw()
        {
            var ballSpeed = this.configuration.BallSpeedMultiplier;
            ImGui.PushItemWidth(150 * ImGuiHelpers.GlobalScale);
            if (ImGui.SliderFloat("Ball Speed", ref ballSpeed, 0.5f, 3.0f))
            {
                this.configuration.BallSpeedMultiplier = ballSpeed;
                this.gameSession.BallSpeedMultiplier = ballSpeed;
                this.configuration.Save();
            }
            ImGui.PopItemWidth();
        }
    }
}