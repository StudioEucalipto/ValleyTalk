using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly INpcProfileService profileService;
        private readonly INpcNightStateService npcNightStateService;
        private readonly IRoomMoodService roomMoodService;
        private readonly IValleyTalkContextBridge contextBridge;
        private readonly ISessionMemoryService sessionMemoryService;
        private readonly IConsequenceEngine consequenceEngine;

        private SocialSession currentSession;
        private string lastStartedSessionKey;

        public SaturdaySocialManager(
            IMonitor monitor,
            ModConfig config,
            IAttendanceService attendanceService,
            INpcProfileService profileService,
            INpcNightStateService npcNightStateService,
            IRoomMoodService roomMoodService,
            IValleyTalkContextBridge contextBridge,
            ISessionMemoryService sessionMemoryService,
            IConsequenceEngine consequenceEngine)
        {
            this.monitor = monitor;
            this.config = config;
            this.attendanceService = attendanceService;
            this.profileService = profileService;
            this.npcNightStateService = npcNightStateService;
            this.roomMoodService = roomMoodService;
            this.contextBridge = contextBridge;
            this.sessionMemoryService = sessionMemoryService;
            this.consequenceEngine = consequenceEngine;
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

        public void NoteConversation(NPC npc, string dialogueText, bool isPlayerLine)
        {
            if (!this.TryGetActiveParticipant(npc, out var profile, out var nightState))
            {
                return;
            }

            var outcome = this.consequenceEngine.ApplyConversation(profile, nightState, dialogueText, isPlayerLine);

            this.sessionMemoryService.Record(this.currentSession, new InteractionRecord
            {
                TimeOfDay = Game1.timeOfDay,
                NpcName = npc.Name,
                Action = outcome.Action.ToString(),
                ResultBeat = outcome.Beat,
                Summary = outcome.Summary,
                VisibleToRoom = outcome.VisibleToRoom
            });

            this.currentSession.RoomMood = this.roomMoodService.Recalculate(this.currentSession);
        }

        public void NoteGift(NPC npc, StardewValley.Object gift, int taste)
        {
            if (!this.TryGetActiveParticipant(npc, out var profile, out var nightState))
            {
                return;
            }

            var outcome = this.consequenceEngine.ApplyGift(profile, nightState, gift, taste);

            this.sessionMemoryService.Record(this.currentSession, new InteractionRecord
            {
                TimeOfDay = Game1.timeOfDay,
                NpcName = npc.Name,
                Action = outcome.Action.ToString(),
                ResultBeat = outcome.Beat,
                Summary = outcome.Summary,
                VisibleToRoom = outcome.VisibleToRoom
            });

            this.currentSession.RoomMood = this.roomMoodService.Recalculate(this.currentSession);
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

        private bool TryGetActiveParticipant(NPC npc, out NpcProfile profile, out NpcNightState nightState)
        {
            profile = null;
            nightState = null;
            if (this.currentSession == null || npc == null)
            {
                return false;
            }

            return this.profileService.TryGetProfile(npc.Name, out profile)
                && this.currentSession.NightStates.TryGetValue(npc.Name, out nightState);
        }
    }
}
