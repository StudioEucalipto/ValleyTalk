using ValleyTalk.Social.Models;

namespace ValleyTalk.Social.Services
{
    public interface IValleyTalkContextBridge
    {
        ValleyTalkPromptContext BuildPromptContext(SocialSession session, string npcName);
    }
}
