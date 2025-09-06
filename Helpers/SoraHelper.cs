using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace OlegBot.Helpers
{
    public static class SoraHelper
    {
        public static async Task<string> RequestImage(string prompt, string size)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            string apiKey = config["OpenAI:ApiKey"];

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new NullReferenceException("Could not retrieve the OpenAI API key in the configuration appsettings.json...");
            }

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var requestBody = new
            {
                prompt,
                model = "gpt-image-1",
                size
            };

            var response = await client.PostAsync(
                "https://api.openai.com/v1/images/generations",
                new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            );

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);


            string error = doc.RootElement.TryGetProperty("error", out var e)
                            && e.TryGetProperty("message", out var m)
                            ? m.GetString()
                            : null;

            if (error != null && error.Length > 0)
            {
                throw new Exception($"Исключение OpenAI API: {error}");
            }


            return doc.RootElement
                .GetProperty("data")[0]
                .GetProperty("b64_json")
                .GetString();
        }
    }
}