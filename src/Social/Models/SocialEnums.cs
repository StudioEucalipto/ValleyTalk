namespace ValleyTalk.Social.Models
{
    public enum RoomVibe
    {
        Cozy,
        Lively,
        Awkward,
        Tense,
        Rowdy,
        Romantic
    }

    public enum MoodState
    {
        Relaxed,
        Cheerful,
        Lonely,
        Guarded,
        Irritated,
        Rowdy
    }

    public enum BuzzLevel
    {
        Sober,
        Buzzed,
        Tipsy,
        Drunk
    }

    public enum PlayerHeat
    {
        Negative,
        Neutral,
        Interested,
        Attracted
    }

    public enum OpennessLevel
    {
        Closed,
        Open,
        Bold
    }

    public enum SocialActivity
    {
        Chatting,
        Drinking,
        Eating,
        Dancing,
        Lingering,
        Brooding
    }

    public enum InteractionBeat
    {
        None,
        Chatted,
        GiftedDrink,
        SharedMeal,
        Danced,
        Flirted,
        Argued,
        Rejected,
        PrivateInvite
    }

    public enum SocialActionType
    {
        Chat,
        Flirt,
        InviteDance,
        Apologize,
        Provoke,
        SuggestPrivateConversation,
        BuyDrink,
        BuyMeal
    }
}
