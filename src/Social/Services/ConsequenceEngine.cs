using System;
using System.Linq;
using ValleyTalk.Social.Models;

namespace ValleyTalk.Social.Services
{
    public class ConsequenceEngine : IConsequenceEngine
    {
        private readonly IConversationActionClassifier classifier;

        public ConsequenceEngine(IConversationActionClassifier classifier)
        {
            this.classifier = classifier;
        }

        public InteractionOutcome ApplyConversation(NpcProfile profile, NpcNightState nightState, SocialRelationshipContext relationshipContext, string dialogueText, bool isPlayerLine)
        {
            var action = this.classifier.Classify(dialogueText, isPlayerLine);
            var outcome = new InteractionOutcome
            {
                Action = action
            };

            switch (action)
            {
                case SocialActionType.Flirt:
                    if (ShouldRejectFlirt(profile, nightState, relationshipContext))
                    {
                        outcome.Beat = InteractionBeat.Rejected;
                        outcome.Summary = relationshipContext.CommittedPartnerPresent
                            ? profile.Name + " answered the flirt carefully and let it go when " + relationshipContext.CommittedPartnerName + " stayed nearby."
                            : profile.Name + " did not really invite the flirt to go further.";
                        outcome.VisibleToRoom = false;
                        nightState.Mood = MoodState.Guarded;
                        nightState.Openness = nightState.Openness == OpennessLevel.Bold ? OpennessLevel.Open : OpennessLevel.Closed;
                        nightState.PlayerHeat = LowerHeat(nightState.PlayerHeat);
                        break;
                    }

                    outcome.Beat = InteractionBeat.Flirted;
                    outcome.Summary = isPlayerLine
                        ? relationshipContext.CommittedPartnerPresent && profile.LoyaltyToCommitments >= 3
                            ? "The farmer flirted with " + profile.Name + ", and " + profile.Name + " answered carefully under the room's eyes."
                            : "The farmer flirted with " + profile.Name + "."
                        : profile.Name + " answered with a flirtatious tone.";
                    nightState.PlayerHeat = RaiseHeat(nightState.PlayerHeat);
                    nightState.Openness = relationshipContext.CommittedPartnerPresent && profile.LoyaltyToCommitments >= 3
                        ? OpennessLevel.Open
                        : OpennessLevel.Bold;
                    nightState.Mood = nightState.Mood == MoodState.Irritated ? MoodState.Guarded : MoodState.Cheerful;
                    break;

                case SocialActionType.InviteDance:
                    if (ShouldRejectInvitation(profile, nightState, relationshipContext))
                    {
                        outcome.Beat = InteractionBeat.Rejected;
                        outcome.Summary = profile.Name + " sidestepped the dance invitation without making it a scene.";
                        outcome.VisibleToRoom = false;
                        nightState.Mood = MoodState.Guarded;
                        nightState.CurrentActivity = SocialActivity.Chatting;
                        break;
                    }

                    outcome.Beat = InteractionBeat.Danced;
                    outcome.Summary = isPlayerLine
                        ? "The farmer steered the conversation toward dancing."
                        : profile.Name + " brought dancing into the conversation.";
                    nightState.CurrentActivity = SocialActivity.Dancing;
                    nightState.PlayerHeat = RaiseHeat(nightState.PlayerHeat);
                    break;

                case SocialActionType.Apologize:
                    outcome.Beat = InteractionBeat.Chatted;
                    outcome.Summary = isPlayerLine
                        ? "The farmer tried to smooth things over with " + profile.Name + "."
                        : profile.Name + " sounded calmer after the apology.";
                    if (nightState.Mood == MoodState.Irritated)
                    {
                        nightState.Mood = MoodState.Relaxed;
                    }
                    if (nightState.Openness == OpennessLevel.Closed)
                    {
                        nightState.Openness = OpennessLevel.Open;
                    }
                    break;

                case SocialActionType.Provoke:
                    outcome.Beat = InteractionBeat.Argued;
                    outcome.Summary = isPlayerLine
                        ? "The farmer pushed " + profile.Name + " into a sharper exchange."
                        : profile.Name + " sounded tense and argumentative.";
                    nightState.Mood = MoodState.Irritated;
                    nightState.PlayerHeat = LowerHeat(nightState.PlayerHeat);
                    break;

                case SocialActionType.SuggestPrivateConversation:
                    if (ShouldRejectPrivateConversation(profile, nightState, relationshipContext))
                    {
                        outcome.Beat = InteractionBeat.Rejected;
                        outcome.Summary = profile.Name + " let the private invitation pass and stayed out in the room.";
                        outcome.VisibleToRoom = false;
                        nightState.Mood = MoodState.Guarded;
                        nightState.Openness = OpennessLevel.Open;
                        break;
                    }

                    outcome.Beat = InteractionBeat.PrivateInvite;
                    outcome.Summary = isPlayerLine
                        ? "The farmer tested whether " + profile.Name + " wanted privacy."
                        : profile.Name + " hinted at wanting a quieter corner.";
                    nightState.Openness = nightState.Openness == OpennessLevel.Closed ? OpennessLevel.Open : OpennessLevel.Bold;
                    if (nightState.PlayerHeat >= PlayerHeat.Interested)
                    {
                        nightState.CurrentActivity = SocialActivity.Lingering;
                    }
                    break;

                default:
                    outcome.Beat = InteractionBeat.Chatted;
                    outcome.Summary = isPlayerLine
                        ? "The farmer spent more time talking with " + profile.Name + "."
                        : profile.Name + " kept the conversation going.";
                    if (isPlayerLine && nightState.Openness == OpennessLevel.Closed)
                    {
                        nightState.Openness = OpennessLevel.Open;
                    }
                    else if (nightState.Mood == MoodState.Guarded)
                    {
                        nightState.Mood = MoodState.Relaxed;
                    }
                    break;
            }

            nightState.LastInteractionBeat = outcome.Beat;
            if (nightState.CurrentActivity != SocialActivity.Dancing)
            {
                nightState.CurrentActivity = outcome.Beat == InteractionBeat.PrivateInvite
                    ? SocialActivity.Lingering
                    : SocialActivity.Chatting;
            }

            return outcome;
        }

