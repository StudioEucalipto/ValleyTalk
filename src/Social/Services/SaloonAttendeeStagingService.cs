using System;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using ValleyTalk.Social.Models;

namespace ValleyTalk.Social.Services
{
    public class SaloonAttendeeStagingService : IAttendeeStagingService
    {
        private readonly IMonitor monitor;
        private readonly MethodInfo warpCharacterMethod;

        public SaloonAttendeeStagingService(IMonitor monitor)
        {
            this.monitor = monitor;
            this.warpCharacterMethod = typeof(Game1)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method =>
                {
                    if (!string.Equals(method.Name, "warpCharacter", StringComparison.Ordinal))
                    {
                        return false;
                    }

                    var parameters = method.GetParameters();
                    return parameters.Length == 3
                        && parameters[0].ParameterType == typeof(NPC)
                        && (parameters[1].ParameterType == typeof(string) || parameters[1].ParameterType == typeof(GameLocation))
                        && (parameters[2].ParameterType == typeof(Point) || parameters[2].ParameterType == typeof(Vector2));
                });

            if (this.warpCharacterMethod == null)
            {
                this.monitor.Log("Saturday social attendee staging is disabled because no compatible Game1.warpCharacter overload was found.", LogLevel.Warn);
            }
        }

        public void StageAttendees(SocialSession session)
        {
            if (session?.PlacementPlan == null)
            {
                return;
            }

            foreach (var placement in session.PlacementPlan.Placements)
            {
                var npc = Game1.getCharacterFromName(placement.NpcName);
                if (npc == null)
                {
                    this.monitor.Log("Unable to stage Saturday social attendee '" + placement.NpcName + "': NPC not found.", LogLevel.Trace);
                    continue;
                }

                if (!session.StageSnapshots.ContainsKey(npc.Name))
                {
                    session.StageSnapshots[npc.Name] = this.CreateSnapshot(npc);
                }

                if (!this.TryWarpNpc(npc, session.LocationName, placement.TileX, placement.TileY))
                {
                    this.monitor.Log("Unable to stage Saturday social attendee '" + npc.Name + "' in the saloon.", LogLevel.Warn);
                    continue;
                }

                npc.faceDirection(placement.FacingDirection);
            }
        }

        public void RestoreAttendees(SocialSession session)
        {
            if (session?.StageSnapshots == null || session.StageSnapshots.Count == 0)
            {
                return;
            }

            foreach (var snapshot in session.StageSnapshots.Values)
            {
                var npc = Game1.getCharacterFromName(snapshot.NpcName);
                if (npc == null)
                {
                    continue;
                }

                if (!this.TryWarpNpc(npc, snapshot.LocationName, snapshot.TileX, snapshot.TileY))
                {
                    this.monitor.Log("Unable to restore Saturday social attendee '" + snapshot.NpcName + "'.", LogLevel.Warn);
                    continue;
                }

                npc.faceDirection(snapshot.FacingDirection);
            }

            session.StageSnapshots.Clear();
        }

        private NpcStageSnapshot CreateSnapshot(NPC npc)
        {
            var tile = npc.TilePoint;
            return new NpcStageSnapshot
            {
                NpcName = npc.Name,
                LocationName = npc.currentLocation?.NameOrUniqueName ?? string.Empty,
                TileX = tile.X,
                TileY = tile.Y,
                FacingDirection = this.GetFacingDirection(npc)
            };
        }

        private int GetFacingDirection(NPC npc)
        {
            var directionProperty = npc.GetType().GetProperty("FacingDirection", BindingFlags.Public | BindingFlags.Instance);
            if (directionProperty?.PropertyType == typeof(int))
            {
                return (int)directionProperty.GetValue(npc);
            }

            var directionField = npc.GetType().GetField("facingDirection", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (directionField?.FieldType == typeof(int))
            {
                return (int)directionField.GetValue(npc);
            }

            return 2;
        }

        private bool TryWarpNpc(NPC npc, string locationName, int tileX, int tileY)
        {
            if (npc == null || string.IsNullOrWhiteSpace(locationName) || this.warpCharacterMethod == null)
            {
                return false;
            }

            var targetLocation = Game1.getLocationFromName(locationName);
            if (targetLocation == null)
            {
                return false;
            }

            var parameters = this.warpCharacterMethod.GetParameters();
            var destination = parameters[1].ParameterType == typeof(GameLocation)
                ? (object)targetLocation
                : locationName;
            var tile = parameters[2].ParameterType == typeof(Point)
                ? (object)new Point(tileX, tileY)
                : new Vector2(tileX, tileY);

            try
            {
                this.warpCharacterMethod.Invoke(null, new[] { (object)npc, destination, tile });
                return true;
            }
            catch (Exception ex)
            {
                this.monitor.Log("Saturday social attendee warp failed for '" + npc.Name + "': " + ex.Message, LogLevel.Warn);
                return false;
            }
        }
    }
}
