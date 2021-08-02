#nullable enable
using System;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Args;

namespace WebApi.Application
{
    public class PokerTelegramBot : IDisposable
    {
        private readonly ILogger _logger;
        private TelegramBotClient _bot;
        private readonly string _token;

        public PokerTelegramBot(IConfiguration configuration, ILogger<PokerTelegramBot> logger)
        {
            _logger = logger;
            _token = configuration["Bot:Token"];
        }

        public void Start(CancellationToken cancellationToken)
        {
            cancellationToken.Register(() => Dispose());
            if (string.IsNullOrEmpty(_token))
            {
                _logger.LogError("Cannot start telegram bot with empty token value");
            }

            _bot = new TelegramBotClient(_token);
            _bot.OnMessage += OnMessage;
            _bot.StartReceiving();
        }

        private void OnMessage(object? sender, MessageEventArgs e)
        {
            const string UNEXPECTED_COMMAND = "Unexpected command";

            var text = e.Message.Text;
            if (string.IsNullOrEmpty(text))
            {
                _bot.SendTextMessageAsync(e.Message.Chat.Id, UNEXPECTED_COMMAND);
            }
            else
            {
                var (command, args) = ParseCommandArgs(text);
                switch (command)
                {
                    case "connect":
                        if (!args.Any())
                        {
                            _bot.SendTextMessageAsync(e.Message.Chat.Id, UNEXPECTED_COMMAND);
                        }

                        var session = args[0];
                        _bot.SendTextMessageAsync(e.Message.Chat.Id, "Connected");
                        _logger.LogInformation("User {user} connected to session {}", e.Message.From.Username,
                            session);
                        break;
                    default:
                        _bot.SendTextMessageAsync(e.Message.Chat.Id, UNEXPECTED_COMMAND);
                        break;
                }
            }
        }

        private static (string command, string[] args) ParseCommandArgs(string text)
        {
            var space = text.IndexOf(" ", StringComparison.Ordinal);
            if (space <= 0)
            {
                return (text[(text[0] == '/' ? 1 : 0)..], Array.Empty<string>());
            }

            var command = text.Substring(text[0] == '/' ? 1 : 0, space - 1);
            var args = text[(space + 1)..].Split(" ", StringSplitOptions.RemoveEmptyEntries);
            return (command, args);
        }

        public void Dispose()
        {
            _bot.StopReceiving();
        }
    }
}