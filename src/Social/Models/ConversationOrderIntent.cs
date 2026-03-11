namespace ValleyTalk.Social.Models
{
    public class ConversationOrderIntent
    {
        public bool IsDrink { get; set; }
        public bool IsMeal { get; set; }
        public bool ForRoom { get; set; }
        public string ItemName { get; set; } = string.Empty;
    }
}
