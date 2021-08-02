using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using WebApi.Application;

namespace WebApi.Infra
{
    public class BotHostedService : IHostedService
    {
        private readonly PokerTelegramBot _bot;

        public BotHostedService(PokerTelegramBot bot)
        {
            _bot = bot;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Task.Run(() =>
            {
                _bot.Start(cancellationToken);
            }, cancellationToken);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _bot.Dispose();
            return Task.CompletedTask;
        }
    }
}