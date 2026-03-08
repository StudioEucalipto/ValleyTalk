using ValleyTalk.Social.Models;

namespace ValleyTalk.Social.Services
{
    public interface IConversationActionClassifier
    {
        SocialActionType Classify(string text, bool isPlayerLine);
    }
}
