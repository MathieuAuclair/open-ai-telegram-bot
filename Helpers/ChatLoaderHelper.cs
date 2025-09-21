using System.Text.Json;
using OlegBot.Models;

namespace OlegBot.Helpers
{
    public class ChatLoaderHelper
    {
        public class Root
        {
            public List<Message> Messages { get; set; }
        }

        public static Root LoadConfiguration()
        {
            var jsonString = File.ReadAllText("messages.json");
            
            return JsonSerializer.Deserialize<Root>(jsonString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
    }
}