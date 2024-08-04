using Microsoft.Extensions.DependencyInjection;

namespace SolBot.Interfaces
{
    public interface IBot
    {
        Task StartAsync(ServiceProvider services);
        Task StopAsync();
    }
}
