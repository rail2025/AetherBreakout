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
        private readonly Plugin plugin;
        private readonly Configuration configuration;
        private readonly GameSession gameSession;

        public ConfigWindow(Plugin plugin, GameSession gameSession) : base("AetherBreakout Settings")
        {
            this.Size = new Vector2(300, 300) * ImGuiHelpers.GlobalScale;
            this.SizeCondition = ImGuiCond.FirstUseEver;
            this.plugin = plugin;
            this.configuration = plugin.Configuration;
            this.gameSession = gameSession;
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

            ImGui.Separator();
            ImGui.Text("Audio");

            var isBgmMuted = configuration.IsBgmMuted;
            if (ImGui.Checkbox("Mute Music", ref isBgmMuted))
            {
                configuration.IsBgmMuted = isBgmMuted;
                configuration.Save();
            }

            ImGui.SameLine();

            var isSfxMuted = configuration.IsSfxMuted;
            if (ImGui.Checkbox("Mute SFX", ref isSfxMuted))
            {
                configuration.IsSfxMuted = isSfxMuted;
                configuration.Save();
            }

            var musicVolume = configuration.MusicVolume;
            if (ImGui.SliderFloat("Music Volume", ref musicVolume, 0.0f, 1.0f))
            {
                configuration.MusicVolume = musicVolume;
                plugin.AudioManager.SetBgmVolume(musicVolume);
            }
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                configuration.Save();
            }

            var sfxVolume = configuration.SfxVolume;
            if (ImGui.SliderFloat("SFX Volume", ref sfxVolume, 0.0f, 1.0f))
            {
                configuration.SfxVolume = sfxVolume;
            }
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                plugin.AudioManager.PlaySfx("bounce.wav");
                configuration.Save();
            }

            ImGui.Separator();
            ImGui.Text("Auto-Open Triggers");

            var openOnDeath = this.configuration.OpenOnDeath;
            if (ImGui.Checkbox("Open on Death", ref openOnDeath))
            {
                this.configuration.OpenOnDeath = openOnDeath;
                this.configuration.Save();
            }

            var openInQueue = this.configuration.OpenInQueue;
            if (ImGui.Checkbox("Open in Duty Queue", ref openInQueue))
            {
                this.configuration.OpenInQueue = openInQueue;
                this.configuration.Save();
            }

            var openInPartyFinder = this.configuration.OpenInPartyFinder;
            if (ImGui.Checkbox("Open in Party Finder", ref openInPartyFinder))
            {
                this.configuration.OpenInPartyFinder = openInPartyFinder;
                this.configuration.Save();
            }

            var openDuringCrafting = this.configuration.OpenDuringCrafting;
            if (ImGui.Checkbox("Open During Crafting", ref openDuringCrafting))
            {
                this.configuration.OpenDuringCrafting = openDuringCrafting;
                this.configuration.Save();
            }
        }
    }
}