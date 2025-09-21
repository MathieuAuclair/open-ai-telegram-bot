namespace OlegBot.Models
{
    public class Session
    {
        public long UserId { get; set; }

        public long ChatId { get; set; }

        public int Index { get; set; }

        public Dictionary<string, string> Variables { get; set; }
    }
}