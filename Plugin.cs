using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Game.Command;
using Dalamud.Plugin.Services;
using AetherBreakout.Windows;
using AetherBreakout.Game;

namespace AetherBreakout
{
    public sealed class Plugin : IDalamudPlugin
    {
        [PluginService] private static IDalamudPluginInterface PluginInterface { get; set; } = null!;
        [PluginService] private static ICommandManager CommandManager { get; set; } = null!;
        [PluginService] private static IClientState ClientState { get; set; } = null!;
        [PluginService] private static IPluginLog Log { get; set; } = null!;

        public readonly WindowSystem WindowSystem = new("AetherBreakout");
        public Configuration Configuration { get; init; }

        private MainWindow MainWindow { get; init; }
        private ConfigWindow ConfigWindow { get; init; }
        private GameSession GameSession { get; init; }


        public Plugin()
        {
            this.Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(PluginInterface);

            this.GameSession = new GameSession(this.Configuration);

            MainWindow = new MainWindow(this, this.GameSession);
            ConfigWindow = new ConfigWindow(this.Configuration, this.GameSession);

            WindowSystem.AddWindow(MainWindow);
            WindowSystem.AddWindow(ConfigWindow);

            CommandManager.AddHandler("/abreakout", new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens the AetherBreakout game window."
            });

            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;
            PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
        }

        public void Dispose()
        {
            PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUI;
            PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUI;
            PluginInterface.UiBuilder.Draw -= DrawUI;

            CommandManager.RemoveHandler("/abreakout");
            this.WindowSystem.RemoveAllWindows();

            MainWindow.Dispose();
            ConfigWindow.Dispose();
        }

        private void OnCommand(string command, string args)
        {
            ToggleMainUI();
        }

        private void ToggleMainUI()
        {
            MainWindow.IsOpen = !MainWindow.IsOpen;
        }

        private void ToggleConfigUI()
        {
            ConfigWindow.IsOpen = !ConfigWindow.IsOpen;
        }

        private void DrawUI()
        {
            this.WindowSystem.Draw();
        }
    }
}