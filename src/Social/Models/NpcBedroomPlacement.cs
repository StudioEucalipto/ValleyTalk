namespace ValleyTalk.Social.Models
{
    public class NpcBedroomPlacement
    {
        public string NpcName { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public int PlayerTileX { get; set; } = -1;
        public int PlayerTileY { get; set; } = -1;
        public int NpcTileX { get; set; } = -1;
        public int NpcTileY { get; set; } = -1;
    }
}
