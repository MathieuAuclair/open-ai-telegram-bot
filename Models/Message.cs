namespace OlegBot.Models
{
    public class Message
    {
        public int Id { get; set; }

        public string Text { get; set; }

        public List<ActionItem> Actions { get; set; } = new();

        public List<Variable> Variables { get; set; } = new();

        public List<Button> Buttons { get; set; } = new();
    }
}
