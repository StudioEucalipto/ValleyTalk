using ValleyTalk.Social.Models;

namespace ValleyTalk.Social.Services
{
    public interface IConversationCommerceParser
    {
        bool TryParse(string dialogueText, out ConversationOrderIntent intent);
    }
}
