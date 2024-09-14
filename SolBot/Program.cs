using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SolBot.Interfaces;
using SolBot.Services;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddUserSecrets(Assembly.GetExecutingAssembly());
builder.Services.AddSingleton<IMusicService, MusicService>();
builder.Services.AddHostedService<BotService>();
IHost host =  builder.Build();
await host.RunAsync();

