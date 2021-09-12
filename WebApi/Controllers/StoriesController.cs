using System;
using System.Threading.Tasks;
using Gateway;
using GrainInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using PlanningPoker.Domain;
using WebApi.Application;

namespace WebApi.Controllers
{
    [Authorize]
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
            return await story.GetState(HttpContext.User.GetDomainUser());
        }

        [HttpPost]
        [Route("{id}/vote")]
        public async Task<Views.StoryView> Vote(Guid id, Requests.Vote request)
        {
            var story = silo.GetGrain<IStoryGrain>(id);
            return await story.Vote(HttpContext.User.GetDomainUser(), request.Card);
        }

        [HttpDelete]
        [Route("{id}/vote")]
        public async Task<Views.StoryView> RemoveVote(Guid id)
        {
            var story = silo.GetGrain<IStoryGrain>(id);
            return await story.RemoveVote(HttpContext.User.GetDomainUser());
        }

        [HttpPost]
        [Route("{id}/close")]
        public async Task<Views.StoryView> Close(Guid id)
        {
            var story = silo.GetGrain<IStoryGrain>(id);
            return await story.Close(HttpContext.User.GetDomainUser());
        }

        [HttpPost]
        [Route("{id}/cleared")]
        public async Task<Views.StoryView> Clear(Guid id)
        {
            var story = silo.GetGrain<IStoryGrain>(id);
            return await story.Clear(HttpContext.User.GetDomainUser(), DateTime.UtcNow);
        }
    }
}