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
        [PluginService] private static ITextureProvider TextureProvider { get; set; } = null!;


        public readonly WindowSystem WindowSystem = new("AetherBreakout");
        public Configuration Configuration { get; init; }

        private MainWindow MainWindow { get; init; }
        private ConfigWindow ConfigWindow { get; init; }
        private AboutWindow AboutWindow { get; init; }
        private GameSession GameSession { get; init; }
        private TextureManager TextureManager { get; init; }


        public Plugin()
        {
            this.Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(PluginInterface);

            this.TextureManager = new TextureManager(Log, TextureProvider, PluginInterface);
            this.GameSession = new GameSession(this.Configuration);

            MainWindow = new MainWindow(this, this.GameSession, this.TextureManager);
            ConfigWindow = new ConfigWindow(this.Configuration, this.GameSession);
            AboutWindow = new AboutWindow(this);

            WindowSystem.AddWindow(MainWindow);
            WindowSystem.AddWindow(ConfigWindow);
            WindowSystem.AddWindow(AboutWindow);

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

            this.TextureManager.Dispose();
            MainWindow.Dispose();
            ConfigWindow.Dispose();
            AboutWindow.Dispose();
        }

        private void OnCommand(string command, string args)
        {
            ToggleMainUI();
        }

        private void ToggleMainUI() => MainWindow.IsOpen = !MainWindow.IsOpen;
        public void ToggleConfigUI() => ConfigWindow.IsOpen = !ConfigWindow.IsOpen;
        public void ToggleAboutUI() => AboutWindow.IsOpen = !AboutWindow.IsOpen;

        private void DrawUI()
        {
            this.WindowSystem.Draw();
        }
    }
}