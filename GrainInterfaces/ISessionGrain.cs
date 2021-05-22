using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Gateway;
using Orleans;
using PlanningPoker.Domain;

namespace GrainInterfaces
{
    public interface ISessionGrain : IGrainWithGuidKey
    {
        Task<Views.SessionView> GetState();
        Task<Views.SessionView> SetOwner(CommonTypes.User user);
        Task<Views.SessionView> AddStory(CommonTypes.User user, string title);
        Task<Views.SessionView> AddParticipant(CommonTypes.User user);
        Task<Views.SessionView> RemoveParticipant(Guid id);

        Task<IReadOnlyList<Views.EventView<Session.Event>>> GetEventsAfter(int version);
    }
}