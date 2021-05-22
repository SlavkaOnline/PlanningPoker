using GrainInterfaces;
using Orleans.EventSourcing;
using PlanningPoker.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using Orleans;
using System.Threading.Tasks;
using Gateway;
using Orleans.Providers;
using Orleans.Streams;

namespace Grains
{
    public class StoryGrainState
    {
        public StoryObj Story { get; private set; } = StoryObj.zero;

        public void Apply(Story.Event e)
        {
            Story = PlanningPoker.Domain.Story.reducer(Story, e);
        }
    }

    [StorageProvider(ProviderName = "InMemory")]
    [LogConsistencyProvider(ProviderName = "LogStorage")]
    public class StoryGrain : JournaledGrain<StoryGrainState, Story.Event>, IStoryGrain
    {
        private readonly Aggregate<StoryObj, Story.Command, Story.Event> _aggregate = null;
        private IAsyncStream<Views.EventView<Story.Event>> _domainEventStream = null;

        public StoryGrain()
        {
            _aggregate = new Aggregate<StoryObj, Story.Command, Story.Event>(Story.producer, Commit);
        }

        public override Task OnActivateAsync()
        {
            _domainEventStream = GetStreamProvider("SMS").GetStream<Views.EventView<Story.Event>>(this.GetPrimaryKey(), "DomainEvents");
            return base.OnActivateAsync();
        }

        private async Task Commit(Story.Event e)
        {
            RaiseEvent(e);
            await ConfirmEvents();
            await _domainEventStream.OnNextAsync(new Views.EventView<Story.Event>(Version - 1, e));
        }

        public async Task<Views.StoryView> Start(CommonTypes.User user, string title)
        {
            await _aggregate.Exec(State.Story, Story.Command.NewStartStory(user, title, DateTime.UtcNow));
            return Views.StoryView.create(this.GetPrimaryKey(), Version, State.Story);
        }

        public async Task<Views.StoryView> Vote(CommonTypes.User user, Card card)
        {
            await _aggregate.Exec(State.Story, Story.Command.NewVote(user, card, DateTime.UtcNow));
            return Views.StoryView.create(this.GetPrimaryKey(), Version, State.Story);
        }

        public async Task<Views.StoryView> RemoveVote(CommonTypes.User user)
        {
            await _aggregate.Exec(State.Story, Story.Command.NewRemoveVote(user));
            return Views.StoryView.create(this.GetPrimaryKey(), Version, State.Story);
        }

        public async Task<Views.StoryView> Close(CommonTypes.User user)
        {
            await _aggregate.Exec(State.Story, Story.Command.NewCloseStory(user, DateTime.UtcNow));
            return Views.StoryView.create(this.GetPrimaryKey(), Version, State.Story);
            ;
        }

        Task<Views.StoryView> IStoryGrain.GetState()
            => Task.FromResult(Views.StoryView.create(this.GetPrimaryKey(), Version, State.Story));

        public async Task<IReadOnlyList<Views.EventView<Story.Event>>> GetEventsAfter(int version)
        {
            var versions = await RetrieveConfirmedEvents(version, Version);
            return versions.Select((v, order) => new Views.EventView<Story.Event>(order, v)).ToArray();
        }
    }
}