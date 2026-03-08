using System.Collections.Generic;
using System.Linq;
using ValleyTalk.Social.Models;

namespace ValleyTalk.Social.Services
{
    public class ValleyTalkContextBridge : IValleyTalkContextBridge
    {
        private readonly INpcProfileService profileService;
        private readonly ISessionMemoryService sessionMemoryService;

        public ValleyTalkContextBridge(INpcProfileService profileService, ISessionMemoryService sessionMemoryService)
        {
            this.profileService = profileService;
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
                RoomSummary = session.RoomMood.Summary + " Visible groupings: " + string.Join(", ", session.RoomMood.VisibleGroups) + ".",
                RelationshipSummary =
                    "Existing commitments matter and should influence loyalty, secrecy, jealousy, guilt, and social risk.",
                RecentVisibleBeats = this.sessionMemoryService.GetRecentVisible(session, npcName, 4).Select(record => record.Summary).ToList()
            };
        }
    }
}
