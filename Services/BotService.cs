using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using OlegBot.Helpers;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace BotDashboard.Services
{
    public class BotService : BackgroundService
    {
        private readonly string _token;
        private readonly string _commandHandle;
        private readonly string[] _allowedUsers;
        private readonly string _pageUrl;
        private TelegramBotClient _bot;
        private GitLabHelper _gitlab;

        public BotService()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            _pageUrl = config["Gitlab:PageUrl"];
            _gitlab = new GitLabHelper(
                config["Gitlab:Host"],
                config["Gitlab:ProjectId"],
                config["Gitlab:PrivateToken"]
            );

            _token = config["Telegram:BotToken"];
            _commandHandle = config["Telegram:CommandHandle"];
            _allowedUsers = config
                .GetSection("Telegram")
                .GetSection("AllowedUsers")
                .Get<string[]>();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {


            _bot = new TelegramBotClient(_token);

            using var cancellationToken = new CancellationTokenSource();

            _bot.StartReceiving(
                updateHandler: HandleUpdate,
                errorHandler: HandleError,
                receiverOptions: new ReceiverOptions(),
                cancellationToken: cancellationToken.Token
            );

            Console.WriteLine($"Bot is running...");

            // поддерживать услугу до ее отмены
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        async Task HandleUpdate(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
        {
            try
            {
                switch (update.Type)
                {
                    case UpdateType.Message:
                        await HandleMessage(update.Message!);
                        break;
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        async Task HandleError(ITelegramBotClient _, Exception exception, CancellationToken cancellationToken)
        {
            await Console.Error.WriteLineAsync(exception.Message);
        }

        async Task HandleMessage(Message msg)
        {
            if (msg.From == null || msg.Text == null)
            {
                return;
            }

            if (!_allowedUsers.Contains(msg.From.Username))
            {
                await _bot.SendMessage(
                        chatId: msg.Chat.Id,
                        text: "Несанкционированный доступ! Вы не внесены в белый список!",
                        parseMode: ParseMode.None,
                        replyParameters: new ReplyParameters
                        {
                            MessageId = msg.MessageId
                        }
                    );

                return;
            }

            if (msg.Text.StartsWith("/"))
            {
                Console.WriteLine($"{msg.From.Username} (IsBot: {msg.From.IsBot}) wrote command {msg.Text}");

                if (msg.Text.StartsWith(_commandHandle))
                {
                    var filename = Guid.NewGuid() + ".png";

                    try
                    {
                        var base64 = await SoraHelper.RequestImage(msg.ReplyToMessage.Text, msg.Text.Replace(_commandHandle, ""));

                        await _gitlab.CommitFileAsync("main", filename, base64);

                        var isPipelineSuccessful = await _gitlab.WaitForPipelineAsync("main");

                        if (isPipelineSuccessful)
                        {
                            var url = _pageUrl + filename;
                            var retryCount = 60; // 5 минута

                            using var client = new HttpClient();
                            while (!(await client.GetAsync(url)).IsSuccessStatusCode)
                            {
                                Console.WriteLine($"Ждем, когда {url} станет доступно...");
                                await Task.Delay(5000);
                                retryCount--;

                                if (retryCount < 0)
                                {
                                    throw new Exception($"Распространение изображения {filename} заняло больше времени, чем ожидалось");
                                }
                            }

                            await _bot.SendMessage(
                                    chatId: msg.Chat.Id,
                                    text: _pageUrl + filename,
                                    parseMode: ParseMode.None,
                                    replyParameters: new ReplyParameters
                                    {
                                        MessageId = msg.MessageId
                                    }
                                );

                            Console.WriteLine($"Доставленное изображение {filename}...");
                        }
                        else
                        {
                            await _bot.SendMessage(
                                chatId: msg.Chat.Id,
                                text: "Не удалось опубликовать ваше изображение!",
                                parseMode: ParseMode.None,
                                replyParameters: new ReplyParameters
                                {
                                    MessageId = msg.MessageId
                                }
                            );
                        }
                    }
                    catch (Exception exception)
                    {
                        await _bot.SendMessage(
                            chatId: msg.Chat.Id,
                            text: $"Произошла ошибка, запрос не обработан! `{exception?.Message}`...",
                            replyParameters: new ReplyParameters
                            {
                                MessageId = msg.Id
                            }
                        );
                    }
                }
            }
        }
    }
}