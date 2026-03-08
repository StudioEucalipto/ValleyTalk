namespace ValleyTalk.Social.Models
{
    public class NpcPlacement
    {
        public string NpcName { get; set; } = string.Empty;
        public string SpotId { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string GroupId { get; set; } = string.Empty;
        public SocialActivity Activity { get; set; } = SocialActivity.Chatting;
        public int TileX { get; set; }
        public int TileY { get; set; }
        public int FacingDirection { get; set; } = 2;
    }
}
