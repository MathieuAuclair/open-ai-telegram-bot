using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace OlegBot.Helpers
{
    public class YooWalletPaymentHandler
    {
        public Dictionary<string, string> PendingRequestIds { get; set; }

        private readonly TimeSpan _timeout;
        private readonly TimeSpan _pollingInterval;
        private readonly TelegramBotClient _bot;
        private readonly YooWalletHelper _walletHelper;

        public YooWalletPaymentHandler(
            string clientId,
            string accessToken,
            string redirectUri,
            int timeoutInMinutes,
            int pollingIntervalInSeconds,
            TelegramBotClient bot)
        {
            PendingRequestIds = new Dictionary<string, string>();
            
            _bot = bot;
            _timeout = TimeSpan.FromMinutes(timeoutInMinutes);
            _pollingInterval = TimeSpan.FromSeconds(pollingIntervalInSeconds);
            _walletHelper = new YooWalletHelper(clientId, accessToken, redirectUri);
        }

        public async Task ProcessPayment(long userId, long chatId)
        {
            var url = _walletHelper.GenerateAuthUrl($"{userId}-{chatId}");

            await _bot.SendMessage(
                chatId,
                "Чтобы оплачивать через ЮMoney, авторизуйтесь по ссылке ниже и скопируйте код из адресной строки:",
                parseMode: ParseMode.Markdown,
                replyMarkup: new InlineKeyboardMarkup(
                    InlineKeyboardButton.WithUrl("Авторизация ЮMoney", url)
                )
            );
        }

        public async Task<bool> WaitForPaymentAsync(string requestId, string amount, string walletNumber)
        {
            var deadline = DateTime.UtcNow + _timeout;

            while (DateTime.UtcNow < deadline)
            {
                if (PendingRequestIds.ContainsKey(requestId))
                {
                    return await GetPaymentStatusAsync(requestId, amount, walletNumber);
                }

                await Task.Delay(_pollingInterval);
            }

            return false;
        }

        private async Task<bool> GetPaymentStatusAsync(string requestId, string amount, string walletNumber)
        {
            var status = await _walletHelper.RequestPaymentFromUser(
                accessToken: PendingRequestIds[requestId],
                amount: float.Parse(amount),
                walletNumber,
                requestId
            );

            // Removing processed event
            PendingRequestIds.Remove(requestId);

            return status;
        }
    }
}
