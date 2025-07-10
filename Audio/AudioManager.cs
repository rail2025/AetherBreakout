using System;
using System.IO;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Dalamud.Plugin.Services;
using System.Reflection;
using System.Linq;
using ImGuiNET;

namespace AetherBreakout.Audio
{
    public class LoopStream : WaveStream
    {
        private readonly WaveStream sourceStream;

        public LoopStream(WaveStream sourceStream)
        {
            this.sourceStream = sourceStream;
            this.EnableLooping = true;
        }

        public bool EnableLooping { get; set; }
        public override WaveFormat WaveFormat => sourceStream.WaveFormat;
        public override long Length => sourceStream.Length;
        public override long Position
        {
            get => sourceStream.Position;
            set => sourceStream.Position = value;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int totalBytesRead = 0;
            while (totalBytesRead < count)
            {
                int bytesRead = sourceStream.Read(buffer, offset + totalBytesRead, count - totalBytesRead);
                if (bytesRead == 0)
                {
                    if (sourceStream.Position == 0 || !EnableLooping) break;
                    sourceStream.Position = 0;
                }
                totalBytesRead += bytesRead;
            }
            return totalBytesRead;
        }
    }

    public class AudioManager : IDisposable
    {
        private readonly IPluginLog log;
        private readonly Configuration configuration;
        private WaveOutEvent? bgmOutputDevice;
        private WaveStream? bgmFileReader;
        private bool isBgmPlaying = false;
        private string? currentTrackName;

        private readonly WaveOutEvent sfxOutputDevice;
        private readonly MixingSampleProvider sfxMixer;

        private bool isFading = false;
        private float fadeStartTime;
        private float fadeDuration;
        private float startVolume;
        private float endVolume;
        private Action? onFadeComplete;

        public AudioManager(IPluginLog log, Configuration configuration)
        {
            this.log = log;
            this.configuration = configuration;
            var mixerFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
            this.sfxMixer = new MixingSampleProvider(mixerFormat) { ReadFully = true };

            this.sfxOutputDevice = new WaveOutEvent();
            this.sfxOutputDevice.Init(this.sfxMixer);
            this.sfxOutputDevice.Play();
        }

        public void PlaySfx(string sfxName)
        {
            if (this.configuration.IsSfxMuted) return;

            var assembly = Assembly.GetExecutingAssembly();
            var resourcePath = $"AetherBreakout.Sfx.{sfxName}";

            try
            {
                using var stream = assembly.GetManifestResourceStream(resourcePath);
                if (stream == null)
                {
                    log.Warning($"SFX resource not found: {resourcePath}");
                    return;
                }

                var memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);
                memoryStream.Position = 0;

                WaveStream readerStream = sfxName.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase)
                    ? new Mp3FileReader(memoryStream)
                    : new WaveFileReader(memoryStream);

                ISampleProvider soundToPlay = readerStream.ToSampleProvider();

                if (soundToPlay.WaveFormat.SampleRate != this.sfxMixer.WaveFormat.SampleRate ||
                    soundToPlay.WaveFormat.Channels != this.sfxMixer.WaveFormat.Channels)
                {
                    var resampler = new WdlResamplingSampleProvider(soundToPlay, this.sfxMixer.WaveFormat.SampleRate);
                    soundToPlay = resampler.WaveFormat.Channels != this.sfxMixer.WaveFormat.Channels
                        ? new MonoToStereoSampleProvider(resampler)
                        : resampler;
                }

                var volumeProvider = new VolumeSampleProvider(soundToPlay)
                {
                    Volume = this.configuration.SfxVolume
                };

