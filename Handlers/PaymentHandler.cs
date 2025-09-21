using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Telegram.Bots.Http;

namespace OlegBot.Helpers
{
    public class PaymentHandler
    {
        private readonly HttpClient _http;
        private readonly string _secretKey;
        private readonly string _shopId;
        private readonly TimeSpan _timeout;
        private readonly TimeSpan _pollingInterval;
        private readonly string _botUsername;

        public PaymentHandler(
            string botUsername,
            string secretKey,
            string shopId,
            int timeoutInMinutes,
            int pollingIntervalInSeconds)
        {
            _http = new HttpClient();
            _secretKey = secretKey;
            _shopId = shopId;
            _timeout = TimeSpan.FromMinutes(timeoutInMinutes);
            _pollingInterval = TimeSpan.FromSeconds(pollingIntervalInSeconds);
            _botUsername = botUsername;
        }

        public async Task<Tuple<string, string>> ProcessPayment(string amount, string currency)
        {
            using var client = new HttpClient();
            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_shopId}:{_secretKey}"));

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);
            client.DefaultRequestHeaders.Add("Idempotence-Key", Guid.NewGuid().ToString());

            var body = new
            {
                amount = new { value = amount, currency },
                confirmation = new { type = "redirect", return_url = $"https://t.me/{_botUsername}" },
                capture = true,
                description = "Оплата через Telegram-бота"
            };

            var response = await client.PostAsync(
                "https://api.yookassa.ru/v3/payments",
                new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
            );

            var content = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(content);

            var paymentId = doc.RootElement.GetProperty("id").GetString();
            var confirmUrl = doc.RootElement
                .GetProperty("confirmation")
                .GetProperty("confirmation_url")
                .GetString();

            return new Tuple<string, string>(paymentId, confirmUrl);
        }

        public async Task<bool?> WaitForPaymentAsync(string paymentId)
        {
            var deadline = DateTime.UtcNow + _timeout;

            while (DateTime.UtcNow < deadline)
            {
                var status = await GetPaymentStatusAsync(paymentId);

                if (status == "succeeded")
                {
                    return true;
                }
                else if (status == "canceled" || status == "failed")
                {
                    return false;
                }

                await Task.Delay(_pollingInterval);
            }

            return null;
        }

        private async Task<string> GetPaymentStatusAsync(string paymentId)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.yookassa.ru/v3/payments/{paymentId}");
            var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_shopId}:{_secretKey}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authToken);

            var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("status").GetString();
        }
    }
}