using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.IO;
using SolBot.Interfaces;

namespace SolBot.Commands.Play
{
    public class PlayCommand(IMusicService musicService) : ModuleBase<SocketCommandContext>
    {
        private static readonly RecyclableMemoryStreamManager MemoryStreamManager = new RecyclableMemoryStreamManager();

        [Command("l", RunMode = RunMode.Async, Summary = "Play a video audio from a computer path")]
        public async Task PlayLocal([Remainder] string link)
        {
            await Play(link, musicService.StreamFromLocal);
        }

        [Command("p", RunMode = RunMode.Async, Summary = "Play a yotube video audio from a link")]
        public async Task PlayYoutube([Remainder] string link)
        {
            await Play(link, musicService.StreamFromYoutube);
        }

        private async Task Play(string link, Func<IAudioClient, string, Task> playFunction)
        {
            if (string.IsNullOrEmpty(link))
            {
                await ReplyAsync(message: "&p <youtube link>");
                return;
            }

            if ((Context.User as IGuildUser)?.VoiceChannel is not SocketVoiceChannel userChannel)
            {
                await ReplyAsync(message: "You must be in a voice channel!");
                return;
            }

            await ReplyAsync(message: $"Found user in channel \"{userChannel.Name}\"!");

            try
            {

                using (IAudioClient audioClient = await userChannel.ConnectAsync())
                {
                    await playFunction(audioClient, link);
                }
                await ReplyAsync(message: $"Disconnected from channel \"{userChannel.Name}\"!");
            }
            catch (Exception ex)
            {
                await ReplyAsync(message: $"An exception occurred: " + ex.Message);
            }
        }
    }
}
