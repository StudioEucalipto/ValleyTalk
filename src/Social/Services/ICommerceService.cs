using ValleyTalk.Social.Models;

namespace ValleyTalk.Social.Services
{
    public interface ICommerceService
    {
        InteractionOutcome ApplyDrinkOrder(DrinkOrder order, NpcProfile profile, NpcNightState nightState);
        InteractionOutcome ApplyMealOrder(MealOrder order, NpcProfile profile, NpcNightState nightState);
    }
}
