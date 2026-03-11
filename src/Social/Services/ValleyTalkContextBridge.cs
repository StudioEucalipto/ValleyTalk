using System.Collections.Generic;
using System.Linq;
using ValleyTalk.Social.Models;

namespace ValleyTalk.Social.Services
{
    public class ValleyTalkContextBridge : IValleyTalkContextBridge
    {
        private readonly INpcProfileService profileService;
        private readonly ISocialRelationshipService relationshipService;
        private readonly ISessionMemoryService sessionMemoryService;

        public ValleyTalkContextBridge(INpcProfileService profileService, ISocialRelationshipService relationshipService, ISessionMemoryService sessionMemoryService)
        {
            this.profileService = profileService;
            this.relationshipService = relationshipService;
            this.sessionMemoryService = sessionMemoryService;
        }

        public ValleyTalkPromptContext BuildPromptContext(SocialSession session, string npcName)
        {
            NpcProfile profile;
            if (!this.profileService.TryGetProfile(npcName, out profile))
            {
                return new ValleyTalkPromptContext
                {
                    NpcName = npcName,
                    Scene = "Saturday night at the Stardrop Saloon.",
                    RoomSummary = session.RoomMood.Summary
                };
            }

            NpcNightState nightState;
            if (!session.NightStates.TryGetValue(npcName, out nightState))
            {
                nightState = new NpcNightState
                {
                    NpcName = npcName
                };
            }

            var placement = session.PlacementPlan.Placements.FirstOrDefault(candidate => candidate.NpcName == npcName);
            var sameGroup = placement == null
                ? new List<string>()
                : session.PlacementPlan.Placements
                    .Where(candidate => candidate.GroupId == placement.GroupId && candidate.NpcName != npcName)
                    .Select(candidate => candidate.NpcName)
                    .ToList();
            var relationshipContext = this.relationshipService.BuildContext(session, profile, npcName);

            return new ValleyTalkPromptContext
            {
                NpcName = npcName,
                Scene = "Saturday night at the Stardrop Saloon.",
                PersonalitySummary =
                    "Traits: sociability " + profile.Sociability + "/5, flirtiness " + profile.Flirtiness + "/5, loyalty " + profile.LoyaltyToCommitments +
                    "/5, temper " + profile.Temper + "/5, alcohol tolerance " + profile.AlcoholTolerance + "/5, guardedness " + profile.Guardedness +
                    "/5. Usually gravitates toward " + profile.PreferredSaloonActivity.ToLowerInvariant() + ". " + profile.RoleplayNotes,
                NightStateSummary =
                    "Mood " + nightState.Mood + ", buzz " + nightState.BuzzLevel + ", player heat " + nightState.PlayerHeat + ", openness " +
                    nightState.Openness + ", currently " + nightState.CurrentActivity + ", last beat " + nightState.LastInteractionBeat + ".",
                PositionSummary = placement == null
                    ? string.Empty
                    : profile.Name + " is in the " + placement.Area.ToLowerInvariant() + (sameGroup.Count > 0
                        ? " with " + string.Join(", ", sameGroup) + "."
                        : " on their own."),
                RoomSummary = session.RoomMood.Summary + " Visible groupings: " + string.Join(", ", session.RoomMood.VisibleGroups) + ".",
                RelationshipSummary = this.BuildRelationshipSummary(relationshipContext),
                RecentVisibleBeats = this.sessionMemoryService.GetRecentVisible(session, npcName, 4).Select(record => record.Summary).ToList()
            };
        }

        private string BuildRelationshipSummary(SocialRelationshipContext relationshipContext)
        {
            var pieces = new List<string>
            {
                "Farmer relationship status: " + relationshipContext.PlayerRelationshipStatus + "."
            };

            if (!string.IsNullOrWhiteSpace(relationshipContext.CommittedPartnerName))
            {
                pieces.Add(relationshipContext.CommittedPartnerPresent
                    ? "Committed to " + relationshipContext.CommittedPartnerName + ", who is in the room."
                    : "Committed to " + relationshipContext.CommittedPartnerName + ", so secrecy and guilt still matter.");
            }

            if (!string.IsNullOrWhiteSpace(relationshipContext.LikelyObserverName))
            {
                pieces.Add("One nearby person who may notice: " + relationshipContext.LikelyObserverName + ".");
            }

            pieces.Add("Social risk tonight is " + relationshipContext.SocialRiskLevel + ".");
            return string.Join(" ", pieces);
        }
    }
}
