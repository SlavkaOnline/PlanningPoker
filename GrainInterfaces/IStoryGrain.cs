using System;
using System.Collections.Generic;
using Orleans;
using PlanningPoker.Domain;
using System.Threading.Tasks;
using Gateway;

namespace GrainInterfaces
{
	public interface IStoryGrain : IDomainGrain<Story.Event>
	{
		Task<Views.Story> GetState(CommonTypes.User user);
		Task<Views.Story> Configure(CommonTypes.User user, string title, string[] cards);
        Task<Views.Story> SetActive(CommonTypes.User user, DateTime startedAt);
        Task<Views.Story> Clear(CommonTypes.User user, DateTime startedAt);
        Task<Views.Story> Vote(CommonTypes.User user, string card, DateTime timeStamp);
        Task<Views.Story> RemoveVote(CommonTypes.User user);
        Task<Views.Story> Close(CommonTypes.User user, DateTime timeStamp, Dictionary<Guid, Guid[]> groups);
        Task<Views.Story> Pause(CommonTypes.User user, DateTime timeStamp);
    }
}
