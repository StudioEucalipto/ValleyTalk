namespace ValleyTalk.Social.Models
{
    public class SocialActionRequest
    {
        public SocialActionType Action { get; set; } = SocialActionType.Chat;
        public string TargetNpcName { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public int Price { get; set; }
        public bool TargetsRoom { get; set; }
    }
}