        public InteractionOutcome ApplyGift(NpcProfile profile, NpcNightState nightState, SocialRelationshipContext relationshipContext, StardewValley.Object gift, int taste)
        {
            var isDrink = IsDrink(gift);
            var outcome = new InteractionOutcome
            {
                Action = isDrink ? SocialActionType.BuyDrink : SocialActionType.BuyMeal,
                Beat = isDrink ? InteractionBeat.GiftedDrink : InteractionBeat.SharedMeal,
                Summary = relationshipContext.CommittedPartnerPresent && isDrink && profile.LoyaltyToCommitments >= 4
                    ? "The farmer bought " + gift.DisplayName + " for " + profile.Name + ", and " + profile.Name + " accepted with a wary glance around the room."
                    : "The farmer bought " + gift.DisplayName + " for " + profile.Name + "."
            };

            nightState.LastInteractionBeat = outcome.Beat;
            nightState.CurrentActivity = isDrink ? SocialActivity.Drinking : SocialActivity.Eating;

            if (isDrink)
            {
                nightState.BuzzLevel = NextBuzzLevel(nightState.BuzzLevel);
                nightState.Openness = nightState.Openness == OpennessLevel.Closed ? OpennessLevel.Open : nightState.Openness;
            }

            if (taste <= 2)
            {
                nightState.PlayerHeat = RaiseHeat(nightState.PlayerHeat);
                nightState.Mood = MoodState.Cheerful;
            }
            else if (taste >= 4)
            {
                nightState.Mood = MoodState.Irritated;
            }
            else if (nightState.Mood == MoodState.Guarded)
            {
                nightState.Mood = MoodState.Relaxed;
            }

            return outcome;
        }

        private static bool ShouldRejectFlirt(NpcProfile profile, NpcNightState nightState, SocialRelationshipContext relationshipContext)
        {
            if (relationshipContext.CommittedPartnerPresent && profile.LoyaltyToCommitments >= 5 && nightState.BuzzLevel <= BuzzLevel.Buzzed)
            {
                return true;
            }

            return profile.Guardedness >= 5
                && nightState.BuzzLevel == BuzzLevel.Sober
                && nightState.PlayerHeat == PlayerHeat.Negative;
        }

        private static bool ShouldRejectInvitation(NpcProfile profile, NpcNightState nightState, SocialRelationshipContext relationshipContext)
        {
            if (relationshipContext.CommittedPartnerPresent && profile.LoyaltyToCommitments >= 5 && nightState.BuzzLevel <= BuzzLevel.Tipsy)
            {
                return true;
            }

            return nightState.PlayerHeat == PlayerHeat.Negative;
        }

        private static bool ShouldRejectPrivateConversation(NpcProfile profile, NpcNightState nightState, SocialRelationshipContext relationshipContext)
        {
            if (relationshipContext.CommittedPartnerPresent && profile.LoyaltyToCommitments >= 5 && nightState.BuzzLevel <= BuzzLevel.Tipsy)
            {
                return true;
            }

            return profile.Guardedness >= 5
                && nightState.BuzzLevel == BuzzLevel.Sober
                && nightState.PlayerHeat == PlayerHeat.Negative;
        }

        private static bool IsDrink(StardewValley.Object gift)
        {
            if (gift == null)
            {
                return false;
            }

            var name = (gift.Name ?? gift.DisplayName ?? string.Empty).ToLowerInvariant();
            string[] drinks =
            {
                "beer", "wine", "ale", "mead", "coffee", "espresso", "juice", "tea", "milk", "cider"
            };

            return drinks.Any(name.Contains);
        }

        private static BuzzLevel NextBuzzLevel(BuzzLevel current)
        {
            switch (current)
            {
                case BuzzLevel.Sober:
                    return BuzzLevel.Buzzed;
                case BuzzLevel.Buzzed:
                    return BuzzLevel.Tipsy;
                case BuzzLevel.Tipsy:
                    return BuzzLevel.Drunk;
                default:
                    return BuzzLevel.Drunk;
            }
        }

        private static PlayerHeat RaiseHeat(PlayerHeat current)
        {
            switch (current)
            {
                case PlayerHeat.Negative:
                    return PlayerHeat.Neutral;
                case PlayerHeat.Neutral:
                    return PlayerHeat.Interested;
                case PlayerHeat.Interested:
                    return PlayerHeat.Attracted;
                default:
                    return PlayerHeat.Attracted;
            }
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
