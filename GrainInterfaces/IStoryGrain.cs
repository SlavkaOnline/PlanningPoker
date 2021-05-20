using Orleans;
using PlanningPoker.Domain;
using System.Threading.Tasks;
using Gateway;

namespace GrainInterfaces
{
	public interface IStoryGrain : IGrainWithGuidKey
	{
		Task<Views.StoryView> GetState();
		Task<Views.StoryView> Start(CommonTypes.User user, string title);
        Task<Views.StoryView> Vote(CommonTypes.User user, Card card);
        Task<Views.StoryView> Close(CommonTypes.User user);
    }
}
