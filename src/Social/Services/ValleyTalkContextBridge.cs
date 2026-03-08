using System.Collections.Generic;
using System.Linq;
using ValleyTalk.Social.Models;

namespace ValleyTalk.Social.Services
{
    public class ValleyTalkContextBridge : IValleyTalkContextBridge
    {
        private readonly INpcProfileService profileService;

        public ValleyTalkContextBridge(INpcProfileService profileService)
        {
            this.profileService = profileService;
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
                RoomSummary = session.RoomMood.Summary,
                RelationshipSummary =
                    "Existing commitments matter and should influence loyalty, secrecy, jealousy, guilt, and social risk.",
                RecentVisibleBeats = session.Memory.Select(record => record.Summary).TakeLast(4).ToList()
            };
        }
    }
}
