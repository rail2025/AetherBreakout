using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;

namespace AetherBreakout
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;
        public float BallSpeedMultiplier { get; set; } = 1.0f;
        public int HighScore { get; set; } = 0;

        // Audio Settings
        public bool IsSfxMuted { get; set; } = false;
        public bool IsBgmMuted { get; set; } = false;
        public float MusicVolume { get; set; } = 0.5f;
        public float SfxVolume { get; set; } = 1.0f;

        // Auto-open Triggers
        public bool OpenOnDeath { get; set; } = false;
        public bool OpenInQueue { get; set; } = false;
        public bool OpenInPartyFinder { get; set; } = false;
        public bool OpenDuringCrafting { get; set; } = false;

        [NonSerialized]
        private IDalamudPluginInterface? pluginInterface;

        public void Initialize(IDalamudPluginInterface pInterface)
        {
            this.pluginInterface = pInterface;
        }

        public void Save()
        {
            this.pluginInterface?.SavePluginConfig(this);
        }
    }
}