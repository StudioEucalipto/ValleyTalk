using System.Collections.Generic;

namespace ValleyTalk.Social.Models
{
    public class AttendanceRoll
    {
        public int MinimumAttendance { get; set; }
        public int MaximumAttendance { get; set; }
        public List<string> SelectedNpcNames { get; set; } = new List<string>();
    }
}
