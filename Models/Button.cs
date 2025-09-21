using System.Text.Json.Serialization;
using OlegBot.Enums;

namespace OlegBot.Models
{
    public class Button
    {
        public string Text { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ButtonType Type { get; set; }

        public int Next { get; set; }

        public string Link { get; set; }
    }
}