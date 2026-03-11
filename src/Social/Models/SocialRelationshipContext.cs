namespace ValleyTalk.Social.Models
{
    public class SocialRelationshipContext
    {
        public string NpcName { get; set; } = string.Empty;
        public string PlayerRelationshipStatus { get; set; } = "acquainted";
        public string CommittedPartnerName { get; set; } = string.Empty;
        public bool CommittedPartnerPresent { get; set; }
        public string LikelyObserverName { get; set; } = string.Empty;
        public string SocialRiskLevel { get; set; } = "low";
    }
}
