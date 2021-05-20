using GrainInterfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using PlanningPoker.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gateway;

namespace WebApi.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class SessionsController : ControllerBase
	{
		private readonly IClusterClient silo;

        public SessionsController(IClusterClient silo)
		{
			this.silo = silo;
		}

        [HttpGet]
        [Route("{id}")]
        public async Task<Views.SessionView> Get(Guid id)
        {
            var story = silo.GetGrain<ISessionGrain>(id);
            return await story.GetState();
        }

		[HttpPost]
		public async Task<Views.SessionView> Create()
		{
			var id = Guid.NewGuid();
			var session = silo.GetGrain<ISessionGrain>(id);
			return await session.SetOwner(new CommonTypes.User(Guid.NewGuid(), "Slava"));
        }

		[HttpPost]
		[Route("{id}/stories")]
		public async Task<Views.SessionView> AddStory(Guid id, Requests.CreateStory request)
		{
            var session = silo.GetGrain<ISessionGrain>(id);
            return await session.AddStory(new CommonTypes.User(Guid.NewGuid(), "Slava"), request.Title);
        }

        [HttpPost]
        [Route("{id}/join")]
        public async Task<Views.SessionView> Join(Guid id)
        {
            var session = silo.GetGrain<ISessionGrain>(id);
            return await session.AddParticipant(new CommonTypes.User(Guid.NewGuid(), "Participant"));
        }

        [HttpPost]
        [Route("{id}/leave")]
        public async Task<Views.SessionView> Leave(Guid id)
        {
            var session = silo.GetGrain<ISessionGrain>(id);
            return await session.RemoveParticipant(Guid.NewGuid());
        }

	}
}
