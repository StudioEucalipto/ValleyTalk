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
        private readonly INpcNightStateService npcNightStateService;
        private readonly IRoomMoodService roomMoodService;
        private readonly IValleyTalkContextBridge contextBridge;
        private readonly ISessionMemoryService sessionMemoryService;

        private SocialSession currentSession;
        private string lastStartedSessionKey;

        public SaturdaySocialManager(
            IMonitor monitor,
            ModConfig config,
            IAttendanceService attendanceService,
            INpcNightStateService npcNightStateService,
            IRoomMoodService roomMoodService,
            IValleyTalkContextBridge contextBridge,
            ISessionMemoryService sessionMemoryService)
        {
            this.monitor = monitor;
            this.config = config;
            this.attendanceService = attendanceService;
            this.npcNightStateService = npcNightStateService;
            this.roomMoodService = roomMoodService;
            this.contextBridge = contextBridge;
            this.sessionMemoryService = sessionMemoryService;
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
            if (!this.TryGetActiveNightState(npc, out var nightState))
            {
                return;
            }

            var beat = this.ClassifyConversationBeat(dialogueText, isPlayerLine);
            var summary = this.BuildConversationSummary(npc, dialogueText, isPlayerLine, beat);

            nightState.LastInteractionBeat = beat;
            nightState.CurrentActivity = SocialActivity.Chatting;
            this.ApplyConversationStateShift(nightState, beat, isPlayerLine);

            this.sessionMemoryService.Record(this.currentSession, new InteractionRecord
            {
                TimeOfDay = Game1.timeOfDay,
                NpcName = npc.Name,
                Action = isPlayerLine ? "PlayerConversation" : "NpcConversation",
                ResultBeat = beat,
                Summary = summary,
                VisibleToRoom = true
            });

            this.currentSession.RoomMood = this.roomMoodService.Recalculate(this.currentSession);
        }

        public void NoteGift(NPC npc, StardewValley.Object gift, int taste)
        {
            if (!this.TryGetActiveNightState(npc, out var nightState))
            {
                return;
            }

            var isDrink = this.IsDrink(gift);
            var beat = isDrink ? InteractionBeat.GiftedDrink : InteractionBeat.SharedMeal;

            nightState.LastInteractionBeat = beat;
            nightState.CurrentActivity = isDrink ? SocialActivity.Drinking : SocialActivity.Eating;

            if (isDrink)
            {
                nightState.BuzzLevel = this.NextBuzzLevel(nightState.BuzzLevel);
                nightState.Openness = nightState.Openness == OpennessLevel.Closed ? OpennessLevel.Open : nightState.Openness;
            }

            if (taste <= 2)
            {
                nightState.PlayerHeat = this.RaiseHeat(nightState.PlayerHeat);
                nightState.Mood = MoodState.Cheerful;
            }
            else if (taste >= 4)
            {
                nightState.Mood = MoodState.Irritated;
            }
            else if (nightState.Mood == MoodState.Guarded)
            {
                nightState.Mood = MoodState.Relaxed;
            }

            this.sessionMemoryService.Record(this.currentSession, new InteractionRecord
            {
                TimeOfDay = Game1.timeOfDay,
                NpcName = npc.Name,
                Action = isDrink ? "GiftDrink" : "GiftMeal",
                ResultBeat = beat,
                Summary = "The farmer bought " + gift.DisplayName + " for " + npc.displayName + ".",
                VisibleToRoom = true
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

        private bool TryGetActiveNightState(NPC npc, out NpcNightState nightState)
        {
            nightState = null;
            if (this.currentSession == null || npc == null)
            {
                return false;
            }

            return this.currentSession.NightStates.TryGetValue(npc.Name, out nightState);
        }

        private InteractionBeat ClassifyConversationBeat(string text, bool isPlayerLine)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return InteractionBeat.None;
            }

            var normalized = text.ToLowerInvariant();
            if (normalized.Contains("dance"))
            {
                return InteractionBeat.Danced;
            }

            if (normalized.Contains("sorry") || normalized.Contains("apolog"))
            {
                return InteractionBeat.None;
            }

            if (normalized.Contains("idiot") || normalized.Contains("stupid") || normalized.Contains("shut up") || normalized.Contains("hate you"))
            {
                return InteractionBeat.Argued;
            }

            if (normalized.Contains("beautiful") || normalized.Contains("handsome") || normalized.Contains("cute") || normalized.Contains("kiss") || normalized.Contains("flirt"))
            {
                return InteractionBeat.Flirted;
            }

            return InteractionBeat.Chatted;
        }

        private string BuildConversationSummary(NPC npc, string dialogueText, bool isPlayerLine, InteractionBeat beat)
        {
            if (beat == InteractionBeat.Flirted)
            {
                return isPlayerLine
                    ? "The farmer flirted with " + npc.displayName + "."
                    : npc.displayName + " answered with a flirtatious tone.";
            }

            if (beat == InteractionBeat.Argued)
            {
                return isPlayerLine
                    ? "The farmer pushed " + npc.displayName + " into a sharper exchange."
                    : npc.displayName + " sounded tense and argumentative.";
            }

            if (beat == InteractionBeat.Danced)
            {
                return isPlayerLine
                    ? "The farmer steered the conversation toward dancing."
                    : npc.displayName + " brought dancing into the conversation.";
            }

            return isPlayerLine
                ? "The farmer spent more time talking with " + npc.displayName + "."
                : npc.displayName + " kept the conversation going.";
        }

        private void ApplyConversationStateShift(NpcNightState nightState, InteractionBeat beat, bool isPlayerLine)
        {
            switch (beat)
            {
                case InteractionBeat.Flirted:
                    nightState.PlayerHeat = this.RaiseHeat(nightState.PlayerHeat);
                    nightState.Openness = OpennessLevel.Bold;
                    nightState.Mood = nightState.Mood == MoodState.Irritated ? MoodState.Guarded : MoodState.Cheerful;
                    break;
                case InteractionBeat.Argued:
                    nightState.Mood = MoodState.Irritated;
                    nightState.PlayerHeat = this.LowerHeat(nightState.PlayerHeat);
                    break;
                case InteractionBeat.Danced:
                    nightState.CurrentActivity = SocialActivity.Dancing;
                    nightState.PlayerHeat = this.RaiseHeat(nightState.PlayerHeat);
                    break;
                case InteractionBeat.Chatted:
                    if (isPlayerLine && nightState.Openness == OpennessLevel.Closed)
                    {
                        nightState.Openness = OpennessLevel.Open;
                    }
                    else if (nightState.Mood == MoodState.Guarded)
                    {
                        nightState.Mood = MoodState.Relaxed;
                    }
                    break;
            }
        }

        private bool IsDrink(StardewValley.Object gift)
        {
            if (gift == null)
            {
                return false;
            }

            var name = (gift.Name ?? gift.DisplayName ?? string.Empty).ToLowerInvariant();
            string[] drinks =
            {
                "beer", "wine", "ale", "mead", "coffee", "espresso", "juice", "tea", "milk", "cider"
            };

            return drinks.Any(name.Contains);
        }

        private BuzzLevel NextBuzzLevel(BuzzLevel current)
        {
            switch (current)
            {
                case BuzzLevel.Sober:
                    return BuzzLevel.Buzzed;
                case BuzzLevel.Buzzed:
                    return BuzzLevel.Tipsy;
                case BuzzLevel.Tipsy:
                    return BuzzLevel.Drunk;
                default:
                    return BuzzLevel.Drunk;
            }
        }

        private PlayerHeat RaiseHeat(PlayerHeat current)
        {
            switch (current)
            {
                case PlayerHeat.Negative:
                    return PlayerHeat.Neutral;
                case PlayerHeat.Neutral:
                    return PlayerHeat.Interested;
                case PlayerHeat.Interested:
                    return PlayerHeat.Attracted;
                default:
                    return PlayerHeat.Attracted;
            }
        }

        private PlayerHeat LowerHeat(PlayerHeat current)
        {
            switch (current)
            {
                case PlayerHeat.Attracted:
                    return PlayerHeat.Interested;
                case PlayerHeat.Interested:
                    return PlayerHeat.Neutral;
                case PlayerHeat.Neutral:
                    return PlayerHeat.Negative;
                default:
                    return PlayerHeat.Negative;
            }
        }
    }
}
