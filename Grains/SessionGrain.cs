using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using GrainInterfaces;
using Orleans;
using Orleans.EventSourcing;
using Orleans.Providers;
using Orleans.Streams;
using PlanningPoker.Domain;

namespace Grains
{
	public class SessionGrainState
	{
		public SessionObj Session { get; private set; } = SessionObj.zero;
		public void Apply (Session.Event e)
		{
			Session = PlanningPoker.Domain.Session.reducer(Session, e);
		}
	}


	[StorageProvider(ProviderName = "InMemory")]
	[LogConsistencyProvider(ProviderName = "LogStorage")]
	public class SessionGrain : JournaledGrain<SessionGrainState, Session.Event>, ISessionGrain
	{
		private readonly Aggregate<SessionObj, Session.Command, Session.Event> _aggregate = null;
		private IAsyncStream<Session.Event> _domainEventStream = null;

		public SessionGrain()
		{
			_aggregate = new Aggregate<SessionObj, Session.Command, Session.Event>(Session.producer, Commit);
		}

		public override Task OnActivateAsync()
		{
			_domainEventStream = GetStreamProvider("SMS").GetStream<Session.Event>(this.GetPrimaryKey(), "DomainEvents");
			return base.OnActivateAsync();
		}

		private async Task Commit(Session.Event e)
		{
			RaiseEvent(e);
			await ConfirmEvents();
			await _domainEventStream.OnNextAsync(e);
		}

		public async Task SetOwner(CommonTypes.User user)
			=> await _aggregate.Exec(State.Session, Session.Command.NewSetOwner(user));


		public async Task<Guid> AddStory(CommonTypes.User user, string title)
		{
			var id = Guid.NewGuid();
			var story = GrainFactory.GetGrain<IStoryGrain>(id);
			await story.Start(State.Session.Owner.Value, title);
			await _aggregate.Exec(State.Session, Session.Command.NewAddStory(user, id));
			return id;
		}

		public async Task AddParticipant(CommonTypes.User user)
		  => await _aggregate.Exec(State.Session, Session.Command.NewAddParticipant(user));


		public Task<ReadOnlyCollection<Guid>> GetStories()
			=> Task.FromResult(State.Session.Stories.ToList().AsReadOnly());


		public async Task<IReadOnlyList<Session.Event>> GetEventsAfter(int version)
			=> await RetrieveConfirmedEvents(version, Version);


		public async Task RemoveParticipant(Guid id)
			=> await _aggregate.Exec(State.Session, Session.Command.NewRemoveParticipant(id));

		public Task<GrainState<Guid,SessionObj>> GetSate() 
			=> Task.FromResult(new GrainState<Guid, SessionObj>(Version, this.GetPrimaryKey(),  State.Session));
	}
}