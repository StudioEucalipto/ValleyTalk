using System;
using ValleyTalk.Social.Models;

namespace ValleyTalk.Social.Services
{
    public class ConversationCommerceParser : IConversationCommerceParser
    {
        public bool TryParse(string dialogueText, out ConversationOrderIntent intent)
        {
            intent = null;
            if (string.IsNullOrWhiteSpace(dialogueText))
            {
                return false;
            }

            var normalized = dialogueText.ToLowerInvariant();
            var wantsToBuy = normalized.Contains("buy")
                || normalized.Contains("get you")
                || normalized.Contains("let me get")
                || normalized.Contains("order you")
                || normalized.Contains("on me");

            if (!wantsToBuy)
            {
                return false;
            }

            if (normalized.Contains("round for the room")
                || normalized.Contains("round for everyone")
                || normalized.Contains("drinks on me")
                || normalized.Contains("round on me"))
            {
                intent = new ConversationOrderIntent
                {
                    IsDrink = true,
                    ForRoom = true,
                    ItemName = "Beer"
                };
                return true;
            }

            var drink = InferDrink(normalized);
            if (!string.IsNullOrWhiteSpace(drink))
            {
                intent = new ConversationOrderIntent
                {
                    IsDrink = true,
                    ItemName = drink
                };
                return true;
            }

            var meal = InferMeal(normalized);
            if (!string.IsNullOrWhiteSpace(meal))
            {
                intent = new ConversationOrderIntent
                {
                    IsMeal = true,
                    ItemName = meal
                };
                return true;
            }

            return false;
        }

        private static string InferDrink(string normalized)
        {
            if (normalized.Contains("beer"))
            {
                return "Beer";
            }

            if (normalized.Contains("ale"))
            {
                return "Pale Ale";
            }

            if (normalized.Contains("wine"))
            {
                return "Wine";
            }

            if (normalized.Contains("coffee"))
            {
                return "Coffee";
            }

            if (normalized.Contains("drink"))
            {
                return "Beer";
            }

            return string.Empty;
        }

        private static string InferMeal(string normalized)
        {
            if (normalized.Contains("salad"))
            {
                return "Salad";
            }

            if (normalized.Contains("pizza"))
            {
                return "Pizza";
            }

            if (normalized.Contains("burger"))
            {
                return "Survival Burger";
            }

            if (normalized.Contains("meal") || normalized.Contains("food") || normalized.Contains("eat") || normalized.Contains("dinner"))
            {
                return "Salad";
            }

            return string.Empty;
        }
    }
}
