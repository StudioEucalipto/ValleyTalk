using System;
using System.Collections.Generic;
using System.Linq;
using ValleyTalk.Social.Models;

namespace ValleyTalk.Social.Services
{
    public class SessionMemoryService : ISessionMemoryService
    {
        public void Record(SocialSession session, InteractionRecord record)
        {
            session.Memory.Add(record);
        }

        public IReadOnlyList<InteractionRecord> GetRecentVisible(SocialSession session, string npcName, int maxCount)
        {
            return session.Memory
                .Where(record => record.VisibleToRoom || string.Equals(record.NpcName, npcName, StringComparison.OrdinalIgnoreCase))
                .TakeLast(maxCount)
                .ToList();
        }
    }
}
