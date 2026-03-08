using System;
using System.Collections.Generic;
using StardewValley;
using ValleyTalk.Social.Models;

namespace ValleyTalk.Social.Services
{
    public class NpcNightStateService : INpcNightStateService
    {
        private readonly INpcProfileService profileService;
        private readonly Random random;

        public NpcNightStateService(INpcProfileService profileService)
        {
            this.profileService = profileService;
            this.random = new Random();
        }

        public Dictionary<string, NpcNightState> BuildNightStates(IEnumerable<string> npcNames)
        {
            var results = new Dictionary<string, NpcNightState>(StringComparer.OrdinalIgnoreCase);
            foreach (var npcName in npcNames)
            {
                if (!this.profileService.TryGetProfile(npcName, out var profile))
                {
                    continue;
                }

                results[npcName] = new NpcNightState
                {
                    NpcName = npcName,
                    Mood = this.SelectMood(profile),
                    BuzzLevel = this.SelectBuzz(profile),
                    PlayerHeat = this.SelectPlayerHeat(npcName, profile),
                    Openness = this.SelectOpenness(profile),
                    CurrentActivity = this.ParseActivity(profile.PreferredSaloonActivity),
                    LastInteractionBeat = InteractionBeat.None
                };
            }

            return results;
        }

        private MoodState SelectMood(NpcProfile profile)
        {
            if (profile.Temper >= 4 && this.random.NextDouble() < 0.3)
            {
                return MoodState.Irritated;
            }

            if (profile.Guardedness >= 4)
            {
                return MoodState.Guarded;
            }

            if (profile.Sociability >= 4)
            {
                return MoodState.Cheerful;
            }

            if (profile.Flirtiness >= 4 && this.random.NextDouble() < 0.35)
            {
                return MoodState.Lonely;
            }

            return MoodState.Relaxed;
        }

        private BuzzLevel SelectBuzz(NpcProfile profile)
        {
            if (profile.PreferredSaloonActivity.Equals(nameof(SocialActivity.Drinking), StringComparison.OrdinalIgnoreCase)
                && profile.AlcoholTolerance >= 3
                && this.random.NextDouble() < 0.4)
            {
                return BuzzLevel.Buzzed;
            }

            return BuzzLevel.Sober;
        }

        private PlayerHeat SelectPlayerHeat(string npcName, NpcProfile profile)
        {
            if (!Game1.getPlayerOrEventFarmer().friendshipData.TryGetValue(npcName, out Friendship friendship))
            {
                return PlayerHeat.Neutral;
            }

            var hearts = friendship.Points / 250;
            if (hearts >= 8 && profile.Flirtiness >= 3)
            {
                return PlayerHeat.Attracted;
            }

            if (hearts >= 4)
            {
                return PlayerHeat.Interested;
            }

            return PlayerHeat.Neutral;
        }

        private OpennessLevel SelectOpenness(NpcProfile profile)
        {
            if (profile.Guardedness >= 4)
            {
                return OpennessLevel.Closed;
            }

            if (profile.Guardedness <= 2 && profile.Sociability >= 4)
            {
                return OpennessLevel.Bold;
            }

            return OpennessLevel.Open;
        }

        private SocialActivity ParseActivity(string value)
        {
            return Enum.TryParse(value, true, out SocialActivity activity)
                ? activity
                : SocialActivity.Chatting;
        }
    }
}
