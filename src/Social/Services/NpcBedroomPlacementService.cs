using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StardewModdingAPI;
using StardewValley;
using ValleyTalk.Social.Models;

namespace ValleyTalk.Social.Services
{
    public class NpcBedroomPlacementService : INpcBedroomPlacementService
    {
        private readonly Dictionary<string, List<NpcBedroomPlacement>> placements;

        public NpcBedroomPlacementService(IModHelper helper, IMonitor monitor, string relativeAssetPath = "assets/social/NpcBedroomPlacements.vanilla.json")
        {
            var assetPath = Path.Combine(helper.DirectoryPath, relativeAssetPath);
            var loadedPlacements = helper.Data.ReadJsonFile<List<NpcBedroomPlacement>>(assetPath) ?? new List<NpcBedroomPlacement>();

            if (loadedPlacements.Count == 0)
            {
                monitor.Log("No NPC bedroom placements were loaded from " + relativeAssetPath + ".", LogLevel.Warn);
            }

            this.placements = loadedPlacements
                .Where(placement => !string.IsNullOrWhiteSpace(placement.NpcName))
                .GroupBy(placement => placement.NpcName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);
        }

        public bool TryGetPlacement(string npcName, out NpcBedroomPlacement placement)
        {
            placement = null!;
            if (!this.placements.TryGetValue(npcName, out var candidates) || candidates.Count == 0)
            {
                return false;
            }

            placement = candidates.FirstOrDefault(candidate =>
                !string.IsNullOrWhiteSpace(candidate.LocationName)
                && Game1.getLocationFromName(candidate.LocationName) != null)
                ?? candidates[0];

            return placement != null;
        }
    }
}
