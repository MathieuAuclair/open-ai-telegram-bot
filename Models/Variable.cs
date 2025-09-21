using System.Text.Json.Serialization;
using OlegBot.Enums;

namespace OlegBot.Models
{
    public class Variable
    {
        public string Name { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public VariableType Type { get; set; }

        public string Value { get; set; }

        public int? Next { get; set; }
    }
}