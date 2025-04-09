using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIHomeStudio.Utilities
{
    public interface IAudioPlayerService
    {

        Task PlayAudioAsync(string filePath);
        Task PlayAudioAsync(Stream audioStream);
        void StopAudio();
        void SetVolume(double volume);
    }
}
