namespace ValleyTalk.Social.Models
{
    public class InteractionOutcome
    {
        public SocialActionType Action { get; set; } = SocialActionType.Chat;
        public InteractionBeat Beat { get; set; } = InteractionBeat.None;
        public string Summary { get; set; } = string.Empty;
        public bool VisibleToRoom { get; set; } = true;
    }
}
