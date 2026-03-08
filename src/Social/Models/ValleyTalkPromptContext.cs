using System.Collections.Generic;
using System.Text;

namespace ValleyTalk.Social.Models
{
    public class ValleyTalkPromptContext
    {
        public string NpcName { get; set; } = string.Empty;
        public string Scene { get; set; } = string.Empty;
        public string PersonalitySummary { get; set; } = string.Empty;
        public string NightStateSummary { get; set; } = string.Empty;
        public string RoomSummary { get; set; } = string.Empty;
        public string RelationshipSummary { get; set; } = string.Empty;
        public List<string> RecentVisibleBeats { get; set; } = new List<string>();

        public string ToCompactSummary()
        {
            var builder = new StringBuilder();
            builder.AppendLine("Scene: " + this.Scene);
            builder.AppendLine("NPC: " + this.NpcName);
            builder.AppendLine("Baseline: " + this.PersonalitySummary);
            builder.AppendLine("Tonight: " + this.NightStateSummary);
            builder.AppendLine("Room: " + this.RoomSummary);

            if (!string.IsNullOrWhiteSpace(this.RelationshipSummary))
            {
                builder.AppendLine("Social risk: " + this.RelationshipSummary);
            }

            if (this.RecentVisibleBeats.Count > 0)
            {
                builder.AppendLine("Recent beats: " + string.Join("; ", this.RecentVisibleBeats));
            }

            return builder.ToString().TrimEnd();
        }
    }
}
