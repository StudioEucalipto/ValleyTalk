using ValleyTalk.Social.Models;

namespace ValleyTalk.Social.Services
{
    public class CommerceService : ICommerceService
    {
        public InteractionOutcome ApplyDrinkOrder(DrinkOrder order, NpcProfile profile, NpcNightState nightState)
        {
            var itemName = string.IsNullOrWhiteSpace(order?.ItemName) ? "a drink" : order.ItemName;
            var outcome = new InteractionOutcome
            {
                Action = SocialActionType.BuyDrink,
                Beat = InteractionBeat.GiftedDrink,
                Summary = order?.ForRoom == true
                    ? "The farmer bought a round for the room."
                    : "The farmer bought " + itemName + " for " + profile.Name + "."
            };

            nightState.LastInteractionBeat = outcome.Beat;
            nightState.CurrentActivity = SocialActivity.Drinking;
            nightState.BuzzLevel = AdvanceBuzz(nightState.BuzzLevel, profile.AlcoholTolerance, order?.ForRoom == true);

            if (profile.Flirtiness >= 4 && nightState.PlayerHeat < PlayerHeat.Attracted)
            {
                nightState.PlayerHeat = RaiseHeat(nightState.PlayerHeat);
            }

            if (nightState.Openness == OpennessLevel.Closed)
            {
                nightState.Openness = OpennessLevel.Open;
            }
            else if (nightState.BuzzLevel >= BuzzLevel.Tipsy && profile.Guardedness <= 2)
            {
                nightState.Openness = OpennessLevel.Bold;
            }

            if (nightState.Mood == MoodState.Guarded || nightState.Mood == MoodState.Lonely)
            {
                nightState.Mood = MoodState.Relaxed;
            }
            else if (nightState.BuzzLevel == BuzzLevel.Drunk && profile.Temper >= 4)
            {
                nightState.Mood = MoodState.Rowdy;
            }
            else
            {
                nightState.Mood = MoodState.Cheerful;
            }

            return outcome;
        }

        public InteractionOutcome ApplyMealOrder(MealOrder order, NpcProfile profile, NpcNightState nightState)
        {
            var itemName = string.IsNullOrWhiteSpace(order?.ItemName) ? "a meal" : order.ItemName;
            var outcome = new InteractionOutcome
            {
                Action = SocialActionType.BuyMeal,
                Beat = InteractionBeat.SharedMeal,
                Summary = "The farmer bought " + itemName + " for " + profile.Name + ".",
                VisibleToRoom = false
            };

            nightState.LastInteractionBeat = outcome.Beat;
            nightState.CurrentActivity = SocialActivity.Eating;

            if (nightState.Mood == MoodState.Irritated || nightState.Mood == MoodState.Guarded)
            {
                nightState.Mood = MoodState.Relaxed;
            }
            else
            {
                nightState.Mood = MoodState.Cheerful;
            }

            if (profile.Guardedness <= 3 && nightState.Openness == OpennessLevel.Closed)
            {
                nightState.Openness = OpennessLevel.Open;
            }

            if (profile.Sociability >= 4 || profile.Flirtiness >= 3)
            {
                nightState.PlayerHeat = RaiseHeat(nightState.PlayerHeat);
            }

            return outcome;
        }

        private static BuzzLevel AdvanceBuzz(BuzzLevel current, int alcoholTolerance, bool softPour)
        {
            switch (current)
            {
                case BuzzLevel.Sober:
                    return BuzzLevel.Buzzed;
                case BuzzLevel.Buzzed:
                    return alcoholTolerance >= 4 && softPour ? BuzzLevel.Buzzed : BuzzLevel.Tipsy;
                case BuzzLevel.Tipsy:
                    return alcoholTolerance >= 5 && softPour ? BuzzLevel.Tipsy : BuzzLevel.Drunk;
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
    }
}
