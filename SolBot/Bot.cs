using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SolBot.Interfaces;
using System.Reflection;

namespace SolBot
{
    public class Bot : IBot
    {
        private ServiceProvider? _serviceProvider;

        private readonly DiscordSocketClient _client;
        private readonly IConfiguration _configuration;
        private readonly CommandService _commands;

        public Bot(IConfiguration configuration)
        {
            _configuration = configuration;

            DiscordSocketConfig clientConfig = new() { GatewayIntents = Discord.GatewayIntents.MessageContent | GatewayIntents.AllUnprivileged};

            _client = new DiscordSocketClient(clientConfig);
            _commands = new CommandService();

        }

        public async Task StartAsync(ServiceProvider services)
        {
            string discordToken = _configuration["BOT_TOKEN"] ?? throw new Exception("Token not found");
            _serviceProvider = services;

            await _commands.AddModulesAsync(Assembly.GetExecutingAssembly(), _serviceProvider);

            await _client.LoginAsync(TokenType.Bot, discordToken);
            await _client.StartAsync();

            _client.MessageReceived += HandleCommandAsync;
            LogService logService = new(_client, _commands);
        }

        public async Task StopAsync()
        {
            if (_client != null)
            {
                await _client.LogoutAsync();
                await _client.StopAsync();
            }
        }

        public async Task HandleCommandAsync(SocketMessage arg)
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
                    _serviceProvider);

                return;
            }

        }

    }
}