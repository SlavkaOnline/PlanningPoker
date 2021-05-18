using Orleans;
using PlanningPoker.Domain;
using System.Threading.Tasks;

namespace GrainInterfaces
{
	public interface IStoryGrain : IGrainWithGuidKey
	{
		Task<StoryObj> GetState();
		Task Start(CommonTypes.User user, string title);
	}
}
