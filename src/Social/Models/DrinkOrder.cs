namespace ValleyTalk.Social.Models
{
    public class DrinkOrder
    {
        public string BuyerName { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public int Price { get; set; }
        public bool ForRoom { get; set; }
    }
}