                this.sfxMixer.AddMixerInput(volumeProvider);
            }
            catch (Exception ex)
            {
                log.Error(ex, $"Failed to play SFX: {sfxName}");
            }
        }

        public void PlayMusic(string musicName, bool loop = false)
        {
            StopMusic();
            isBgmPlaying = true;
            currentTrackName = musicName;
            var resourcePath = $"AetherBreakout.Music.{musicName}";

            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var stream = assembly.GetManifestResourceStream(resourcePath);
                if (stream == null) return;

                var memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);
                memoryStream.Position = 0;

                this.bgmFileReader = new Mp3FileReader(memoryStream);
                this.bgmOutputDevice = new WaveOutEvent();

                if (loop)
                {
                    var loopStream = new LoopStream(this.bgmFileReader);
                    this.bgmOutputDevice.Init(loopStream);
                }
                else
                {
                    this.bgmOutputDevice.PlaybackStopped += OnBgmPlaybackStopped;
                    this.bgmOutputDevice.Init(this.bgmFileReader);
                }

                this.bgmOutputDevice.Volume = this.configuration.MusicVolume;

                if (!this.configuration.IsBgmMuted)
                {
                    this.bgmOutputDevice.Play();
                }
            }
            catch (Exception ex)
            {
                this.log.Error(ex, $"Failed to play BGM: {musicName}");
            }
        }

        public void FadeMusic(float targetVolume, float duration, Action? onComplete = null)
        {
            if (bgmOutputDevice == null)
            {
                onComplete?.Invoke();
                return;
            }

            startVolume = bgmOutputDevice.Volume;
            endVolume = Math.Clamp(targetVolume, 0f, 1f);
            fadeDuration = duration;
            fadeStartTime = (float)ImGui.GetTime();
            isFading = true;
            onFadeComplete = onComplete;
        }

        private void OnBgmPlaybackStopped(object? sender, StoppedEventArgs e)
        {
            if (isBgmPlaying && currentTrackName != null)
            {
                var trackNumberStr = new string(currentTrackName.Where(char.IsDigit).ToArray());
                if (int.TryParse(trackNumberStr, out var trackNumber))
                {
                    var nextTrackNumber = (trackNumber % 4) + 1;
                    PlayMusic($"bgm{nextTrackNumber}.mp3", false);
                }
            }
        }

        public void UpdateBgmState()
        {
            if (bgmOutputDevice == null) return;

            if (isFading)
            {
                var elapsedTime = (float)ImGui.GetTime() - fadeStartTime;
                if (elapsedTime >= fadeDuration)
                {
                    bgmOutputDevice.Volume = endVolume;
                    isFading = false;
                    onFadeComplete?.Invoke();
                }
                else
                {
                    float progress = elapsedTime / fadeDuration;
                    bgmOutputDevice.Volume = startVolume + (endVolume - startVolume) * progress;
                }
            }
            else
            {
                bgmOutputDevice.Volume = this.configuration.MusicVolume;

                if (this.configuration.IsBgmMuted && bgmOutputDevice.PlaybackState == PlaybackState.Playing)
                {
                    bgmOutputDevice.Pause();
                }
                else if (!this.configuration.IsBgmMuted && bgmOutputDevice.PlaybackState == PlaybackState.Paused)
                {
                    bgmOutputDevice.Play();
                }
            }
        }

        public void SetBgmVolume(float volume)
        {
            if (this.bgmOutputDevice != null)
            {
                this.bgmOutputDevice.Volume = volume;
            }
        }

        public void StopMusic()
        {
            if (this.bgmOutputDevice != null)
            {
                this.bgmOutputDevice.PlaybackStopped -= OnBgmPlaybackStopped;
                this.bgmOutputDevice.Stop();
                this.bgmOutputDevice.Dispose();
                this.bgmOutputDevice = null;
            }
            this.bgmFileReader?.Dispose();
            this.bgmFileReader = null;
        }

        public void EndPlaylist()
        {
            this.isBgmPlaying = false;
            StopMusic();
        }

        public void Dispose()
        {
            isBgmPlaying = false;
            StopMusic();
            this.sfxOutputDevice.Dispose();
        }
    }
}