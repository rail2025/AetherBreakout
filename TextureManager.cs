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
    public class TextureManager
    {
        public IDalamudTextureWrap? BallTexture { get; private set; }

        public TextureManager(IPluginLog log, ITextureProvider textureProvider, IDalamudPluginInterface pluginInterface)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "AetherBreakout.Resources.ball.png";

            try
            {
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    var image = SixLabors.ImageSharp.Image.Load<Rgba32>(stream);
                    var rgbaBytes = new byte[image.Width * image.Height * 4];
                    image.CopyPixelDataTo(rgbaBytes);
                    this.BallTexture = textureProvider.CreateFromRaw(RawImageSpecification.Rgba32(image.Width, image.Height), rgbaBytes);
                }
                else
                {
                    log.Warning($"Embedded resource not found at path: {resourceName}");
                }
            }
            catch (Exception ex)
            {
                log.Error(ex, $"Failed to load embedded texture: {resourceName}");
            }
        }

        public void Dispose()
        {
            this.BallTexture?.Dispose();
        }
    }
}