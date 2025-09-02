using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using OlegBot.Helpers;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace BotDashboard.Services
{
    public class BotService : BackgroundService
    {
        private readonly string _token;
        private readonly string _commandHandle;
        private readonly string[] _allowedUsers;
        private TelegramBotClient _bot;

        public BotService()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

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

                    case UpdateType.CallbackQuery:
                        await HandleButton(update.CallbackQuery!);
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
                    InlineKeyboardMarkup menuMarkup = new([[
                        InlineKeyboardButton.WithCallbackData("1024x1024"),
                        InlineKeyboardButton.WithCallbackData("1024x1536"),
                        InlineKeyboardButton.WithCallbackData("1536x1024"),
                        InlineKeyboardButton.WithCallbackData("auto")
                    ]]);

                    await _bot.SendMessage(
                        chatId: msg.Chat.Id,
                        text: "Какие размеры?",
                        parseMode: ParseMode.None,
                        replyParameters: new ReplyParameters
                        {
                            MessageId = msg.MessageId
                        },
                        replyMarkup: menuMarkup
                    );
                }
            }
        }

        async Task HandleButton(CallbackQuery query)
        {
            await _bot.SendMessage(
                chatId: query.Message.Chat.Id,
                text: $"Создание изображения по следующему запросу!",
                replyParameters: new ReplyParameters
                {
                    MessageId = query.Message.ReplyToMessage.Id
                }
            );

            try
            {
                var imageBytes = await SoraHelper.RequestImage(query.Message.ReplyToMessage.Text, query.Data);
                using var stream = new MemoryStream(imageBytes);
                await _bot.SendPhoto(
                    chatId: query.Message.Chat.Id,
                    photo: stream,
                    caption: "Вот ваше сгенерированное изображение",
                    replyParameters: new ReplyParameters
                    {
                        MessageId = query.Message.ReplyToMessage.Id
                    }
                );
            }
            catch (Exception exception)
            {
                await _bot.SendMessage(
                    chatId: query.Message.Chat.Id,
                    text: $"Произошла ошибка, запрос не обработан! `{exception.Message}`...",
                    replyParameters: new ReplyParameters
                    {
                        MessageId = query.Message.ReplyToMessage.Id
                    }
                );
            }

            await _bot.AnswerCallbackQuery(query.Id);
        }
    }
}