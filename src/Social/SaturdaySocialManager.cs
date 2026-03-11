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
        private const int RelaxedLateEveningTime = 2200;

        private readonly IMonitor monitor;
        private readonly ModConfig config;
        private readonly IAttendanceService attendanceService;
        private readonly INpcProfileService profileService;
        private readonly INpcNightStateService npcNightStateService;
        private readonly IPlacementPlanService placementPlanService;
        private readonly IAttendeeStagingService attendeeStagingService;
        private readonly ICommerceService commerceService;
        private readonly ISocialRelationshipService relationshipService;
        private readonly IRoomMoodService roomMoodService;
        private readonly IValleyTalkContextBridge contextBridge;
        private readonly ISessionMemoryService sessionMemoryService;
        private readonly IConsequenceEngine consequenceEngine;
        private readonly IConversationCommerceParser conversationCommerceParser;

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
            ICommerceService commerceService,
            ISocialRelationshipService relationshipService,
            IRoomMoodService roomMoodService,
            IValleyTalkContextBridge contextBridge,
            ISessionMemoryService sessionMemoryService,
            IConsequenceEngine consequenceEngine,
            IConversationCommerceParser conversationCommerceParser)
        {
            this.monitor = monitor;
            this.config = config;
            this.attendanceService = attendanceService;
            this.profileService = profileService;
            this.npcNightStateService = npcNightStateService;
            this.placementPlanService = placementPlanService;
            this.attendeeStagingService = attendeeStagingService;
            this.commerceService = commerceService;
            this.relationshipService = relationshipService;
            this.roomMoodService = roomMoodService;
            this.contextBridge = contextBridge;
            this.sessionMemoryService = sessionMemoryService;
            this.consequenceEngine = consequenceEngine;
            this.conversationCommerceParser = conversationCommerceParser;
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

            this.MaintainLowPressureSessionState();
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

        public void NoteConversation(NPC npc, string dialogueText, bool isPlayerLine)
        {
            if (!this.TryGetActiveParticipant(npc, out var profile, out var nightState))
            {
                return;
            }

            if (isPlayerLine && this.conversationCommerceParser.TryParse(dialogueText, out var orderIntent))
            {
                if (orderIntent.ForRoom && orderIntent.IsDrink)
                {
                    if (this.TryBuyDrinkForRoom(orderIntent.ItemName))
                    {
                        return;
                    }
                }

                if (orderIntent.IsDrink)
                {
                    if (this.TryBuyDrinkForNpc(npc.Name, orderIntent.ItemName))
                    {
                        return;
                    }
                }

                if (orderIntent.IsMeal)
                {
                    if (this.TryBuyMealForNpc(npc.Name, orderIntent.ItemName))
                    {
                        return;
                    }
                }
            }

            var relationshipContext = this.relationshipService.BuildContext(this.currentSession, profile, npc.Name);
            var outcome = this.consequenceEngine.ApplyConversation(profile, nightState, relationshipContext, dialogueText, isPlayerLine);
            this.RecordPrimaryAndObservedOutcomes(profile, relationshipContext, outcome);
        }

        public bool TryBuyDrinkForNpc(string npcName, string itemName, int price = 0)
        {
            if (!this.TryGetActiveParticipant(npcName, out var profile, out var nightState))
            {
                return false;
            }

            var relationshipContext = this.relationshipService.BuildContext(this.currentSession, profile, npcName);
            var outcome = this.commerceService.ApplyDrinkOrder(
                new DrinkOrder
                {
                    BuyerName = Game1.player?.Name ?? "Farmer",
                    RecipientName = npcName,
                    ItemName = itemName,
                    Price = price
                },
                profile,
                nightState);

            this.RecordPrimaryAndObservedOutcomes(profile, relationshipContext, outcome);
            return true;
        }

        public bool TryBuyMealForNpc(string npcName, string itemName, int price = 0)
        {
            if (!this.TryGetActiveParticipant(npcName, out var profile, out var nightState))
            {
                return false;
            }

            var relationshipContext = this.relationshipService.BuildContext(this.currentSession, profile, npcName);
            var outcome = this.commerceService.ApplyMealOrder(
                new MealOrder
                {
                    BuyerName = Game1.player?.Name ?? "Farmer",
                    RecipientName = npcName,
                    ItemName = itemName,
                    Price = price
                },
                profile,
                nightState);

            this.RecordPrimaryAndObservedOutcomes(profile, relationshipContext, outcome);
            return true;
        }

        public bool TryBuyDrinkForRoom(string itemName, int price = 0)
        {
            if (this.currentSession == null)
            {
                return false;
            }

            var applied = false;
            foreach (var npcName in this.currentSession.Attendance.SelectedNpcNames)
            {
                if (!this.TryGetActiveParticipant(npcName, out var profile, out var nightState))
                {
                    continue;
                }

                this.commerceService.ApplyDrinkOrder(
                    new DrinkOrder
                    {
                        BuyerName = Game1.player?.Name ?? "Farmer",
                        RecipientName = npcName,
                        ItemName = itemName,
                        Price = price,
                        ForRoom = true
                    },
                    profile,
                    nightState);

                applied = true;
            }

            if (!applied)
            {
                return false;
            }

            this.RecordOutcome(
                "Room",
                new InteractionOutcome
                {
                    Action = SocialActionType.BuyDrink,
                    Beat = InteractionBeat.GiftedDrink,
                    Summary = "The farmer bought a round for the room.",
                    VisibleToRoom = true
                });

            return true;
        }

        public void NoteGift(NPC npc, StardewValley.Object gift, int taste)
        {
            if (!this.TryGetActiveParticipant(npc, out var profile, out var nightState))
            {
                return;
            }

            var relationshipContext = this.relationshipService.BuildContext(this.currentSession, profile, npc.Name);
            var outcome = this.consequenceEngine.ApplyGift(profile, nightState, relationshipContext, gift, taste);
            this.RecordPrimaryAndObservedOutcomes(profile, relationshipContext, outcome);
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
                ClockFrozen = false,
                Attendance = attendance,
                RoomMood = roomMood,
                PlacementPlan = placementPlan,
                NightStates = nightStates
            };

            this.MaintainLowPressureSessionState();
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

            this.RestorePlayerEnergy();
            this.attendeeStagingService.RestoreAttendees(this.currentSession);
            this.monitor.Log("Ended Saturday social session.", LogLevel.Trace);
            this.currentSession = null;
        }

        private void MaintainLowPressureSessionState()
        {
            if (this.currentSession == null || !this.IsSaloon(Game1.player?.currentLocation))
            {
                return;
            }

            this.RestorePlayerEnergy();

            var relaxedTimeAnchor = this.GetRelaxedTimeAnchor();
            if (Game1.timeOfDay > relaxedTimeAnchor)
            {
                Game1.timeOfDay = relaxedTimeAnchor;
                this.currentSession.ClockFrozen = true;
            }
            else
            {
                this.currentSession.ClockFrozen = Game1.timeOfDay >= relaxedTimeAnchor;
            }
        }

        private int GetRelaxedTimeAnchor()
        {
            if (this.currentSession == null)
            {
                return RelaxedLateEveningTime;
            }

            return Math.Max(this.currentSession.StartedAtTime, RelaxedLateEveningTime);
        }

        private void RestorePlayerEnergy()
        {
            if (Game1.player == null)
            {
                return;
            }

            Game1.player.Stamina = (float)Game1.player.MaxStamina;
        }

        private void RecordOutcome(string npcName, InteractionOutcome outcome, bool recalculateRoomMood = true)
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

            if (recalculateRoomMood)
            {
                this.currentSession.RoomMood = this.roomMoodService.Recalculate(this.currentSession);
            }
        }

        private void RecordPrimaryAndObservedOutcomes(NpcProfile profile, SocialRelationshipContext relationshipContext, InteractionOutcome outcome)
        {
            this.RecordOutcome(profile.Name, outcome, recalculateRoomMood: false);

            foreach (var observedReaction in this.relationshipService.ApplyObservedReactions(this.currentSession, profile, relationshipContext, outcome))
            {
                this.sessionMemoryService.Record(this.currentSession, observedReaction);
            }

            this.currentSession.RoomMood = this.roomMoodService.Recalculate(this.currentSession);
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
