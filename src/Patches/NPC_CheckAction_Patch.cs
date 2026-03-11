using HarmonyLib;
using StardewValley;
using StardewModdingAPI;

namespace ValleyTalk
{
    /// <summary>
    /// Patch for NPC.checkAction to allow initiating a conversation with typed dialogue
    /// </summary>
    [HarmonyPatch(typeof(NPC), nameof(NPC.checkAction))]
    public class NPC_CheckAction_Patch
    {

        /// <summary>
        /// Prefix method for NPC.checkAction
        /// </summary>
        public static bool Prefix(ref NPC __instance, ref bool __result, Farmer who, GameLocation l)
        {
            var typedDialogueKeyDown = ModEntry.SHelper.Input.IsDown(ModEntry.Config.InitiateTypedDialogueKey);
            var socialActionKeyDown = ModEntry.SHelper.Input.IsDown(ModEntry.Config.SaturdaySocialActionMenuKey);

            // Check for cases when we should not allow initiating typed dialogue
            if (
                __instance.IsInvisible ||
                __instance.isSleeping.Value ||
                !who.CanMove ||
                !DialogueBuilder.Instance.PatchNpc(__instance)
                )
            {
                return true;
            }

            if (socialActionKeyDown && ModEntry.SaturdaySocial?.CanInteractWithNpc(__instance) == true)
            {
                var actions = ModEntry.SaturdaySocial.GetAvailableActionsForNpc(__instance.Name);
                if (actions.Count > 0)
                {
                    Game1.activeClickableMenu = new SaturdaySocialActionMenu(
                        __instance.displayName,
                        actions,
                        action =>
                        {
                            if (ModEntry.SaturdaySocial.TryExecuteAction(action, out var feedback))
                            {
                                Game1.playSound("coin");
                                Game1.showGlobalMessage(feedback);
                            }
                            else
                            {
                                Game1.playSound("cancel");
                                Game1.showGlobalMessage("That social move did not land.");
                            }
                        });
                    __result = false;
                    return false;
                }
            }

            if (!typedDialogueKeyDown)
            {
                return true;
            }

            DialogueBuilder.Instance.ClearContext();
            var character = DialogueBuilder.Instance.GetCharacter(__instance);  
            var prompt = Util.GetString(character, "uiStartConversation", new { Name = __instance.displayName }) ?? $"What do you want to say to {__instance.displayName}?";
            // Show text entry dialog for the player to type their dialogue
            TextInputManager.RequestTextInput
            (
                prompt,
                __instance
            );
            __result = false;
            return false; // Prevent the original method from executing
        }
    }
}
