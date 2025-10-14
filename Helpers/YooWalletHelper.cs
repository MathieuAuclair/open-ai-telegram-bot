using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OlegBot.Helpers
{
    public class YooWalletHelper
    {
        private readonly HttpClient _httpClient;
        private string _clientId;
        private string _clientSecret;
        private string _redirectUri;

        public YooWalletHelper(string clientId, string clientSecret, string redirectUri)
        {
            _httpClient = new HttpClient();
            _clientId = clientId;
            _clientSecret = clientSecret;
            _redirectUri = redirectUri;
        }

        public string GenerateAuthUrl(string state)
        {
            var parameters = new Dictionary<string, string>
            {
                ["client_id"] = _clientId,
                ["response_type"] = "code",
                ["redirect_uri"] = _redirectUri + $"?state={state}",
                ["scope"] = "account-info operation-history payment-p2p"
            };

            if (!string.IsNullOrEmpty(state))
            {
                parameters["state"] = state;
            }

            var query = string.Join("&", parameters.Select(x => $"{x.Key}={Uri.EscapeDataString(x.Value)}"));
            return $"https://yoomoney.ru/oauth/authorize?{query}";
        }

        public async Task<string> GetAccessToken(string code, string state)
        {
            try
            {
                var requestData = new Dictionary<string, string>
                {
                    ["code"] = code,
                    ["state"] = state,
                    ["client_id"] = _clientId,
                    ["client_secret"] = _clientSecret,
                    ["redirect_uri"] = _redirectUri,
                    ["grant_type"] = "authorization_code"
                };

                var response = await _httpClient.PostAsync(
                    "https://yoomoney.ru/oauth/token",
                    new FormUrlEncodedContent(requestData));

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var tokenData = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                    return tokenData?["access_token"]?.ToString();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Ошибка: {error}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Исключение: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> RequestPaymentFromUser(string accessToken, float amount, string walletNumber, string requestId)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var formData = new List<KeyValuePair<string, string>>
            {
                new("pattern_id", "p2p"),
                new("to", walletNumber),
                new("amount", amount.ToString("0.00", CultureInfo.InvariantCulture)),
                new("message", $"Service fee ${amount}RUB."),
                new("comment", $"Payment from user {requestId}"),
                new("label", requestId)
            };

            var response = await client.PostAsync(
                "https://yoomoney.ru/api/request-payment",
                new FormUrlEncodedContent(formData)
            );

            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Request Payment Response: {content}");

            dynamic responseObj = Newtonsoft.Json.JsonConvert.DeserializeObject(content);

            if (responseObj.status == "success")
            {
                string paymentRequestId = responseObj.request_id;
                return await ProcessPayment(accessToken, paymentRequestId);
            }

            if (responseObj.error != null)
            {
                string error = responseObj.error;
                string errorDesc = responseObj.error_description ?? "Unknown error";
                Console.WriteLine($"Payment error: {error} - {errorDesc}");
            }

            return false;
        }

        private async Task<bool> ProcessPayment(string accessToken, string requestId)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var formData = new List<KeyValuePair<string, string>>
            {
                new("request_id", requestId)
            };

            var response = await client.PostAsync(
                "https://yoomoney.ru/api/process-payment",
                new FormUrlEncodedContent(formData)
            );

            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Process Payment Response: {content}");

            if (content.Contains("error"))
            {
                return false;
            }

            return response.IsSuccessStatusCode;
        }
    }
}