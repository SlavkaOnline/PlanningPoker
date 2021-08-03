using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Telegram.Bot;
using WebApi.Application;

namespace WebApi.Infra
{
    public class BotHostedService : IHostedService
    {
        private readonly PlanningTelegramBot.Bot _botOld;

        public BotHostedService(IConfiguration configuration, IClusterClient siloClient, ILoggerFactory loggerFactory)
        {
            _botOld = new PlanningTelegramBot.Bot(configuration["Bot:Token"], siloClient, loggerFactory);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Task.Run(async () =>
            {
               await _botOld.Start(cancellationToken);
            }, cancellationToken);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}