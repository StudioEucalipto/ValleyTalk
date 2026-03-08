using System.Collections.Generic;

namespace ValleyTalk.Social.Models
{
    public class RoomMoodState
    {
        public RoomVibe Vibe { get; set; } = RoomVibe.Cozy;
        public int AttendanceSize { get; set; }
        public List<string> VisibleGroups { get; set; } = new List<string>();
        public string Summary { get; set; } = "A warm, low-pressure saloon night.";
    }
}
