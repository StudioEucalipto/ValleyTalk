namespace ValleyTalk;

public interface IValleyTalkInterface
{
    void SetModName(string modName);
    bool IsEnabledForCharacter(StardewValley.NPC character);
    void RegisterPromptOverride(string characterName, string promptElement, string overrideText);
    void ClearPromptOverride(string characterName, string promptElement);
    void ClearPromptOverrides(string characterName = "");
    bool TryBuyDrinkForNpc(string characterName, string itemName, int price = 0);
    bool TryBuyMealForNpc(string characterName, string itemName, int price = 0);
    bool TryBuyDrinkForRoom(string itemName, int price = 0);
}
