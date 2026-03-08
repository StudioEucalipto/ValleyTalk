using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StardewModdingAPI;
using ValleyTalk.Social.Models;

namespace ValleyTalk.Social.Services
{
    public class SaloonPlacementService : IPlacementPlanService
    {
        private readonly List<SaloonSpot> spots;

        public SaloonPlacementService(IModHelper helper, IMonitor monitor, string relativeAssetPath = "assets/social/SaloonSpots.vanilla.json")
        {
            var assetPath = Path.Combine(helper.DirectoryPath, relativeAssetPath);
            this.spots = helper.Data.ReadJsonFile<List<SaloonSpot>>(assetPath) ?? new List<SaloonSpot>();

            if (this.spots.Count == 0)
            {
                monitor.Log("No saloon spot definitions were loaded from " + relativeAssetPath + ".", LogLevel.Warn);
            }
        }

        public PlacementPlan BuildPlan(AttendanceRoll attendance, IDictionary<string, NpcNightState> nightStates)
        {
            var availableSpots = this.spots
                .OrderBy(spot => spot.Priority)
                .ThenBy(spot => spot.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var placements = new List<NpcPlacement>();
            foreach (var npcName in attendance.SelectedNpcNames)
            {
                if (!nightStates.TryGetValue(npcName, out var nightState))
                {
                    continue;
                }

                var chosenSpot = this.SelectSpot(availableSpots, nightState.CurrentActivity)
                    ?? availableSpots.FirstOrDefault();
                if (chosenSpot == null)
                {
                    break;
                }

                placements.Add(new NpcPlacement
                {
                    NpcName = npcName,
                    SpotId = chosenSpot.Id,
                    Area = chosenSpot.Area,
                    GroupId = chosenSpot.GroupId,
                    Activity = nightState.CurrentActivity,
                    TileX = chosenSpot.TileX,
                    TileY = chosenSpot.TileY,
                    FacingDirection = chosenSpot.FacingDirection
                });

                availableSpots.Remove(chosenSpot);
            }

            return new PlacementPlan
            {
                Placements = placements
            };
        }

        private SaloonSpot SelectSpot(IEnumerable<SaloonSpot> availableSpots, SocialActivity activity)
        {
            var activityName = activity.ToString();
            return availableSpots.FirstOrDefault(spot => string.Equals(spot.Activity, activityName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
