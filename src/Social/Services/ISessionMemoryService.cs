using System.Collections.Generic;
using ValleyTalk.Social.Models;

namespace ValleyTalk.Social.Services
{
    public interface ISessionMemoryService
    {
        void Record(SocialSession session, InteractionRecord record);
        IReadOnlyList<InteractionRecord> GetRecentVisible(SocialSession session, string npcName, int maxCount);
    }
}
