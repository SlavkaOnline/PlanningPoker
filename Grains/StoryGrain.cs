using GrainInterfaces;
using Orleans.EventSourcing;
using PlanningPoker.Domain;
using System;
using Orleans;
using System.Threading.Tasks;
using Orleans.Providers;

namespace Grains
{
	[StorageProvider(ProviderName = "InMemory")]
	[LogConsistencyProvider(ProviderName = "LogStorage")]
	public class StoryGrain : JournaledGrain<StoryObj, Story.Event>, IStoryGrain
	{

		private readonly Aggregate<StoryObj, Story.Command, Story.Event> _aggregate = null;

		public StoryGrain()
		{
			_aggregate = new Aggregate<StoryObj, Story.Command, Story.Event>(Story.producer, Commit);
		}

		private async Task Commit(Story.Event e)
		{
			RaiseEvent(e);
			await ConfirmEvents();
		}

		public Task<StoryObj> GetState() => Task.FromResult(State);

		public async Task Start(CommonTypes.User user, string title)
		 => await _aggregate.Exec(State, Story.Command.NewStartStory(user, title, DateTime.UtcNow));
	}
}
