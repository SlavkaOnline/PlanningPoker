using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gateway;
using GrainInterfaces;
using Orleans;
using Orleans.EventSourcing;
using Orleans.Providers;
using Orleans.Streams;
using PlanningPoker.Domain;
using PlanningPoker.Extensions;

namespace Grains
{
    public class SessionGrainState
    {
        public SessionObj Session { get; private set; } = SessionObj.zero;

        public void Apply(Session.Event e)
        {
            Session = PlanningPoker.Domain.Session.reducer(Session, e);
        }
    }


    [StorageProvider(ProviderName = "InMemory")]
    [LogConsistencyProvider(ProviderName = "LogStorage")]
    public class SessionGrain : JournaledGrain<SessionGrainState, Session.Event>, ISessionGrain
    {
        private readonly Aggregate<SessionObj, Session.Command, Session.Event> _aggregate = null;
        private IAsyncStream<Views.EventView<Session.Event>> _domainEventStream = null;

        public SessionGrain()
        {
            _aggregate = new Aggregate<SessionObj, Session.Command, Session.Event>(Session.producer, Commit);
        }

        public override Task OnActivateAsync()
        {
            _domainEventStream =
                GetStreamProvider("SMS")
                    .GetStream<Views.EventView<Session.Event>>(this.GetPrimaryKey(), "DomainEvents");
            return base.OnActivateAsync();
        }

        private async Task Commit(Session.Event e)
        {
            RaiseEvent(e);
            await ConfirmEvents();
            await _domainEventStream.OnNextAsync(new Views.EventView<Session.Event>(Version, e));
        }

        public async Task<Views.SessionView> Start(string title, CommonTypes.User user)
        {
            await _aggregate.Exec(State.Session, Session.Command.NewStart(title, user));
            return Views.SessionView.create(this.GetPrimaryKey(), Version, State.Session);
        }

        public async Task<Views.SessionView> AddStory(CommonTypes.User user, string title, string[] cards)
        {
            var id = Guid.NewGuid();
            var story = GrainFactory.GetGrain<IStoryGrain>(id);
            await story.Configure(State.Session.Owner.Value, title, cards);
            await _aggregate.Exec(State.Session, Session.Command.NewAddStory(user, id));
            return Views.SessionView.create(this.GetPrimaryKey(), Version, State.Session);
        }

        public async Task<Views.SessionView> SetActiveStory(CommonTypes.User user, Guid id, DateTime timeStamp)
        {
            var currentStory = State.Session.ActiveStory.GetValue();

            var lazy = new Lazy<Task>(() =>
            {
                if (currentStory == default) return Task.CompletedTask;
                var oldStory = GrainFactory.GetGrain<IStoryGrain>(currentStory);
                return oldStory.Pause(user, timeStamp);

            });

            await _aggregate.Exec(State.Session, Session.Command.NewSetActiveStory(user, id));
            var story = GrainFactory.GetGrain<IStoryGrain>(id);

            await Task.WhenAll(new []{story.SetActive(user, timeStamp), lazy.Value});
            return Views.SessionView.create(this.GetPrimaryKey(), Version, State.Session);
        }

        public async Task<Views.SessionView> AddParticipant(CommonTypes.User user)
        {
            await _aggregate.Exec(State.Session, Session.Command.NewAddParticipant(user));
            return Views.SessionView.create(this.GetPrimaryKey(), Version, State.Session);
        }

        public Task<Views.SessionView> GetState()
            => Task.FromResult(Views.SessionView.create(this.GetPrimaryKey(), Version, State.Session));

        public async Task<Views.SessionView> RemoveParticipant(Guid id)
        {
            await _aggregate.Exec(State.Session, Session.Command.NewRemoveParticipant(id));
            return Views.SessionView.create(this.GetPrimaryKey(), Version, State.Session);
        }

        public async Task<IReadOnlyList<Views.EventView<Session.Event>>> GetEventsAfter(int version)
        {
            var events = await RetrieveConfirmedEvents(version, Version);
            return events.Select((v, order) =>
                new Views.EventView<Session.Event>(version > 0 ? order + version : order, v)).ToArray();
        }
    }
}