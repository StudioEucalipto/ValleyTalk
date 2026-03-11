using ValleyTalk.Social.Models;

namespace ValleyTalk.Social.Services
{
    public interface IAttendeeStagingService
    {
        void StageAttendees(SocialSession session);
        void RestoreAttendees(SocialSession session);
    }
}
