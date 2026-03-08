using System.Collections.Generic;
using ValleyTalk.Social.Models;

namespace ValleyTalk.Social.Services
{
    public interface IPlacementPlanService
    {
        PlacementPlan BuildPlan(AttendanceRoll attendance, IDictionary<string, NpcNightState> nightStates);
    }
}
