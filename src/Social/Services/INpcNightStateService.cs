using System.Collections.Generic;
using ValleyTalk.Social.Models;

namespace ValleyTalk.Social.Services
{
    public interface INpcNightStateService
    {
        Dictionary<string, NpcNightState> BuildNightStates(IEnumerable<string> npcNames);
    }
}
