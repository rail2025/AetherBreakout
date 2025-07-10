using System;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using System.IO;
using System.Reflection;
using SixLabors.ImageSharp.PixelFormats;

namespace AetherBreakout
{
    public class TextureManager : IDisposable
    {
        public IDalamudTextureWrap? BallTexture { get; private set; }
        public IDalamudTextureWrap? BackgroundTexture { get; private set; }
        public IDalamudTextureWrap? GameOverTexture { get; private set; }

        public TextureManager(IPluginLog log, ITextureProvider textureProvider, IDalamudPluginInterface pluginInterface)
        {
            var assembly = Assembly.GetExecutingAssembly();

            // Load Ball Texture
            LoadTexture(log, textureProvider, assembly, "AetherBreakout.Resources.ball.png", tex => this.BallTexture = tex);

            // Load Background Texture
            LoadTexture(log, textureProvider, assembly, "AetherBreakout.Resources.background.png", tex => this.BackgroundTexture = tex);

            // Load Game Over Texture
            LoadTexture(log, textureProvider, assembly, "AetherBreakout.Resources.gameover.png", tex => this.GameOverTexture = tex);
        }

        private void LoadTexture(IPluginLog log, ITextureProvider textureProvider, Assembly assembly, string resourcePath, Action<IDalamudTextureWrap> onTextureLoaded)
        {
            try
            {
                using var stream = assembly.GetManifestResourceStream(resourcePath);
                if (stream != null)
                {
                    var image = SixLabors.ImageSharp.Image.Load<Rgba32>(stream);
                    var rgbaBytes = new byte[image.Width * image.Height * 4];
                    image.CopyPixelDataTo(rgbaBytes);
                    var texture = textureProvider.CreateFromRaw(RawImageSpecification.Rgba32(image.Width, image.Height), rgbaBytes);
                    onTextureLoaded(texture);
                }
                else
                {
                    log.Warning($"Embedded resource not found at path: {resourcePath}");
                }
            }
            catch (Exception ex)
            {
                log.Error(ex, $"Failed to load embedded texture: {resourcePath}");
            }
        }

        public void Dispose()
        {
            this.BallTexture?.Dispose();
            this.BackgroundTexture?.Dispose();
            this.GameOverTexture?.Dispose();
        }
    }
}