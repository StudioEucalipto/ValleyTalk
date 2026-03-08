using System;
using System.Collections.Generic;
using System.Linq;
using ValleyTalk.Social.Models;

namespace ValleyTalk.Social.Services
{
    public class AttendanceService : IAttendanceService
    {
        private readonly INpcProfileService profileService;
        private readonly Random random;

        public AttendanceService(INpcProfileService profileService)
        {
            this.profileService = profileService;
            this.random = new Random();
        }

        public AttendanceRoll RollAttendance(int minimumAttendance, int maximumAttendance)
        {
            var candidates = this.profileService.GetEligibleProfiles().ToList();
            var selected = new List<string>();
            var target = this.random.Next(minimumAttendance, maximumAttendance + 1);

            while (selected.Count < target && candidates.Count > 0)
            {
                var totalWeight = candidates.Sum(candidate => Math.Max(1, candidate.AttendanceWeight));
                var roll = this.random.Next(0, totalWeight);
                var running = 0;

                foreach (var candidate in candidates.ToList())
                {
                    running += Math.Max(1, candidate.AttendanceWeight);
                    if (roll >= running)
                    {
                        continue;
                    }

                    selected.Add(candidate.Name);
                    candidates.Remove(candidate);
                    break;
                }
            }

            return new AttendanceRoll
            {
                MinimumAttendance = minimumAttendance,
                MaximumAttendance = maximumAttendance,
                SelectedNpcNames = selected
            };
        }
    }
}
