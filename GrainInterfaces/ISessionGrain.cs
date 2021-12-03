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
        Task<Views.Session> GetState();
        Task<Views.Session> Start(string tittle, CommonTypes.User user);
        Task<Views.Session> AddStory(CommonTypes.User user, string title, string[] cards);
        Task<Views.Session> SetActiveStory(CommonTypes.User user, Guid id, DateTime timeStamp);
        Task<Views.Session> AddParticipant(CommonTypes.User user);
        Task<Views.Session> RemoveParticipant(Guid id);

        Task<Views.Session> AddGroup(CommonTypes.User user, Group group);
        Task<Views.Session> RemoveGroup(CommonTypes.User user, Guid id);
        Task<Views.Session> MoveParticipantToGroup(CommonTypes.User user, Guid participantId, Guid groupId);
    }
}