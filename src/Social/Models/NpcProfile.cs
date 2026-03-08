namespace ValleyTalk.Social.Models
{
    public class NpcProfile
    {
        public string Name { get; set; } = string.Empty;
        public bool IsAdult { get; set; } = true;
        public bool EligibleForSaturdaySocial { get; set; } = true;
        public int AttendanceWeight { get; set; } = 3;
        public int Sociability { get; set; } = 3;
        public int Flirtiness { get; set; } = 2;
        public int LoyaltyToCommitments { get; set; } = 3;
        public int Temper { get; set; } = 2;
        public int AlcoholTolerance { get; set; } = 2;
        public int Guardedness { get; set; } = 3;
        public string PreferredSaloonActivity { get; set; } = "Chatting";
        public string RoleplayNotes { get; set; } = string.Empty;
    }
}
