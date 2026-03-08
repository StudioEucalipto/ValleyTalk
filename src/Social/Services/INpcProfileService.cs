using System.Collections.Generic;
using ValleyTalk.Social.Models;

namespace ValleyTalk.Social.Services
{
    public interface INpcProfileService
    {
        IReadOnlyList<NpcProfile> GetEligibleProfiles();
        bool TryGetProfile(string npcName, out NpcProfile profile);
    }
}
