namespace ValleyTalk.Social.Models
{
    public class NpcNightState
    {
        public string NpcName { get; set; } = string.Empty;
        public MoodState Mood { get; set; } = MoodState.Relaxed;
        public BuzzLevel BuzzLevel { get; set; } = BuzzLevel.Sober;
        public PlayerHeat PlayerHeat { get; set; } = PlayerHeat.Neutral;
        public OpennessLevel Openness { get; set; } = OpennessLevel.Open;
        public SocialActivity CurrentActivity { get; set; } = SocialActivity.Chatting;
        public InteractionBeat LastInteractionBeat { get; set; } = InteractionBeat.None;
    }
}
