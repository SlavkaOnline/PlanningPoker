using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Orleans;
using PlanningPoker.Domain;

namespace GrainInterfaces
{
    public interface ISessionGrain : IGrainWithGuidKey
    {
        Task<GrainState<Guid,SessionObj>> GetSate();
        Task SetOwner(CommonTypes.User user);
        Task<Guid> AddStory(CommonTypes.User user, string title);
        Task AddParticipant(CommonTypes.User user);
        Task RemoveParticipant(Guid id);

        Task<ReadOnlyCollection<Guid>> GetStories();
        Task<IReadOnlyList<Session.Event>> GetEventsAfter(int version);
    }
}