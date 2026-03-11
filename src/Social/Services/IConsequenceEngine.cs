using ValleyTalk.Social.Models;

namespace ValleyTalk.Social.Services
{
    public interface IConsequenceEngine
    {
        InteractionOutcome ApplyConversation(NpcProfile profile, NpcNightState nightState, SocialRelationshipContext relationshipContext, string dialogueText, bool isPlayerLine);
        InteractionOutcome ApplyGift(NpcProfile profile, NpcNightState nightState, SocialRelationshipContext relationshipContext, StardewValley.Object gift, int taste);
    }
}
