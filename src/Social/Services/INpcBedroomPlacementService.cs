using ValleyTalk.Social.Models;

namespace ValleyTalk.Social.Services
{
    public interface INpcBedroomPlacementService
    {
        bool TryGetPlacement(string npcName, out NpcBedroomPlacement placement);
    }
}
