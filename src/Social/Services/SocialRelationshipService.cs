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

            if (context.CommittedPartnerPresent && !string.IsNullOrWhiteSpace(context.CommittedPartnerName))
            {
                context.LikelyObserverName = context.CommittedPartnerName;
            }
            else
            {
                context.LikelyObserverName = session.NightStates.Values
                    .Where(state => !string.Equals(state.NpcName, npcName, StringComparison.OrdinalIgnoreCase))
                    .Where(state => sameGroupNames.Contains(state.NpcName))
                    .Where(state => state.PlayerHeat == PlayerHeat.Attracted)
                    .OrderByDescending(state => state.Openness)
                    .Select(state => state.NpcName)
                    .FirstOrDefault() ?? string.Empty;
            }

            context.SocialRiskLevel = context.CommittedPartnerPresent
                ? "high"
                : !string.IsNullOrWhiteSpace(context.LikelyObserverName) || !string.IsNullOrWhiteSpace(context.CommittedPartnerName)
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

            if (string.IsNullOrWhiteSpace(relationshipContext.LikelyObserverName))
            {
                return Array.Empty<InteractionRecord>();
            }

            if (outcome.Beat != InteractionBeat.Flirted
                && outcome.Beat != InteractionBeat.PrivateInvite)
            {
                return Array.Empty<InteractionRecord>();
            }

            var observerName = relationshipContext.LikelyObserverName;
            if (!session.NightStates.TryGetValue(observerName, out var observerState))
            {
                return Array.Empty<InteractionRecord>();
            }

            if (!this.profileService.TryGetProfile(observerName, out var observerProfile))
            {
                return Array.Empty<InteractionRecord>();
            }

            observerState.LastInteractionBeat = InteractionBeat.Jealous;
            observerState.Openness = observerState.Openness == OpennessLevel.Bold ? OpennessLevel.Open : OpennessLevel.Closed;
            observerState.PlayerHeat = string.Equals(observerName, relationshipContext.CommittedPartnerName, StringComparison.OrdinalIgnoreCase)
                ? LowerHeat(observerState.PlayerHeat)
                : observerState.PlayerHeat;
            observerState.Mood = string.Equals(observerName, relationshipContext.CommittedPartnerName, StringComparison.OrdinalIgnoreCase)
                ? (observerProfile.Temper >= 4 ? MoodState.Irritated : MoodState.Guarded)
                : MoodState.Guarded;

            return new[]
            {
                new InteractionRecord
                {
                    TimeOfDay = Game1.timeOfDay,
                    NpcName = observerName,
                    Action = "Observed",
                    ResultBeat = InteractionBeat.Jealous,
                    Summary = string.Equals(observerName, relationshipContext.CommittedPartnerName, StringComparison.OrdinalIgnoreCase)
                        ? observerName + " noticed the exchange with " + targetProfile.Name + " and went quiet."
                        : observerName + " seemed to notice the exchange with " + targetProfile.Name + ".",
                    VisibleToRoom = false
                }
            };
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
