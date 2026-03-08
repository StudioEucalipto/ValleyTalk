using System;
using StardewModdingAPI;
using StardewValley;
using ValleyTalk.Social.Models;
using ValleyTalk.Social.Services;

namespace ValleyTalk.Social
{
    public class SaturdaySocialManager
    {
        private readonly IMonitor monitor;
        private readonly ModConfig config;
        private readonly IAttendanceService attendanceService;
        private readonly INpcNightStateService npcNightStateService;
        private readonly IRoomMoodService roomMoodService;
        private readonly IValleyTalkContextBridge contextBridge;

        private SocialSession currentSession;
        private string lastStartedSessionKey;

        public SaturdaySocialManager(
            IMonitor monitor,
            ModConfig config,
            IAttendanceService attendanceService,
            INpcNightStateService npcNightStateService,
            IRoomMoodService roomMoodService,
            IValleyTalkContextBridge contextBridge)
        {
            this.monitor = monitor;
            this.config = config;
            this.attendanceService = attendanceService;
            this.npcNightStateService = npcNightStateService;
            this.roomMoodService = roomMoodService;
            this.contextBridge = contextBridge;
        }

        public bool HasActiveSession => this.currentSession != null;

        public void HandleWarp(GameLocation newLocation)
        {
            if (!this.config.EnableSaturdaySocial)
            {
                return;
            }

            if (!this.IsSaloon(newLocation))
            {
                if (this.currentSession != null)
                {
                    this.EndSession();
                }

                return;
            }

            if (!this.CanStartSession())
            {
                return;
            }

            this.StartSession();
        }

        public void ResetForNewDay()
        {
            this.currentSession = null;
            this.lastStartedSessionKey = null;
        }

        public void ResetForTitle()
        {
            this.currentSession = null;
            this.lastStartedSessionKey = null;
        }

        public bool TryGetPromptContext(NPC npc, out string contextText)
        {
            contextText = string.Empty;
            if (this.currentSession == null || npc == null)
            {
                return false;
            }

            if (!this.currentSession.Attendance.SelectedNpcNames.Contains(npc.Name))
            {
                return false;
            }

            contextText = this.contextBridge.BuildPromptContext(this.currentSession, npc.Name).ToCompactSummary();
            return !string.IsNullOrWhiteSpace(contextText);
        }

        private bool CanStartSession()
        {
            return this.currentSession == null
                && this.IsSaturday()
                && Game1.timeOfDay >= this.config.SaturdaySocialStartTime
                && this.GetSessionKey() != this.lastStartedSessionKey;
        }

        private bool IsSaloon(GameLocation location)
        {
            return location != null && string.Equals(location.NameOrUniqueName, "Saloon", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsSaturday()
        {
            return Game1.dayOfMonth % 7 == 6;
        }

        private string GetSessionKey()
        {
            return Game1.year + ":" + Game1.currentSeason + ":" + Game1.dayOfMonth;
        }

        private void StartSession()
        {
            var attendance = this.attendanceService.RollAttendance(this.config.SaturdaySocialMinAttendance, this.config.SaturdaySocialMaxAttendance);
            var nightStates = this.npcNightStateService.BuildNightStates(attendance.SelectedNpcNames);
            var roomMood = this.roomMoodService.BuildInitialRoomMood(attendance);

            this.currentSession = new SocialSession
            {
                SessionKey = this.GetSessionKey(),
                StartedAtTime = Game1.timeOfDay,
                Attendance = attendance,
                RoomMood = roomMood,
                NightStates = nightStates
            };

            this.lastStartedSessionKey = this.currentSession.SessionKey;
            this.monitor.Log(
                "Started Saturday social session with attendees: " + string.Join(", ", attendance.SelectedNpcNames),
                LogLevel.Info);
        }

        private void EndSession()
        {
            this.monitor.Log("Ended Saturday social session.", LogLevel.Trace);
            this.currentSession = null;
        }
    }
}
