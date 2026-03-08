namespace ValleyTalk.Social.Models
{
    public class SaloonSpot
    {
        public string Id { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string Activity { get; set; } = string.Empty;
        public string GroupId { get; set; } = string.Empty;
        public int TileX { get; set; }
        public int TileY { get; set; }
        public int FacingDirection { get; set; } = 2;
        public int Priority { get; set; } = 1;
    }
}
