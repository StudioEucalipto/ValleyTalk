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
        private readonly IPlacementPlanService placementPlanService;
        private readonly IAttendeeStagingService attendeeStagingService;
        private readonly ISocialActionService socialActionService;
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
            IPlacementPlanService placementPlanService,
            IAttendeeStagingService attendeeStagingService,
            ISocialActionService socialActionService,
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
            this.placementPlanService = placementPlanService;
            this.attendeeStagingService = attendeeStagingService;
            this.socialActionService = socialActionService;
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
            this.EndSession();
            this.lastStartedSessionKey = null;
        }

        public void ResetForTitle()
        {
            this.EndSession();
            this.lastStartedSessionKey = null;
        }

        public void HandleTimeChanged()
        {
            if (this.currentSession == null || !this.IsSaloon(Game1.player?.currentLocation))
            {
                return;
            }

            this.attendeeStagingService.StageAttendees(this.currentSession);
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

        public bool CanInteractWithNpc(NPC npc)
        {
            return npc != null
                && this.currentSession != null
                && this.IsSaloon(Game1.player?.currentLocation)
                && this.currentSession.Attendance.SelectedNpcNames.Contains(npc.Name);
        }

        public IReadOnlyList<SocialActionRequest> GetAvailableActionsForNpc(string npcName)
        {
            if (!this.TryGetActiveParticipant(npcName, out var profile, out var nightState))
            {
                return Array.Empty<SocialActionRequest>();
            }

            var actions = new List<SocialActionRequest>
            {
                new SocialActionRequest
                {
                    Action = SocialActionType.BuyDrink,
                    TargetNpcName = npcName,
                    Label = "Buy Drink",
                    Description = "Order " + profile.Name + " a beer.",
                    ItemName = "Beer",
                    Price = 400
                },
                new SocialActionRequest
                {
                    Action = SocialActionType.BuyMeal,
                    TargetNpcName = npcName,
                    Label = "Buy Meal",
                    Description = "Order " + profile.Name + " something to eat.",
                    ItemName = "Salad",
                    Price = 220
                },
                new SocialActionRequest
                {
                    Action = SocialActionType.InviteDance,
                    TargetNpcName = npcName,
                    Label = "Invite Dance",
                    Description = "Try to pull " + profile.Name + " onto the floor."
                },
                new SocialActionRequest
                {
                    Action = SocialActionType.SuggestPrivateConversation,
                    TargetNpcName = npcName,
                    Label = "Private Talk",
                    Description = "Suggest stepping somewhere quieter."
                },
                new SocialActionRequest
                {
                    Action = SocialActionType.Apologize,
                    TargetNpcName = npcName,
                    Label = "Apologize",
                    Description = "Try to smooth the mood over."
                }
            };

            if (nightState.Mood != MoodState.Irritated || profile.Temper >= 3)
            {
                actions.Add(new SocialActionRequest
                {
                    Action = SocialActionType.Provoke,
                    TargetNpcName = npcName,
                    Label = "Provoke",
                    Description = "Push the conversation into risky territory."
                });
            }

            actions.Add(new SocialActionRequest
            {
                Action = SocialActionType.BuyDrink,
                TargetNpcName = npcName,
                Label = "Buy Round",
                Description = "Cover a round for everybody in the room.",
                ItemName = "Beer",
                Price = 1600,
                TargetsRoom = true
            });

            return actions;
        }

        public void NoteConversation(NPC npc, string dialogueText, bool isPlayerLine)
        {
            if (!this.TryGetActiveParticipant(npc, out var profile, out var nightState))
            {
                return;
            }

            var outcome = this.consequenceEngine.ApplyConversation(profile, nightState, dialogueText, isPlayerLine);
            this.RecordOutcome(npc.Name, outcome);
        }

        public bool TryBuyDrinkForNpc(string npcName, string itemName, int price = 0)
        {
            return this.TryExecuteAction(new SocialActionRequest
            {
                Action = SocialActionType.BuyDrink,
                TargetNpcName = npcName,
                ItemName = itemName,
                Price = price
            }, out _);
        }

        public bool TryBuyMealForNpc(string npcName, string itemName, int price = 0)
        {
            return this.TryExecuteAction(new SocialActionRequest
            {
                Action = SocialActionType.BuyMeal,
                TargetNpcName = npcName,
                ItemName = itemName,
                Price = price
            }, out _);
        }

        public bool TryBuyDrinkForRoom(string itemName, int price = 0)
        {
            return this.TryExecuteAction(new SocialActionRequest
            {
                Action = SocialActionType.BuyDrink,
                TargetNpcName = "Room",
                ItemName = itemName,
                Price = price,
                TargetsRoom = true
            }, out _);
        }

        public bool TryExecuteAction(SocialActionRequest request, out string feedback)
        {
            feedback = string.Empty;
            if (request == null || this.currentSession == null)
            {
                return false;
            }

            if (request.TargetsRoom)
            {
                return this.TryExecuteRoomAction(request, out feedback);
            }

            if (!this.TryGetActiveParticipant(request.TargetNpcName, out var profile, out var nightState))
            {
                return false;
            }

            var outcome = this.socialActionService.Execute(
                new SocialActionRequest
                {
                    Action = request.Action,
                    TargetNpcName = request.TargetNpcName,
                    ItemName = request.ItemName,
                    Price = request.Price
                },
                profile,
                nightState);

            this.RecordOutcome(request.TargetNpcName, outcome);
            feedback = outcome.Summary;
            return true;
        }

        public void NoteGift(NPC npc, StardewValley.Object gift, int taste)
        {
            if (!this.TryGetActiveParticipant(npc, out var profile, out var nightState))
            {
                return;
            }

            var outcome = this.consequenceEngine.ApplyGift(profile, nightState, gift, taste);
            this.RecordOutcome(npc.Name, outcome);
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
            var placementPlan = this.placementPlanService.BuildPlan(attendance, nightStates);
            var roomMood = this.roomMoodService.BuildInitialRoomMood(attendance, placementPlan);

            this.currentSession = new SocialSession
            {
                SessionKey = this.GetSessionKey(),
                StartedAtTime = Game1.timeOfDay,
                Attendance = attendance,
                RoomMood = roomMood,
                PlacementPlan = placementPlan,
                NightStates = nightStates
            };

            this.attendeeStagingService.StageAttendees(this.currentSession);
            this.lastStartedSessionKey = this.currentSession.SessionKey;
            this.monitor.Log(
                "Started Saturday social session with attendees: " + string.Join(", ", attendance.SelectedNpcNames),
                LogLevel.Info);
        }

        private void EndSession()
        {
            if (this.currentSession == null)
            {
                return;
            }

            this.attendeeStagingService.RestoreAttendees(this.currentSession);
            this.monitor.Log("Ended Saturday social session.", LogLevel.Trace);
            this.currentSession = null;
        }

        private void RecordOutcome(string npcName, InteractionOutcome outcome)
        {
            this.sessionMemoryService.Record(this.currentSession, new InteractionRecord
            {
                TimeOfDay = Game1.timeOfDay,
                NpcName = npcName,
                Action = outcome.Action.ToString(),
                ResultBeat = outcome.Beat,
                Summary = outcome.Summary,
                VisibleToRoom = outcome.VisibleToRoom
            });

            this.currentSession.RoomMood = this.roomMoodService.Recalculate(this.currentSession);
        }

        private bool TryExecuteRoomAction(SocialActionRequest request, out string feedback)
        {
            feedback = string.Empty;
            var applied = false;

            foreach (var npcName in this.currentSession.Attendance.SelectedNpcNames)
            {
                if (!this.TryGetActiveParticipant(npcName, out var profile, out var nightState))
                {
                    continue;
                }

                this.socialActionService.Execute(
                    new SocialActionRequest
                    {
                        Action = request.Action,
                        TargetNpcName = npcName,
                        ItemName = request.ItemName,
                        Price = request.Price,
                        TargetsRoom = true
                    },
                    profile,
                    nightState);

                applied = true;
            }

            if (!applied)
            {
                return false;
            }

            var outcome = new InteractionOutcome
            {
                Action = request.Action,
                Beat = request.Action == SocialActionType.BuyDrink ? InteractionBeat.GiftedDrink : InteractionBeat.SharedMeal,
                Summary = request.Action == SocialActionType.BuyDrink
                    ? "The farmer bought a round for the room."
                    : "The farmer covered food for the room.",
                VisibleToRoom = true
            };

            this.RecordOutcome("Room", outcome);
            feedback = outcome.Summary;
            return true;
        }

        private bool TryGetActiveParticipant(NPC npc, out NpcProfile profile, out NpcNightState nightState)
        {
            return this.TryGetActiveParticipant(npc?.Name, out profile, out nightState);
        }

        private bool TryGetActiveParticipant(string npcName, out NpcProfile profile, out NpcNightState nightState)
        {
            profile = null;
            nightState = null;
            if (this.currentSession == null || string.IsNullOrWhiteSpace(npcName))
            {
                return false;
            }

            return this.profileService.TryGetProfile(npcName, out profile)
                && this.currentSession.NightStates.TryGetValue(npcName, out nightState);
        }
    }
}
