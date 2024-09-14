using CliWrap;
using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using System.Diagnostics;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using Microsoft.IO;

namespace SolBot.Commands.Play
{
    public class PlayCommand : ModuleBase<SocketCommandContext>
    {
        private static RecyclableMemoryStreamManager _memoryStreamManager = new RecyclableMemoryStreamManager();

        [Command("l", RunMode = RunMode.Async, Summary = "Play a video audio from a computer path")]
        public async Task PlayLocal([Remainder] string link)
        {
            await Play(link, StreamFromLocal);
        }

        [Command("p", RunMode = RunMode.Async, Summary = "Play a yotube video audio from a link")]
        public async Task PlayYoutube([Remainder] string link)
        {
            await Play(link, StreamFromYoutube);
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

        private Process CreateFFmpegProcess(string path)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-hide_banner -loglevel panic -re -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }) ?? throw new Exception("Null ffmpeg process reference!");
        }

        private async Task StreamFromLocal(IAudioClient audioClient, string path)
        {
            using var ffmpeg = CreateFFmpegProcess(path);
            await using var output = ffmpeg.StandardOutput.BaseStream;
            await using var discord = audioClient.CreatePCMStream(AudioApplication.Mixed);

            try
            {
                await output.CopyToAsync(discord);
            }
            finally
            {
                await discord.FlushAsync();
            }
        }

        private async Task StreamFromYoutube(IAudioClient audioClient, string link)
        {

            YoutubeClient youtubeClient = new();
            StreamManifest streamManifest = await youtubeClient.Videos.Streams.GetManifestAsync(link);
            IStreamInfo streamInfo = streamManifest.GetAudioOnlyStreams().First();
            Console.WriteLine("Streaming link: " + streamInfo.Url);
            Stream stream = await youtubeClient.Videos.Streams.GetAsync(streamInfo);

            await using var memoryStream = _memoryStreamManager.GetStream();
            await using var discord = audioClient.CreatePCMStream(AudioApplication.Mixed);

            await Cli.Wrap("ffmpeg")
                .WithArguments(" -hide_banner -loglevel panic -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1")
                .WithStandardInputPipe(PipeSource.FromStream(stream))
                .WithStandardOutputPipe(PipeTarget.ToStream(memoryStream))
                .ExecuteAsync();

            Console.WriteLine("Memory size: " + memoryStream.Capacity + " Bytes");
            try
            {
                await discord.WriteAsync(memoryStream.GetBuffer());
            }
            finally
            {
                await discord.FlushAsync();
                await memoryStream.FlushAsync();
                memoryStream.Capacity = 0;
            }
        }
    }
}
