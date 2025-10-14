using OlegBot.Bus;
using OlegBot.Handlers;
using OlegBot.Helpers;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using static OlegBot.Helpers.ChatLoaderHelper;

namespace BotDashboard.Services
{
    public class BotService : BackgroundService
    {
        private IConfiguration _config;
        private readonly Root _chatConfiguration;
        private YooWalletPaymentHandler _walletPaymentHandler;
        private YooKassaPaymentHandler _kassaPaymentHandler;
        private UpdateHandler _updateHandler;
        private TelegramBotClient _bot;

        public BotService()
        {
            _config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            _chatConfiguration = LoadConfiguration();

            AuthEventBus.OnWalletLinked += async (requestId, token) =>
            {
                if (string.IsNullOrWhiteSpace(requestId))
                {
                    Console.WriteLine("[WARNING]: Invalid BUS event for wallet authentication...");
                    return;
                }
                
                _walletPaymentHandler.PendingRequestIds.Add(requestId, token);
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _bot = new TelegramBotClient(_config["Telegram:BotToken"]);

            var botData = await _bot.GetMe();

            await _bot.SetMyCommands([
                new BotCommand {
                    Command = _config["Telegram:CommandHandle"].ToString(),
                    Description = _config["Telegram:CommandDescription"].ToString()
                },
            ]);

            _kassaPaymentHandler = new YooKassaPaymentHandler(
                botData.Username,
                _config["UKassa:ApiKeySecret"],
                _config["UKassa:ShopId"],
                int.Parse(_config["UKassa:PaymentTimeoutInMinutes"]),
                int.Parse(_config["UKassa:PollingIntervalInSeconds"])
            );

            _walletPaymentHandler = new YooWalletPaymentHandler(
                _config["UMoney:ClientId"],
                _config["UMoney:PrivateToken"],
                _config["UMoney:ReturnUrl"],
                int.Parse(_config["UMoney:PaymentTimeoutInMinutes"]),
                int.Parse(_config["UMoney:PollingIntervalInSeconds"]),
                _bot
            );

            _updateHandler = new UpdateHandler(
                _bot,
                botData.Username,
                _config,
                _walletPaymentHandler,
                _kassaPaymentHandler,
                _chatConfiguration,
                _config["Telegram:CommandHandle"]
            );

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
            while (_updateHandler == null)
            {
                return;
            }

            try
            {
                switch (update.Type)
                {
                    case UpdateType.Message:
                        await _updateHandler.HandleMessageUpdate(update);
                        break;
                    case UpdateType.CallbackQuery:
                        await _updateHandler.HandleCallbackUpdate(update);
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
    }
}