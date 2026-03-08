using System;
using System.Collections.Generic;
using System.Linq;
using ValleyTalk.Social.Models;

namespace ValleyTalk.Social.Services
{
    public class RoomMoodService : IRoomMoodService
    {
        public RoomMoodState BuildInitialRoomMood(AttendanceRoll attendance, PlacementPlan placementPlan)
        {
            var vibe = attendance.SelectedNpcNames.Count >= 6 ? RoomVibe.Lively : RoomVibe.Cozy;
            return new RoomMoodState
            {
                AttendanceSize = attendance.SelectedNpcNames.Count,
                Vibe = vibe,
                VisibleGroups = this.BuildVisibleGroups(placementPlan),
                Summary = this.BuildSummary(vibe, attendance.SelectedNpcNames.Count)
            };
        }

        public RoomMoodState Recalculate(SocialSession session)
        {
            var flirtCount = 0;
            var argumentCount = 0;

            foreach (var record in session.Memory)
            {
                if (record.ResultBeat == InteractionBeat.Flirted)
                {
                    flirtCount++;
                }
                else if (record.ResultBeat == InteractionBeat.Argued)
                {
                    argumentCount++;
                }
            }

            var vibe = RoomVibe.Cozy;
            if (argumentCount > 0)
            {
                vibe = RoomVibe.Tense;
            }
            else if (flirtCount >= 2)
            {
                vibe = RoomVibe.Romantic;
            }
            else if (session.Attendance.SelectedNpcNames.Count >= 6)
            {
                vibe = RoomVibe.Lively;
            }

            return new RoomMoodState
            {
                AttendanceSize = session.Attendance.SelectedNpcNames.Count,
                Vibe = vibe,
                VisibleGroups = this.BuildVisibleGroups(session.PlacementPlan),
                Summary = this.BuildSummary(vibe, session.Attendance.SelectedNpcNames.Count)
            };
        }

        private List<string> BuildVisibleGroups(PlacementPlan placementPlan)
        {
            var groups = new List<string>();
            var groupedPlacements = placementPlan.Placements
                .GroupBy(placement => placement.GroupId)
                .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase);

            foreach (var group in groupedPlacements)
            {
                var names = group.Select(placement => placement.NpcName).ToList();
                if (names.Count == 0)
                {
                    continue;
                }

                if (names.Count == 1)
                {
                    groups.Add(names[0] + " in the " + group.First().Area.ToLowerInvariant());
                }
                else
                {
                    groups.Add(string.Join(", ", names) + " together in the " + group.First().Area.ToLowerInvariant());
                }
            }

            return groups;
        }

        private string BuildSummary(RoomVibe vibe, int attendanceSize)
        {
            switch (vibe)
            {
                case RoomVibe.Tense:
                    return "The saloon feels tense, with " + attendanceSize + " villagers watching each other carefully.";
                case RoomVibe.Romantic:
                    return "The saloon feels soft and charged, with " + attendanceSize + " villagers lingering close.";
                case RoomVibe.Lively:
                    return "The saloon feels lively, with " + attendanceSize + " villagers drifting between tables.";
                default:
                    return "The saloon feels cozy and grounded, with " + attendanceSize + " villagers settling into the night.";
            }
        }
    }
}
