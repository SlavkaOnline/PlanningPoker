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
		Task<Views.StoryView> GetState(CommonTypes.User user);
		Task<Views.StoryView> Configure(CommonTypes.User user, string title, string[] cards);
        Task<Views.StoryView> SetActive(CommonTypes.User user, DateTime startedAt);
        Task<Views.StoryView> Clear(CommonTypes.User user, DateTime startedAt);
        Task<Views.StoryView> Vote(CommonTypes.User user, string card);
        Task<Views.StoryView> RemoveVote(CommonTypes.User user);
        Task<Views.StoryView> Close(CommonTypes.User user);
        Task<Views.StoryView> Pause(CommonTypes.User user, DateTime timeStamp);
    }
}
