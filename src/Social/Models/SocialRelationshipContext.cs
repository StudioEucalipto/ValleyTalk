using System.Collections.Generic;

namespace ValleyTalk.Social.Models
{
    public class SocialRelationshipContext
    {
        public string NpcName { get; set; } = string.Empty;
        public string PlayerRelationshipStatus { get; set; } = "acquainted";
        public string CommittedPartnerName { get; set; } = string.Empty;
        public bool CommittedPartnerPresent { get; set; }
        public List<string> JealousyRiskNpcNames { get; set; } = new List<string>();
        public string SocialRiskLevel { get; set; } = "low";
    }
}
