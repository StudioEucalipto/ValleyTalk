using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StardewModdingAPI;
using ValleyTalk.Social.Models;

namespace ValleyTalk.Social.Services
{
    public class NpcProfileService : INpcProfileService
    {
        private readonly Dictionary<string, NpcProfile> profiles;

        public NpcProfileService(IModHelper helper, IMonitor monitor, string relativeAssetPath = "assets/social/NpcProfiles.vanilla.json")
        {
            var assetPath = Path.Combine(helper.DirectoryPath, relativeAssetPath);
            var loadedProfiles = helper.Data.ReadJsonFile<List<NpcProfile>>(assetPath) ?? new List<NpcProfile>();

            if (loadedProfiles.Count == 0)
            {
                monitor.Log("No social NPC profiles were loaded from " + relativeAssetPath + ".", LogLevel.Warn);
            }

            this.profiles = loadedProfiles
                .GroupBy(profile => profile.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        }

        public IReadOnlyList<NpcProfile> GetEligibleProfiles()
        {
            return this.profiles.Values
                .Where(profile => profile.IsAdult && profile.EligibleForSaturdaySocial)
                .OrderBy(profile => profile.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public bool TryGetProfile(string npcName, out NpcProfile profile)
        {
            return this.profiles.TryGetValue(npcName, out profile);
        }
    }
}
