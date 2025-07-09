using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace AetherBreakout
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;
        public float BallSpeedMultiplier { get; set; } = 1.0f;
        public int HighScore { get; set; } = 0;

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