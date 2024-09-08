using CliWrap;
using CliWrap.Buffered;
using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using System.Diagnostics;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace SolBot.Commands.Play
{

    public class PlayCommand : ModuleBase<SocketCommandContext>
    {
        //private readonly CancellationTokenSource _tokenSource;

        //private CurrentTrack _currentTrack;

        //public PlayCommand() 
        //{
        //    _tokenSource = new CancellationTokenSource();
        //}

        

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

        public async Task Play(string link, Func<IAudioClient, string, Task> playFunction)
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

        private Process CreateYTDLPProcess(string path)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "yt-dlp",
                Arguments = $"-x --audio-format mp3 {path} -o -",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }) ?? throw new Exception("Null yt-dlp process reference!");
        }

        private async Task StreamFromLocal(IAudioClient audioClient, string path)
        {
            using (var ffmpeg = CreateFFmpegProcess(path))
            using (var output = ffmpeg.StandardOutput.BaseStream)
            using (var discord = audioClient.CreatePCMStream(AudioApplication.Mixed))
            {
                try 
                {
                    await output.CopyToAsync(discord); 
                }
                finally { await discord.FlushAsync(); }
            }

        }

        private async Task StreamFromYoutube(IAudioClient audioClient, string link)
        {
            
            YoutubeClient _youtubeClient = new();
            StreamManifest streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(link);
            IStreamInfo streamInfo = streamManifest.GetAudioOnlyStreams().First();
            Console.WriteLine("Streaming link: " + streamInfo.Url);
            Stream stream = await _youtubeClient.Videos.Streams.GetAsync(streamInfo);

            var memoryStream = new MemoryStream();
            await Cli.Wrap("ffmpeg")
                .WithArguments(" -hide_banner -loglevel panic -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1")
                .WithStandardInputPipe(PipeSource.FromStream(stream))
                .WithStandardOutputPipe(PipeTarget.ToStream(memoryStream))
                .ExecuteAsync();

            Console.WriteLine("Memory size: " + memoryStream.Capacity + " Bytes");
            

            using (var discord = audioClient.CreatePCMStream(AudioApplication.Mixed))
            try
            {
                await discord.WriteAsync(memoryStream.GetBuffer());
            }
            finally
            {
                await discord.FlushAsync();
            }
        }
        private struct CurrentTrack
        {
            public CurrentTrack(//ValueTask trackTask, 
                CancellationTokenSource tokenSource)
            {
                //_currentTrackTask = trackTask;
                _tokenSource = tokenSource;
            }

            //public ValueTask? _currentTrackTask;
            public CancellationTokenSource _tokenSource;
        }
    }

    
}
