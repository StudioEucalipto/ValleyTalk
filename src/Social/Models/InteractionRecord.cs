namespace ValleyTalk.Social.Models
{
    public class InteractionRecord
    {
        public int TimeOfDay { get; set; }
        public string NpcName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public InteractionBeat ResultBeat { get; set; } = InteractionBeat.None;
        public string Summary { get; set; } = string.Empty;
        public bool VisibleToRoom { get; set; } = true;
    }
}
