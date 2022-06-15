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

    [StorageProvider(ProviderName = "Database")]
    [LogConsistencyProvider(ProviderName = "LogStorage")]
    public class StoryGrain : JournaledGrain<StoryGrainState, Story.Event>, IStoryGrain
    {
        private readonly Aggregate<StoryObj, Story.Command, Story.Event> _aggregate = null;
        private IAsyncStream<Views.Event<Story.Event>> _eventStream = null;

        public StoryGrain()
        {
            _aggregate = new Aggregate<StoryObj, Story.Command, Story.Event>(Story.producer, Commit);
        }

        public override Task OnActivateAsync()
        {
            _eventStream = GetStreamProvider("SMS")
                .GetStream<Views.Event<Story.Event>>(this.GetPrimaryKey(), CommonTypes.Streams.StoreEvents.Namespace);
            return base.OnActivateAsync();
        }

        private async Task Commit(Story.Event e)
        {
            RaiseEvent(e);
            await ConfirmEvents();
            await _eventStream.OnNextAsync(new Views.Event<Story.Event>(Version, e));
        }

        public async Task<Views.Story> Configure(CommonTypes.User user, string title, string[] cards)
        {
            await _aggregate.Exec(State.Story, Story.Command.NewConfigure(user, title, cards));
            return Views.Story.create(this.GetPrimaryKey(), Version, State.Story, user);
        }

        public async Task<Views.Story> SetActive(CommonTypes.User user, DateTime startedAt)
        {
            await _aggregate.Exec(State.Story, Story.Command.NewSetActive(user, startedAt));
            return Views.Story.create(this.GetPrimaryKey(), Version, State.Story, user);
        }

        public async Task<Views.Story> Clear(CommonTypes.User user, DateTime startedAt)
        {
            await _aggregate.Exec(State.Story, Story.Command.NewClear(user, startedAt));
            return Views.Story.create(this.GetPrimaryKey(), Version, State.Story, user);
        }

        public async Task<Views.Story> Vote(CommonTypes.User user, string card, DateTime timeStamp)
        {
            await _aggregate.Exec(State.Story, Story.Command.NewVote(user, card, timeStamp));
            return Views.Story.create(this.GetPrimaryKey(), Version, State.Story, user);
        }

        public async Task<Views.Story> RemoveVote(CommonTypes.User user)
        {
            await _aggregate.Exec(State.Story, Story.Command.NewRemoveVote(user));
            return Views.Story.create(this.GetPrimaryKey(), Version, State.Story, user);
        }

        public async Task<Views.Story> Close(CommonTypes.User user, DateTime timeStamp, Dictionary<Guid, Guid[]> groups)
        {
            await _aggregate.Exec(State.Story, Story.Command.NewCloseStory(user, timeStamp, groups.Select(kv => new StatisticsGroup(kv.Key, kv.Value.ToArray())).ToArray()));
            return Views.Story.create(this.GetPrimaryKey(), Version, State.Story, user);
        }

        public async Task<Views.Story> Pause(CommonTypes.User user, DateTime timeStamp)
        {
            await _aggregate.Exec(State.Story, Story.Command.NewPause(user, timeStamp));
            return Views.Story.create(this.GetPrimaryKey(), Version, State.Story, user);
        }

        Task<Views.Story> IStoryGrain.GetState(CommonTypes.User user)
            => Task.FromResult(Views.Story.create(this.GetPrimaryKey(), Version, State.Story, user));

        public async Task<IReadOnlyList<Views.Event<Story.Event>>> GetEventsAfter(int version)
        {
            var versions = await RetrieveConfirmedEvents(version, Version);
            return versions
                .Select((v, order) => new Views.Event<Story.Event>(version > 0 ? order + version + 1 : order + 1, v))
                .ToArray();
        }
    }
}