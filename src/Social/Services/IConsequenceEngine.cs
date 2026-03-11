using ValleyTalk.Social.Models;

namespace ValleyTalk.Social.Services
{
    public interface IConsequenceEngine
    {
        InteractionOutcome ApplyConversation(NpcProfile profile, NpcNightState nightState, string dialogueText, bool isPlayerLine);
        InteractionOutcome ApplyGift(NpcProfile profile, NpcNightState nightState, StardewValley.Object gift, int taste);
    }
}
