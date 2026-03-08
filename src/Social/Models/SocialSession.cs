using System.Collections.Generic;

namespace ValleyTalk.Social.Models
{
    public class SocialSession
    {
        public string SessionKey { get; set; } = string.Empty;
        public string LocationName { get; set; } = "Saloon";
        public int StartedAtTime { get; set; }
        public bool ClockFrozen { get; set; }
        public AttendanceRoll Attendance { get; set; } = new AttendanceRoll();
        public RoomMoodState RoomMood { get; set; } = new RoomMoodState();
        public Dictionary<string, NpcNightState> NightStates { get; set; } = new Dictionary<string, NpcNightState>();
        public List<InteractionRecord> Memory { get; set; } = new List<InteractionRecord>();
    }
}
