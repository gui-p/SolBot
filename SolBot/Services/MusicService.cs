using SolBot.Interfaces;
using CliWrap;
using Discord.Audio;
using System.Diagnostics;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using Microsoft.IO;
using SolBot.Enums;
//using YoutubeExplode.Exceptions;

namespace SolBot.Services
{
    internal sealed class MusicService : IMusicService
    {

        private readonly RecyclableMemoryStreamManager _memoryStreamManager = new();
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly YoutubeClient _youtubeClient = new();
        
        private CancellationTokenSource? _playbackCts;
        private Process? _ffmpeg;

        public async Task Play(StreamFrom source, string path, IAudioClient audioClient)
        {
            switch (source)
            {
                case StreamFrom.LocalFolder:
                    {
                        await StreamFromLocal(audioClient, path);
                        break;
                    }

                case StreamFrom.Youtube:
                    {
                        await StreamFromYoutube(audioClient, path);
                        break;
                    }

                default: throw new ArgumentException("Invalid source type");
            }
        }

        public async Task Stop()
        {
            if (_playbackCts == null)
                return;

            try
            {
                await _playbackCts.CancelAsync();
            }
            catch { }

            if (_ffmpeg != null && !_ffmpeg.HasExited)
            {
                _ffmpeg.Kill(true);
                await _ffmpeg.WaitForExitAsync();
            }

            _playbackCts.Dispose();
            _playbackCts = null;
        }

        private static Process CreateFFmpegProcess(string path)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-hide_banner -loglevel panic -re -i {path} -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }) ?? throw new Exception("Null ffmpeg process reference!");
        }

        private async Task StreamFromLocal(IAudioClient audioClient, string path)
        {
            using var ffmpeg = CreateFFmpegProcess($"\"{path}\"");
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

        private async Task StreamFromYoutube(IAudioClient audioClient, string link)
        {
            
            bool isPlaylist;

            try
            {
                await _youtubeClient.Playlists.GetAsync(link);
                isPlaylist = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                isPlaylist = false;
            }


            if (isPlaylist)
            {
                await foreach (var video in _youtubeClient.Playlists.GetVideosAsync(link))
                {
                    
                    await BeginStreamingYoutube(audioClient, video.Url);
                }
            }
            else
            {
                await BeginStreamingYoutube(audioClient, link);
            }
            
            
            
            //await BeginStreamingYoutube(audioClient, link);

        }
        
        private async Task BeginStreamingYoutube(IAudioClient audioClient, string link)
        {
            _playbackCts = new CancellationTokenSource();
            var token = _playbackCts.Token;

            StreamManifest manifest = await _youtubeClient.Videos.Streams.GetManifestAsync(link);
            AudioOnlyStreamInfo streamInfo = manifest.GetAudioOnlyStreams().First();

            await using Stream youtubeStream = await _youtubeClient.Videos.Streams.GetAsync(streamInfo);
            await using AudioOutStream discord = audioClient.CreatePCMStream(AudioApplication.Mixed);

            _ffmpeg = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = "-hide_banner -loglevel panic -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                }
            };

            _ffmpeg.Start();

            var ffmpegIn = _ffmpeg.StandardInput.BaseStream;
            var ffmpegOut = _ffmpeg.StandardOutput.BaseStream;

            var inputTask = youtubeStream.CopyToAsync(ffmpegIn, token);
            var outputTask = ffmpegOut.CopyToAsync(discord, token);

            try
            {
                await Task.WhenAll(inputTask, outputTask);
            }
            catch (OperationCanceledException) { }
            finally
            {
                await discord.FlushAsync();
            }
        }
        
    }
}

