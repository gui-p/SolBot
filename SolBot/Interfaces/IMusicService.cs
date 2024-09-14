using Discord.Audio;

namespace SolBot.Interfaces
{
    public interface IMusicService
    {
        Task StreamFromLocal(IAudioClient audioClient, string link);
        Task StreamFromYoutube(IAudioClient audioClient, string link);
    }
}

