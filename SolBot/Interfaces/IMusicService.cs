using Discord.Audio;
using SolBot.Enums;

namespace SolBot.Interfaces
{
    public interface IMusicService
    {
        Task Play(StreamFrom source, string path, IAudioClient audioClient);
        Task Stop();
    }
}

