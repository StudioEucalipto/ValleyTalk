using ValleyTalk.Social.Models;

namespace ValleyTalk.Social.Services
{
    public interface IRoomMoodService
    {
        RoomMoodState BuildInitialRoomMood(AttendanceRoll attendance, PlacementPlan placementPlan);
        RoomMoodState Recalculate(SocialSession session);
    }
}
