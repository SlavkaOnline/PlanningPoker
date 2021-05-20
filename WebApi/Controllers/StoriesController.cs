using System;
using System.Threading.Tasks;
using Gateway;
using GrainInterfaces;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using PlanningPoker.Domain;

namespace WebApi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class StoriesController : ControllerBase
    {

        private readonly IClusterClient silo;

        public StoriesController(IClusterClient silo)
        {
            this.silo = silo;
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<Views.StoryView> Get(Guid id)
        {
            var story = silo.GetGrain<IStoryGrain>(id);
            return await story.GetState();
        }

        [HttpPost]
        [Route("{id}/vote")]
        public async Task<Views.StoryView> Vote(Guid id, Requests.Vote request)
        {
            var card = Views.fromString<Card>(request.Card.ToUpper());
            var story = silo.GetGrain<IStoryGrain>(id);
            return await story.Vote(new CommonTypes.User(Guid.NewGuid(), "Empty"), card.Value);
        }

        [HttpPost]
        [Route("{id}/close")]
        public async Task<Views.StoryView> CloseStory(Guid id)
        {
            var story = silo.GetGrain<IStoryGrain>(id);
            return await story.Close(new CommonTypes.User(Guid.NewGuid(), "Empty"));
        }
    }
}