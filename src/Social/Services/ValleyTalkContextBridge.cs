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
                    Scene = this.BuildSceneDescription(session),
                    RoomSummary = this.BuildRoomSummary(session)
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
                Scene = this.BuildSceneDescription(session),
                PersonalitySummary =
                    "Traits: sociability " + profile.Sociability + "/5, flirtiness " + profile.Flirtiness + "/5, loyalty " + profile.LoyaltyToCommitments +
                    "/5, temper " + profile.Temper + "/5, alcohol tolerance " + profile.AlcoholTolerance + "/5, guardedness " + profile.Guardedness +
                    "/5. Usually gravitates toward " + profile.PreferredSaloonActivity.ToLowerInvariant() + ". " + profile.RoleplayNotes,
                NightStateSummary =
                    "Mood " + nightState.Mood + ", buzz " + nightState.BuzzLevel + ", player heat " + nightState.PlayerHeat + ", openness " +
                    nightState.Openness + ", currently " + nightState.CurrentActivity + ", last beat " + nightState.LastInteractionBeat + ".",
                PositionSummary = session.Phase == SocialSessionPhase.FollowUpScene
                    ? this.BuildFollowUpPositionSummary(session, profile.Name)
                    : placement == null
                    ? string.Empty
                    : profile.Name + " is in the " + placement.Area.ToLowerInvariant() + (sameGroup.Count > 0
                        ? " with " + string.Join(", ", sameGroup) + "."
                        : " on their own."),
                RoomSummary = this.BuildRoomSummary(session),
                RelationshipSummary = this.BuildRelationshipSummary(relationshipContext),
                RecentVisibleBeats = this.sessionMemoryService.GetRecentVisible(session, npcName, 4).Select(record => record.Summary).ToList()
            };
        }

        private string BuildSceneDescription(SocialSession session)
        {
            if (session?.PendingScene == null || session.Phase != SocialSessionPhase.FollowUpScene)
            {
                return "Saturday night at the Stardrop Saloon.";
            }

            switch (session.PendingScene.SceneType)
            {
                case PostSocialSceneType.OutsideFight:
                    return "Late Saturday night outside the Stardrop Saloon, where the tension from inside followed you into the street.";
                case PostSocialSceneType.PlayerBedroomRomance:
                    return "Late Saturday night in the farmhouse bedroom, after leaving the saloon together.";
                case PostSocialSceneType.NpcBedroomRomance:
                    return "Late Saturday night in " + session.PendingScene.TargetNpcName + "'s bedroom, after leaving the saloon together.";
                default:
                    return "Saturday night after leaving the Stardrop Saloon.";
            }
        }

        private string BuildRoomSummary(SocialSession session)
        {
            if (session?.PendingScene == null || session.Phase != SocialSessionPhase.FollowUpScene)
            {
                return session.RoomMood.Summary + " Visible groupings: " + string.Join(", ", session.RoomMood.VisibleGroups) + ".";
            }

            switch (session.PendingScene.SceneType)
            {
                case PostSocialSceneType.OutsideFight:
                    return "The warmth of the saloon is gone. The night air is colder, the street is emptier, and the argument now has nowhere to hide.";
                case PostSocialSceneType.PlayerBedroomRomance:
                case PostSocialSceneType.NpcBedroomRomance:
                    return "The room is private and close. The social performance is over, but the night still carries the mood and risk of what happened in the saloon.";
                default:
                    return session.RoomMood.Summary;
            }
        }

        private string BuildFollowUpPositionSummary(SocialSession session, string npcName)
        {
            if (session?.PendingScene == null || !string.Equals(session.PendingScene.TargetNpcName, npcName))
            {
                return string.Empty;
            }

            switch (session.PendingScene.SceneType)
            {
                case PostSocialSceneType.OutsideFight:
                    return npcName + " is standing outside with the farmer near the saloon entrance.";
                case PostSocialSceneType.PlayerBedroomRomance:
                    return npcName + " is in the farmhouse bedroom with the farmer.";
                case PostSocialSceneType.NpcBedroomRomance:
                    return npcName + " is in their bedroom with the farmer.";
                default:
                    return string.Empty;
            }
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
