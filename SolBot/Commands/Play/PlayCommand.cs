using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using SolBot.Enums;
using SolBot.Interfaces;

namespace SolBot.Commands.Play
{
    public class PlayCommand(IMusicService musicService) : ModuleBase<SocketCommandContext>
    {

        [Command("l", RunMode = RunMode.Async, Summary = "Play a video audio from a computer path")]
        public async Task PlayLocal([Remainder] string link)
        {
            await Play(StreamFrom.LocalFolder, link);
        }

        [Command("p", RunMode = RunMode.Async, Summary = "Play a youtube video audio from a link")]
        public async Task PlayYoutube([Remainder] string link)
        {
            await Play(StreamFrom.Youtube, link);
        }

        [Command("s", RunMode = RunMode.Async, Summary = "Stop a music")]
        public async Task StopPlaying()
        {
            await musicService.Stop();
        }

        private async Task Play(StreamFrom source, string path)
        {
            if (string.IsNullOrEmpty(path))
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
                    await musicService.Play(source, path,audioClient);
                }

                await ReplyAsync(message: $"Disconnected from channel \"{userChannel.Name}\"!");
            }
            catch (TaskCanceledException)
            {
                await ReplyAsync(message: $"Stopped playing!");
            }
            catch (Exception ex)
            {
                await ReplyAsync(message: $"An exception occurred: {ex.Message}");
            }
        }
    }
}
