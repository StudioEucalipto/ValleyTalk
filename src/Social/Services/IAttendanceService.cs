using ValleyTalk.Social.Models;

namespace ValleyTalk.Social.Services
{
    public interface IAttendanceService
    {
        AttendanceRoll RollAttendance(int minimumAttendance, int maximumAttendance);
    }
}
