using System.Text.Json.Serialization;
using OlegBot.Enums;

namespace OlegBot.Models
{
    public class ActionItem
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ActionType Type { get; set; }

        public List<string> Params { get; set; } = new();

        public int Next { get; set; }
    }
}