using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley;
using ValleyTalk.Social.Models;

namespace ValleyTalk.Social.Services
{
    public class SocialRelationshipService : ISocialRelationshipService
    {
        private readonly INpcProfileService profileService;

        public SocialRelationshipService(INpcProfileService profileService)
        {
            this.profileService = profileService;
        }

        public SocialRelationshipContext BuildContext(SocialSession session, NpcProfile profile, string npcName)
        {
            var placement = session.PlacementPlan.Placements.FirstOrDefault(candidate => string.Equals(candidate.NpcName, npcName, StringComparison.OrdinalIgnoreCase));
            var sameGroupNames = placement == null
                ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                : session.PlacementPlan.Placements
                    .Where(candidate => string.Equals(candidate.GroupId, placement.GroupId, StringComparison.OrdinalIgnoreCase))
                    .Select(candidate => candidate.NpcName)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var context = new SocialRelationshipContext
            {
                NpcName = npcName,
                CommittedPartnerName = profile.CommittedPartnerName ?? string.Empty,
                PlayerRelationshipStatus = this.GetPlayerRelationshipStatus(npcName)
            };

            if (!string.IsNullOrWhiteSpace(context.CommittedPartnerName))
            {
                context.CommittedPartnerPresent = session.Attendance.SelectedNpcNames
                    .Contains(context.CommittedPartnerName, StringComparer.OrdinalIgnoreCase);
            }

            context.JealousyRiskNpcNames = session.NightStates.Values
                .Where(state => !string.Equals(state.NpcName, npcName, StringComparison.OrdinalIgnoreCase))
                .Where(state => state.PlayerHeat >= PlayerHeat.Interested)
                .OrderByDescending(state => sameGroupNames.Contains(state.NpcName))
                .ThenByDescending(state => state.PlayerHeat)
                .ThenByDescending(state => state.Openness)
                .Select(state => state.NpcName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(3)
                .ToList();

            if (context.CommittedPartnerPresent && !string.IsNullOrWhiteSpace(context.CommittedPartnerName))
            {
                context.JealousyRiskNpcNames.RemoveAll(name => string.Equals(name, context.CommittedPartnerName, StringComparison.OrdinalIgnoreCase));
                context.JealousyRiskNpcNames.Insert(0, context.CommittedPartnerName);
            }

            context.SocialRiskLevel = context.CommittedPartnerPresent || context.JealousyRiskNpcNames.Count >= 2
                ? "high"
                : context.JealousyRiskNpcNames.Count == 1 || !string.IsNullOrWhiteSpace(context.CommittedPartnerName)
                    ? "medium"
                    : "low";

            return context;
        }

        public IReadOnlyList<InteractionRecord> ApplyObservedReactions(SocialSession session, NpcProfile targetProfile, SocialRelationshipContext relationshipContext, InteractionOutcome outcome)
        {
            if (session == null || targetProfile == null || relationshipContext == null || !outcome.VisibleToRoom)
            {
                return Array.Empty<InteractionRecord>();
            }

            if (outcome.Beat != InteractionBeat.Flirted
                && outcome.Beat != InteractionBeat.Danced
                && outcome.Beat != InteractionBeat.PrivateInvite
                && outcome.Beat != InteractionBeat.GiftedDrink)
            {
                return Array.Empty<InteractionRecord>();
            }

            var records = new List<InteractionRecord>();
            foreach (var observerName in relationshipContext.JealousyRiskNpcNames.Distinct(StringComparer.OrdinalIgnoreCase).Take(2))
            {
                if (!session.NightStates.TryGetValue(observerName, out var observerState))
                {
                    continue;
                }

                if (!this.profileService.TryGetProfile(observerName, out var observerProfile))
                {
                    continue;
                }

                observerState.LastInteractionBeat = InteractionBeat.Jealous;
                observerState.CurrentActivity = SocialActivity.Brooding;
                observerState.Openness = OpennessLevel.Closed;
                observerState.PlayerHeat = LowerHeat(observerState.PlayerHeat);
                observerState.Mood = observerProfile.Temper >= 4 || string.Equals(observerName, relationshipContext.CommittedPartnerName, StringComparison.OrdinalIgnoreCase)
                    ? MoodState.Irritated
                    : MoodState.Guarded;

                records.Add(new InteractionRecord
                {
                    TimeOfDay = Game1.timeOfDay,
                    NpcName = observerName,
                    RelatedNpcName = targetProfile.Name,
                    Action = "Observed",
                    ResultBeat = InteractionBeat.Jealous,
                    Summary = string.Equals(observerName, relationshipContext.CommittedPartnerName, StringComparison.OrdinalIgnoreCase)
                        ? observerName + " noticed the exchange with " + targetProfile.Name + " and looked openly jealous."
                        : observerName + " looked stung watching the farmer with " + targetProfile.Name + ".",
                    VisibleToRoom = true
                });
            }

            return records;
        }

        private string GetPlayerRelationshipStatus(string npcName)
        {
            if (!Game1.getPlayerOrEventFarmer().friendshipData.TryGetValue(npcName, out var friendship))
            {
                return "acquainted";
            }

            if (friendship.IsMarried() || friendship.IsRoommate())
            {
                return friendship.IsRoommate() ? "roommates" : "married";
            }

            if (friendship.IsEngaged())
            {
                return "engaged";
            }

            if (friendship.IsDating())
            {
                return "dating";
            }

            return friendship.Points >= 1500 ? "close" : "acquainted";
        }

        private static PlayerHeat LowerHeat(PlayerHeat current)
        {
            switch (current)
            {
                case PlayerHeat.Attracted:
                    return PlayerHeat.Interested;
                case PlayerHeat.Interested:
                    return PlayerHeat.Neutral;
                case PlayerHeat.Neutral:
                    return PlayerHeat.Negative;
                default:
                    return PlayerHeat.Negative;
            }
        }
    }
}
