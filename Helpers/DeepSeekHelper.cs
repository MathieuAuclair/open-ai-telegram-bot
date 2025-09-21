using System.Text;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace OlegBot.Helpers
{
    public static class DeepSeekHelper
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public static async Task<string> ProcessPrompt(string prompt, string systemPrompt, float temperature)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            string apiKey = config["DeepSeek:ApiKey"];
            const string apiUrl = "https://api.deepseek.com/v1/chat/completions";

            var requestBody = new
            {
                model = "deepseek-chat",
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = prompt }
                },
                temperature
            };

            var jsonContent = JsonConvert.SerializeObject(requestBody);
            var request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };

            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var response = await _httpClient.SendAsync(request);

            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var responseObject = JsonConvert.DeserializeObject<dynamic>(responseJson);

            return responseObject?.choices?[0]?.message?.content?.ToString() ?? "No response content.";
        }
    }
}