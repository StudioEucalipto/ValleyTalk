using ValleyTalk.Social.Models;

namespace ValleyTalk.Social.Services
{
    public class SocialActionService : ISocialActionService
    {
        private readonly IConsequenceEngine consequenceEngine;
        private readonly ICommerceService commerceService;

        public SocialActionService(IConsequenceEngine consequenceEngine, ICommerceService commerceService)
        {
            this.consequenceEngine = consequenceEngine;
            this.commerceService = commerceService;
        }

        public InteractionOutcome Execute(SocialActionRequest request, NpcProfile profile, NpcNightState nightState)
        {
            switch (request.Action)
            {
                case SocialActionType.BuyDrink:
                    return this.commerceService.ApplyDrinkOrder(
                        new DrinkOrder
                        {
                            BuyerName = request.TargetsRoom ? "Farmer" : string.Empty,
                            RecipientName = profile.Name,
                            ItemName = request.ItemName,
                            Price = request.Price,
                            ForRoom = request.TargetsRoom
                        },
                        profile,
                        nightState);

                case SocialActionType.BuyMeal:
                    return this.commerceService.ApplyMealOrder(
                        new MealOrder
                        {
                            BuyerName = "Farmer",
                            RecipientName = profile.Name,
                            ItemName = request.ItemName,
                            Price = request.Price
                        },
                        profile,
                        nightState);

                default:
                    return this.consequenceEngine.ApplyExplicitAction(profile, nightState, request.Action);
            }
        }
    }
}
