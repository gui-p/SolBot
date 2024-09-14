using SolBot.Interfaces;
using CliWrap;
using Discord.Audio;
using System.Diagnostics;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using Microsoft.IO;

namespace SolBot.Services
{
    public sealed class MusicService : IMusicService
    {

        private readonly RecyclableMemoryStreamManager _memoryStreamManager = new ();
        private readonly CancellationTokenSource _cancellationTokenSource = new ();
        private readonly YoutubeClient _youtubeClient = new (); 
        
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
                await output.CopyToAsync(discord, _cancellationTokenSource.Token);
            }
            finally
            {
                await discord.FlushAsync();
            }
        }

        public async Task StreamFromYoutube(IAudioClient audioClient, string link)
        {
            StreamManifest streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(link);
            IStreamInfo streamInfo = streamManifest.GetAudioOnlyStreams().First();
            Console.WriteLine("Streaming link: " + streamInfo.Url);
            
            await using Stream stream = await _youtubeClient.Videos.Streams.GetAsync(streamInfo);
            await using RecyclableMemoryStream memoryStream = _memoryStreamManager.GetStream();
            await using AudioOutStream discord = audioClient.CreatePCMStream(AudioApplication.Mixed);

            await Cli.Wrap("ffmpeg")
                .WithArguments(" -hide_banner -loglevel panic -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1")
                .WithStandardInputPipe(PipeSource.FromStream(stream))
                .WithStandardOutputPipe(PipeTarget.ToStream(memoryStream))
                .ExecuteAsync();

            Console.WriteLine("Memory size: " + memoryStream.Capacity + " Bytes");
            try
            {
                await discord.WriteAsync(memoryStream.GetBuffer(), _cancellationTokenSource.Token);
            }
            finally
            {
                await stream.FlushAsync();
                await discord.FlushAsync();
                await memoryStream.FlushAsync();
                memoryStream.Capacity = 0;
            }
        }

        public async Task StopPlayingMusic()
        {
            await _cancellationTokenSource.CancelAsync();
            _cancellationTokenSource.TryReset();
        }
    }
}

