using ValleyTalk.Social.Models;

namespace ValleyTalk.Social.Services
{
    public interface ISocialActionService
    {
        InteractionOutcome Execute(SocialActionRequest request, NpcProfile profile, NpcNightState nightState);
    }
}
