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
        Task<Views.SessionView> AddStory(CommonTypes.User user, string title, string[] cards);
        Task<Views.SessionView> SetActiveStory(CommonTypes.User user, Guid id, DateTime timeStamp);
        Task<Views.SessionView> AddParticipant(CommonTypes.User user);
        Task<Views.SessionView> RemoveParticipant(Guid id);

        Task<Views.SessionView> AddGroup(CommonTypes.User user, Group group);
        Task<Views.SessionView> RemoveGroup(CommonTypes.User user, Guid id);
        Task<Views.SessionView> MoveParticipantToGroup(CommonTypes.User user, Guid participantId, Guid groupId);
    }
}