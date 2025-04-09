using Plugin.Maui.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIHomeStudio.Utilities
{
    public class AudioPlayerService : IAudioPlayerService
    {
        private readonly IAudioManager _audioManager;
        private IAudioPlayer _audioPlayer;

        public AudioPlayerService(IAudioManager audioManager)
        {
            Logger.Log("Initializing..", this, true);
            _audioManager = audioManager;
        }



        // OBS: using Task.Run here to avoid having the sync method IAudioPlayer.Play() block the UI-thread

        public async Task PlayAudioAsync(string filePath)
        {
            try
            {
                await Task.Run(async () =>
                {
                    await using var stream = File.OpenRead(filePath);
                    await PlayAudioAsync(stream);
                });
            }
            catch (Exception ex)
            {
                Logger.Log($"Exception thrown: {ex}", this, false);
            }
        }

        
        public async Task PlayAudioAsync(Stream audioStream)
        {
            try
            {
                StopAudio();
                using var audioPlayer = _audioManager.CreatePlayer(audioStream);
                _audioPlayer = audioPlayer; // ref for volume
                await Task.Run(() => audioPlayer.Play()); 
            }
            catch (Exception ex)
            {
                Logger.Log($"Exception thrown: {ex}", this, false);
            }
        }



        public void SetVolume(double volume)
        {
            if (_audioPlayer != null)
            {
                _audioPlayer.Volume = volume;
            }
        }



        public void StopAudio()
        {
            if (_audioPlayer != null && _audioPlayer.IsPlaying)
            {
                _audioPlayer.Stop();
                _audioPlayer.Dispose(); 
                _audioPlayer = null;
            }
        }
    }
}
