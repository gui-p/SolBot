using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SolBot.Interfaces;
using System.Reflection;
using Microsoft.Extensions.Hosting;

namespace SolBot.Services
{
    public sealed class BotService : IHostedService
    {

        private readonly DiscordSocketClient? _client;
        private readonly IConfiguration _configuration;
        private readonly CommandService _commands;
        private readonly IServiceProvider _serviceProvider;
        
        private readonly string _token;

        public BotService(IConfiguration configuration, IServiceProvider serviceProvider)
        {
            _configuration = configuration;
            _serviceProvider = serviceProvider;
            _token = _configuration["BOT_TOKEN"] ?? throw new Exception("Token not found");
            DiscordSocketConfig clientConfig = new() { GatewayIntents = Discord.GatewayIntents.MessageContent | GatewayIntents.AllUnprivileged};

            _client = new DiscordSocketClient(clientConfig);
            _commands = new CommandService();

        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _commands.AddModulesAsync(Assembly.GetExecutingAssembly(), _serviceProvider);
            await _client!.LoginAsync(TokenType.Bot, _token);
            await _client.StartAsync();

            _client.MessageReceived += HandleCommandAsync;
            LogService logService = new(_client, _commands);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_client is not null)
            {
                await _client.LogoutAsync();
                await _client.StopAsync();
            }
        }

        private async Task HandleCommandAsync(SocketMessage arg)
        {
            if (arg is not SocketUserMessage message || arg.Author.IsBot)
            {
                return;
            }

            int prefixPos = 0;

            bool isCommand = message.HasCharPrefix('&', ref prefixPos);
            if (isCommand)
            {
                await _commands.ExecuteAsync(
                    new SocketCommandContext(_client, message),
                    prefixPos,
                    _serviceProvider
                );
            }

        }

    }
}