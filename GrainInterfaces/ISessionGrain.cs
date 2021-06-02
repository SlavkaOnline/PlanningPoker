using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Gateway;
using Orleans;
using PlanningPoker.Domain;

namespace GrainInterfaces
{
    public interface ISessionGrain : IDomainGrain<Session.Event>
    {
        Task<Views.SessionView> GetState();
        Task<Views.SessionView> Start(string tittle, CommonTypes.User user);
        Task<Views.SessionView> AddStory(CommonTypes.User user, string title);
        Task<Views.SessionView> SetActiveStory(CommonTypes.User user, Guid id);
        Task<Views.SessionView> AddParticipant(CommonTypes.User user);
        Task<Views.SessionView> RemoveParticipant(Guid id);
    }
}