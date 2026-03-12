using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
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
        private readonly Random random = new Random();
        private readonly MethodInfo warpCharacterMethod;

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
        }

        public bool HasActiveSession => this.currentSession != null;

        public void HandleWarp(GameLocation oldLocation, GameLocation newLocation)
        {
            if (!this.config.EnableSaturdaySocial)
            {
                return;
            }

            if (this.currentSession == null)
            {
                if (this.IsSaloon(newLocation) && this.CanStartSession())
                {
                    this.StartSession();
                }

                return;
            }

            var exitedSaloon = this.IsSaloon(oldLocation) && !this.IsSaloon(newLocation);
            if (this.currentSession.Phase == SocialSessionPhase.Saloon && exitedSaloon)
            {
                this.HandleSaloonExit(newLocation);
                return;
            }

            if (this.currentSession.Phase == SocialSessionPhase.Saloon && !this.IsSaloon(newLocation))
            {
                this.EndSession();
            }
        }

        public void ResetForNewDay()
        {
            this.EndSession(restoreAttendees: false);
            this.lastStartedSessionKey = null;
        }

        public void ResetForTitle()
        {
            this.EndSession();
            this.lastStartedSessionKey = null;
        }

        public void HandleTimeChanged()
        {
            if (this.currentSession == null)
            {
                return;
            }

            this.MaintainLowPressureSessionState();
            if (this.currentSession.Phase == SocialSessionPhase.Saloon && this.IsSaloon(Game1.player?.currentLocation))
            {
                this.attendeeStagingService.StageAttendees(this.currentSession);
            }
        }

        public void HandleUpdateTicked(ulong ticks)
        {
            if (this.currentSession == null)
            {
                return;
            }

            this.MaintainLowPressureSessionState();
            var scene = this.currentSession.PendingScene;
            if (this.currentSession.Phase != SocialSessionPhase.FollowUpScene || scene.SceneType == PostSocialSceneType.None)
            {
                return;
            }

            if (!scene.TransitionQueued && !this.IsAtPlannedSceneLocation(scene))
            {
                scene.TransitionQueued = true;
                this.TransitionPlayerToFollowUpScene(scene);
                return;
            }

            if (!scene.DialogueOpened)
            {
                if (this.IsAtPlannedSceneLocation(scene) && Game1.activeClickableMenu == null && !AsyncBuilder.Instance.AwaitingGeneration)
                {
                    this.OpenFollowUpDialogue(scene);
                    this.currentSession.LastFollowUpActivityTick = ticks;
                }

                return;
            }

            if (Game1.activeClickableMenu != null || AsyncBuilder.Instance.AwaitingGeneration || TextInputManager.AwaitingTextInput)
            {
                this.currentSession.LastFollowUpActivityTick = ticks;
                return;
            }

            if (scene.SceneType == PostSocialSceneType.PlayerBedroomRomance
                || scene.SceneType == PostSocialSceneType.NpcBedroomRomance)
            {
                return;
            }

            if (ticks - this.currentSession.LastFollowUpActivityTick < 15)
            {
                return;
            }

            this.ResolveFollowUpScene(scene);
        }

        public bool TryGetPromptContext(NPC npc, out string contextText)
        {
            contextText = string.Empty;
            if (this.currentSession == null || npc == null)
            {
                return false;
            }

            if (this.currentSession.Phase == SocialSessionPhase.FollowUpScene
                && !string.Equals(this.currentSession.FollowUpNpcName, npc.Name, StringComparison.OrdinalIgnoreCase))
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

            if (isPlayerLine && this.currentSession?.Phase == SocialSessionPhase.FollowUpScene)
            {
                this.currentSession.LastFollowUpActivityTick = Game1.ticks;
            }

            if (isPlayerLine
                && this.currentSession?.Phase == SocialSessionPhase.FollowUpScene
                && this.IsSleepRequest(dialogueText))
            {
                this.ResolveFollowUpScene(this.currentSession.PendingScene);
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
            this.TryQueuePostSocialScene(profile, nightState, relationshipContext, dialogueText, isPlayerLine, outcome);
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
                Phase = SocialSessionPhase.Saloon,
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

        private void EndSession(bool restoreAttendees = true)
        {
            if (this.currentSession == null)
            {
                return;
            }

            if (restoreAttendees)
            {
                this.attendeeStagingService.RestoreAttendees(this.currentSession);
            }

            this.monitor.Log("Ended Saturday social session.", LogLevel.Trace);
            this.currentSession = null;
        }

        private void HandleSaloonExit(GameLocation newLocation)
        {
            if (this.currentSession == null)
            {
                return;
            }

            var scene = this.currentSession.PendingScene;
            if (scene.SceneType == PostSocialSceneType.None)
            {
                this.EndSession();
                this.BeginNightRest();
                return;
            }

            this.RemoveTargetSnapshot(scene.TargetNpcName);
            this.attendeeStagingService.RestoreAttendees(this.currentSession);
            this.currentSession.Phase = SocialSessionPhase.FollowUpScene;
            this.currentSession.FollowUpNpcName = scene.TargetNpcName;
            this.currentSession.LastFollowUpActivityTick = Game1.ticks;

            if (scene.SceneType == PostSocialSceneType.OutsideFight)
            {
                scene.LocationName = newLocation?.NameOrUniqueName ?? "Town";
                var playerTile = Game1.player?.TilePoint ?? new Point(32, 62);
                scene.PlayerTileX = playerTile.X;
                scene.PlayerTileY = playerTile.Y;
                scene.NpcTileX = playerTile.X + 1;
                scene.NpcTileY = playerTile.Y;
                this.PlaceNpcForScene(scene);
            }
        }

        private void MaintainLowPressureSessionState()
        {
            if (this.currentSession == null)
            {
                return;
            }

            var relaxedTimeAnchor = Math.Max(this.currentSession.StartedAtTime, RelaxedLateEveningTime);
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

        private void TryQueuePostSocialScene(
            NpcProfile profile,
            NpcNightState nightState,
            SocialRelationshipContext relationshipContext,
            string dialogueText,
            bool isPlayerLine,
            InteractionOutcome outcome)
        {
            if (!isPlayerLine || this.currentSession == null || this.currentSession.Phase != SocialSessionPhase.Saloon)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(dialogueText) || outcome.Beat == InteractionBeat.Rejected)
            {
                return;
            }

            if (this.TryCreateOutsideFightPlan(profile, dialogueText, out var fightPlan))
            {
                this.QueuePendingScene(fightPlan);
                return;
            }

            if (!profile.IsAdult)
            {
                return;
            }

            if (this.TryCreatePlayerBedroomPlan(profile, nightState, relationshipContext, dialogueText, out var playerBedroomPlan))
            {
                this.QueuePendingScene(playerBedroomPlan);
                return;
            }

            if (this.TryCreateNpcBedroomPlan(profile, nightState, relationshipContext, dialogueText, out var npcBedroomPlan))
            {
                this.QueuePendingScene(npcBedroomPlan);
            }
        }

        private bool TryCreateOutsideFightPlan(NpcProfile profile, string dialogueText, out PostSocialScenePlan scene)
        {
            scene = null;
            var normalized = dialogueText.ToLowerInvariant();
            if (!normalized.Contains("take this outside")
                && !normalized.Contains("settle this outside")
                && !normalized.Contains("step outside")
                && !normalized.Contains("outside right now")
                && !normalized.Contains("meet me outside"))
            {
                return false;
            }

            scene = new PostSocialScenePlan
            {
                SceneType = PostSocialSceneType.OutsideFight,
                TargetNpcName = profile.Name,
                IntroSummary = "The argument spilled outside the saloon."
            };
            return true;
        }

        private bool TryCreatePlayerBedroomPlan(
            NpcProfile profile,
            NpcNightState nightState,
            SocialRelationshipContext relationshipContext,
            string dialogueText,
            out PostSocialScenePlan scene)
        {
            scene = null;
            if (!this.IsRomanceReady(profile, nightState, relationshipContext))
            {
                return false;
            }

            var normalized = dialogueText.ToLowerInvariant();
            if (!normalized.Contains("come home with me")
                && !normalized.Contains("come back to my place")
                && !normalized.Contains("come to my place")
                && !normalized.Contains("come back to the farm")
                && !normalized.Contains("come to the farm")
                && !normalized.Contains("my room")
                && !normalized.Contains("my bed"))
            {
                return false;
            }

            var bedTile = this.GetPlayerBedTile();
            scene = new PostSocialScenePlan
            {
                SceneType = PostSocialSceneType.PlayerBedroomRomance,
                TargetNpcName = profile.Name,
                LocationName = "FarmHouse",
                PlayerTileX = bedTile.X,
                PlayerTileY = bedTile.Y,
                NpcTileX = bedTile.X + 1,
                NpcTileY = bedTile.Y,
                IntroSummary = "The night continued back at the farmhouse."
            };
            return true;
        }

        private bool TryCreateNpcBedroomPlan(
            NpcProfile profile,
            NpcNightState nightState,
            SocialRelationshipContext relationshipContext,
            string dialogueText,
            out PostSocialScenePlan scene)
        {
            scene = null;
            if (!this.IsRomanceReady(profile, nightState, relationshipContext))
            {
                return false;
            }

            var npc = Game1.getCharacterFromName(profile.Name);
            var home = npc?.GetData()?.Home?.FirstOrDefault();
            if (home == null || string.IsNullOrWhiteSpace(home.Location))
            {
                return false;
            }

            var normalized = dialogueText.ToLowerInvariant();
            if (!normalized.Contains("walk you home")
                && !normalized.Contains("take you home")
                && !normalized.Contains("go to your place")
                && !normalized.Contains("back to your place")
                && !normalized.Contains("your room")
                && !normalized.Contains("your bed")
                && !normalized.Contains("can i come home with you"))
            {
                return false;
            }

            var bedTile = home.Tile;
            if (bedTile.X <= 0 && bedTile.Y <= 0)
            {
                bedTile = new Point(3, 3);
            }

            scene = new PostSocialScenePlan
            {
                SceneType = PostSocialSceneType.NpcBedroomRomance,
                TargetNpcName = profile.Name,
                LocationName = home.Location,
                PlayerTileX = bedTile.X,
                PlayerTileY = bedTile.Y,
                NpcTileX = bedTile.X + 1,
                NpcTileY = bedTile.Y,
                IntroSummary = "The night continued back at " + profile.Name + "'s home."
            };
            return true;
        }

        private bool IsRomanceReady(NpcProfile profile, NpcNightState nightState, SocialRelationshipContext relationshipContext)
        {
            if (relationshipContext.CommittedPartnerPresent && profile.LoyaltyToCommitments >= 5 && nightState.PlayerHeat < PlayerHeat.Attracted)
            {
                return false;
            }

            return nightState.PlayerHeat >= PlayerHeat.Interested
                || relationshipContext.PlayerRelationshipStatus == "dating"
                || relationshipContext.PlayerRelationshipStatus == "engaged"
                || relationshipContext.PlayerRelationshipStatus == "married"
                || profile.Flirtiness >= 4;
        }

        private void QueuePendingScene(PostSocialScenePlan scene)
        {
            if (this.currentSession == null || scene == null)
            {
                return;
            }

            this.currentSession.PendingScene = scene;
            this.sessionMemoryService.Record(this.currentSession, new InteractionRecord
            {
                TimeOfDay = Game1.timeOfDay,
                NpcName = scene.TargetNpcName,
                Action = "SceneQueued",
                ResultBeat = InteractionBeat.PrivateInvite,
                Summary = scene.IntroSummary,
                VisibleToRoom = false
            });
        }

        private bool IsAtPlannedSceneLocation(PostSocialScenePlan scene)
        {
            return scene != null
                && Game1.player?.currentLocation != null
                && string.Equals(Game1.player.currentLocation.NameOrUniqueName, scene.LocationName, StringComparison.OrdinalIgnoreCase);
        }

        private void TransitionPlayerToFollowUpScene(PostSocialScenePlan scene)
        {
            if (scene == null)
            {
                return;
            }

            if (this.IsAtPlannedSceneLocation(scene))
            {
                scene.TransitionQueued = false;
                this.PlaceNpcForScene(scene);
                return;
            }

            Game1.globalFadeToBlack(() =>
            {
                Game1.warpFarmer(scene.LocationName, scene.PlayerTileX, scene.PlayerTileY, false);
                this.PlaceNpcForScene(scene);
                scene.TransitionQueued = false;
            }, 0.02f);
        }

        private void PlaceNpcForScene(PostSocialScenePlan scene)
        {
            if (scene == null || string.IsNullOrWhiteSpace(scene.TargetNpcName))
            {
                return;
            }

            var npc = Game1.getCharacterFromName(scene.TargetNpcName);
            if (npc == null)
            {
                return;
            }

            if (scene.NpcTileX < 0 || scene.NpcTileY < 0)
            {
                scene.NpcTileX = scene.PlayerTileX + 1;
                scene.NpcTileY = scene.PlayerTileY;
            }

            this.TryWarpNpc(npc, scene.LocationName, scene.NpcTileX, scene.NpcTileY);
            npc.faceDirection(3);
            Game1.player.faceDirection(1);
        }

        private void OpenFollowUpDialogue(PostSocialScenePlan scene)
        {
            var npc = Game1.getCharacterFromName(scene.TargetNpcName);
            if (npc == null)
            {
                this.ResolveFollowUpScene(scene);
                return;
            }

            scene.DialogueOpened = true;
            DialogueBuilder.Instance.ClearContext();
            AsyncBuilder.Instance.RequestNpcBasic(npc, "SaturdaySocialFollowUp", string.Empty);
        }

        private void ResolveFollowUpScene(PostSocialScenePlan scene)
        {
            if (scene == null || scene.SleepQueued)
            {
                return;
            }

            if (scene.SceneType == PostSocialSceneType.OutsideFight
                && this.TryGetActiveParticipant(scene.TargetNpcName, out var profile, out var nightState))
            {
                scene.FightOutcome = this.RollFightOutcome(profile, nightState);
                this.RecordOutcome(scene.TargetNpcName, new InteractionOutcome
                {
                    Action = SocialActionType.Provoke,
                    Beat = InteractionBeat.Argued,
                    Summary = this.BuildFightSummary(profile.Name, scene.FightOutcome),
                    VisibleToRoom = false
                });
            }

            scene.SleepQueued = true;
            this.currentSession.Phase = SocialSessionPhase.ResolvingNight;
            this.EndSession(restoreAttendees: false);
            this.BeginNightRest();
        }

        private FightOutcome RollFightOutcome(NpcProfile profile, NpcNightState nightState)
        {
            var playerScore = this.random.Next(1, 7) + (Game1.player?.LuckLevel ?? 0);
            var npcScore = this.random.Next(1, 7) + profile.Temper + (int)nightState.BuzzLevel;
            if (Math.Abs(playerScore - npcScore) <= 1)
            {
                return FightOutcome.BrokenUp;
            }

            return playerScore > npcScore ? FightOutcome.PlayerWins : FightOutcome.NpcWins;
        }

        private string BuildFightSummary(string npcName, FightOutcome outcome)
        {
            switch (outcome)
            {
                case FightOutcome.PlayerWins:
                    return "Outside the saloon, the farmer got the better of " + npcName + " before the fight burned out.";
                case FightOutcome.NpcWins:
                    return "Outside the saloon, " + npcName + " put the farmer on the back foot before it was over.";
                default:
                    return "Outside the saloon, the fight never turned into a clean win for anyone before it got broken up.";
            }
        }

        private bool IsSleepRequest(string dialogueText)
        {
            if (string.IsNullOrWhiteSpace(dialogueText))
            {
                return false;
            }

            var normalized = dialogueText.ToLowerInvariant();
            return normalized.Contains("go to sleep")
                || normalized.Contains("let's sleep")
                || normalized.Contains("lets sleep")
                || normalized.Contains("go to bed")
                || normalized.Contains("let's go to bed")
                || normalized.Contains("lets go to bed")
                || normalized.Contains("stay the night")
                || normalized.Contains("turn in for the night")
                || normalized.Contains("call it a night")
                || normalized.Contains("get some sleep")
                || normalized.Contains("sleep now");
        }

        private void BeginNightRest()
        {
            Game1.globalFadeToBlack(() =>
            {
                this.WarpPlayerHomeForSleep();
                if (!this.TryInvokeStartSleep())
                {
                    Farmer.passOutFromTired(Game1.player);
                }
            }, 0.02f);
        }

        private void WarpPlayerHomeForSleep()
        {
            var bedTile = this.GetPlayerBedTile();
            Game1.warpFarmer("FarmHouse", bedTile.X, bedTile.Y, false);
        }

        private Point GetPlayerBedTile()
        {
            if (this.TryFindBedTile(Game1.getLocationFromName("FarmHouse"), out var bedTile))
            {
                return bedTile;
            }

            return new Point(9, 8);
        }

        private bool TryFindBedTile(GameLocation location, out Point tile)
        {
            tile = default;
            if (location == null)
            {
                return false;
            }

            foreach (var methodName in new[] { "GetPlayerBedSpot", "getBedSpot", "GetMainBedSpot", "GetBedSpot" })
            {
                var method = location.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (method == null || method.GetParameters().Length != 0)
                {
                    continue;
                }

                if (this.TryConvertToPoint(method.Invoke(location, null), out tile))
                {
                    return true;
                }
            }

            foreach (var propertyName in new[] { "playerBedSpot", "PlayerBedSpot", "BedSpot" })
            {
                var property = location.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (property == null)
                {
                    continue;
                }

                if (this.TryConvertToPoint(property.GetValue(location), out tile))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryConvertToPoint(object value, out Point point)
        {
            point = default;
            switch (value)
            {
                case Point asPoint:
                    point = asPoint;
                    return true;
                case Vector2 asVector:
                    point = new Point((int)asVector.X, (int)asVector.Y);
                    return true;
                default:
                    return false;
            }
        }

        private bool TryInvokeStartSleep()
        {
            var location = Game1.player?.currentLocation;
            if (location == null)
            {
                return false;
            }

            var startSleep = location.GetType().GetMethod("startSleep", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (startSleep == null || startSleep.GetParameters().Length != 0)
            {
                return false;
            }

            startSleep.Invoke(location, null);
            return true;
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
                this.monitor.Log("Saturday social follow-up warp failed for '" + npc.Name + "': " + ex.Message, LogLevel.Warn);
                return false;
            }
        }

        private void RemoveTargetSnapshot(string npcName)
        {
            if (this.currentSession?.StageSnapshots == null || string.IsNullOrWhiteSpace(npcName))
            {
                return;
            }

            this.currentSession.StageSnapshots.Remove(npcName);
        }

        private void RecordOutcome(string npcName, InteractionOutcome outcome, bool recalculateRoomMood = true)
        {
            if (this.currentSession == null)
            {
                return;
            }

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
