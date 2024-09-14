using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using SolBot.Interfaces;


namespace SolBot
{
    internal class Program
    {

        public static void Main(string[] args) => MainAsync(args).GetAwaiter().GetResult(); 

        private static async Task MainAsync(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddUserSecrets(Assembly.GetExecutingAssembly())
                .Build();

            var serviceProvider = new ServiceCollection()
                .AddSingleton<IConfiguration>(configuration)
                .AddScoped<IBot, Bot>()
                .BuildServiceProvider();

            try
            {
                IBot bot = serviceProvider.GetRequiredService<IBot>();
                await bot.StartAsync(serviceProvider);
                Console.WriteLine("Connected to discord");
                
                
                do
                {
                    #if DEBUG
                    ConsoleKeyInfo keyInfo = Console.ReadKey(true);  

                    if(keyInfo.Key == ConsoleKey.Q)
                    {
                        Console.WriteLine("Closing bot application");
                        await bot.StopAsync();
                        break;
                    }
                    #endif
                }
                while (true);
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Environment.Exit(-1);
            }
        }
      
    }
}