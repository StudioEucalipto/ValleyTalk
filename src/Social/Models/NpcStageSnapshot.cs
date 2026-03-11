namespace ValleyTalk.Social.Models
{
    public class NpcStageSnapshot
    {
        public string NpcName { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public int TileX { get; set; }
        public int TileY { get; set; }
        public int FacingDirection { get; set; } = 2;
    }
}
