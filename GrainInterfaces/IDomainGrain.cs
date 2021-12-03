using System.Collections.Generic;
using System.Threading.Tasks;
using Gateway;
using Orleans;
using PlanningPoker.Domain;

namespace GrainInterfaces
{
    public interface IDomainGrain<TEvent>: IGrainWithGuidKey
    {
        Task<IReadOnlyList<Views.Event<TEvent>>> GetEventsAfter(int version);
    }
}