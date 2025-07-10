using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Game.Command;
using Dalamud.Plugin.Services;
using AetherBreakout.Windows;
using AetherBreakout.Game;
using AetherBreakout.Audio;
using Dalamud.Game.ClientState.Conditions;

namespace AetherBreakout
{
    public sealed class Plugin : IDalamudPlugin
    {
        [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
        [PluginService] internal static IClientState ClientState { get; private set; } = null!;
        [PluginService] internal static IPluginLog Log { get; private set; } = null!;
        [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
        [PluginService] internal static ICondition Condition { get; private set; } = null!;

        public readonly WindowSystem WindowSystem = new("AetherBreakout");
        public Configuration Configuration { get; init; }
        public AudioManager AudioManager { get; init; }

        private MainWindow MainWindow { get; init; }
        private ConfigWindow ConfigWindow { get; init; }
        private AboutWindow AboutWindow { get; init; }
        private GameSession GameSession { get; init; }
        private TextureManager TextureManager { get; init; }

        private bool wasDead = false;

        public Plugin()
        {
            this.Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(PluginInterface);

            this.AudioManager = new AudioManager(Log, this.Configuration);
            this.TextureManager = new TextureManager(Log, TextureProvider, PluginInterface);
            this.GameSession = new GameSession(this.Configuration, this.AudioManager);

            MainWindow = new MainWindow(this, this.GameSession, this.TextureManager, this.AudioManager);
            ConfigWindow = new ConfigWindow(this, this.GameSession);
            AboutWindow = new AboutWindow(this);

            WindowSystem.AddWindow(MainWindow);
            WindowSystem.AddWindow(ConfigWindow);
            WindowSystem.AddWindow(AboutWindow);

            CommandManager.AddHandler("/abreakout", new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens the AetherBreakout game window."
            });

            Condition.ConditionChange += OnConditionChanged;
            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;
            PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
        }

        public void Dispose()
        {
            Condition.ConditionChange -= OnConditionChanged;
            PluginInterface.UiBuilder.Draw -= DrawUI;
            PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUI;
            PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUI;

            CommandManager.RemoveHandler("/abreakout");
            this.WindowSystem.RemoveAllWindows();

            this.AudioManager.EndPlaylist();
            this.TextureManager.Dispose();
            this.AudioManager.Dispose();
            MainWindow.Dispose();
            ConfigWindow.Dispose();
            AboutWindow.Dispose();
        }

        private void OnCommand(string command, string args) => ToggleMainUI();

        public void ToggleConfigUI() => ConfigWindow.Toggle();
        public void ToggleMainUI() => MainWindow.Toggle();
        public void ToggleAboutUI() => AboutWindow.Toggle();

        private void DrawUI()
        {
            this.AudioManager.UpdateBgmState();
            this.WindowSystem.Draw();
        }

        private void OnConditionChanged(ConditionFlag flag, bool value)
        {
            if (flag == ConditionFlag.InCombat && !value)
            {
                bool isDead = ClientState.LocalPlayer?.CurrentHp == 0;
                if (isDead && !wasDead && Configuration.OpenOnDeath) { MainWindow.IsOpen = true; }
                wasDead = isDead;
            }

            if (flag == ConditionFlag.InDutyQueue && value && Configuration.OpenInQueue) { MainWindow.IsOpen = true; }
            if (flag == ConditionFlag.UsingPartyFinder && value && Configuration.OpenInPartyFinder) { MainWindow.IsOpen = true; }
            if (flag == ConditionFlag.Crafting && value && Configuration.OpenDuringCrafting) { MainWindow.IsOpen = true; }
        }
    }
}