using System.Collections.Generic;
using ValleyTalk.Social.Models;

namespace ValleyTalk.Social.Services
{
    public interface ISocialRelationshipService
    {
        SocialRelationshipContext BuildContext(SocialSession session, NpcProfile profile, string npcName);
        IReadOnlyList<InteractionRecord> ApplyObservedReactions(SocialSession session, NpcProfile targetProfile, SocialRelationshipContext relationshipContext, InteractionOutcome outcome);
    }
}
