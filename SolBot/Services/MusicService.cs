using SolBot.Interfaces;
using CliWrap;
using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using System.Diagnostics;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using Microsoft.IO;

namespace SolBot.Services
{
    public sealed class MusicService : IMusicService
    {
        
        private static readonly RecyclableMemoryStreamManager MemoryStreamManager = new RecyclableMemoryStreamManager();
        
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
        
        public async Task StreamFromLocal(IAudioClient audioClient, string path)
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

        public async Task StreamFromYoutube(IAudioClient audioClient, string link)
        {

            YoutubeClient youtubeClient = new();
            StreamManifest streamManifest = await youtubeClient.Videos.Streams.GetManifestAsync(link);
            IStreamInfo streamInfo = streamManifest.GetAudioOnlyStreams().First();
            Console.WriteLine("Streaming link: " + streamInfo.Url);
            Stream stream = await youtubeClient.Videos.Streams.GetAsync(streamInfo);

            await using var memoryStream = MemoryStreamManager.GetStream();
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

