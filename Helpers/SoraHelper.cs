using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace OlegBot.Helpers
{
    public static class SoraHelper
    {
        public static async Task<string> RequestImage(string prompt, string size)
        {
            //return "data:image/png;base64,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";

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