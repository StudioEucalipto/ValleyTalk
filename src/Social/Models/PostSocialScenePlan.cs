namespace ValleyTalk.Social.Models
{
    public class PostSocialScenePlan
    {
        public PostSocialSceneType SceneType { get; set; } = PostSocialSceneType.None;
        public string TargetNpcName { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public int PlayerTileX { get; set; } = -1;
        public int PlayerTileY { get; set; } = -1;
        public int NpcTileX { get; set; } = -1;
        public int NpcTileY { get; set; } = -1;
        public bool TransitionQueued { get; set; }
        public bool DialogueOpened { get; set; }
        public bool SleepQueued { get; set; }
        public FightOutcome FightOutcome { get; set; } = FightOutcome.None;
        public string IntroSummary { get; set; } = string.Empty;
    }
