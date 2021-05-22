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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using WebApi.Application;

namespace WebApi.Controllers
{
    [Authorize]
	[Route("api/[controller]")]
	[ApiController]
	public class SessionsController : ControllerBase
	{
		private readonly IClusterClient silo;
        private readonly UserManager<ApplicationUser> _userManager;

        public SessionsController(IClusterClient silo, UserManager<ApplicationUser> userManager)
        {
            this.silo = silo;
            _userManager = userManager;
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<Views.SessionView> Get(Guid id)
        {
            var session = silo.GetGrain<ISessionGrain>(id);
            return await session.GetState();
        }

        [HttpGet]
        [Route("{id}/events")]
        public async Task<IReadOnlyList<Views.EventView<Session.Event>>> GetEvents(Guid id)
        {
            var session = silo.GetGrain<IDomainGrain<Session.Event>>(id);
            return await session.GetEventsAfter(0);
        }

		[HttpPost]
		public async Task<Views.SessionView> Create()
        {
            var user = await  _userManager.GetUserAsync(HttpContext.User);
			var id = Guid.NewGuid();
			var session = silo.GetGrain<ISessionGrain>(id);
			return await session.SetOwner(new CommonTypes.User(Guid.Parse(user.Id), user.UserName));
        }

		[HttpPost]
		[Route("{id}/stories")]
		public async Task<Views.SessionView> AddStory(Guid id, Requests.CreateStory request)
		{
            var user = await  _userManager.GetUserAsync(HttpContext.User);
            var session = silo.GetGrain<ISessionGrain>(id);
            return await session.AddStory(new CommonTypes.User(Guid.Parse(user.Id), user.UserName), request.Title);
        }

        [HttpPost]
        [Route("{id}/join")]
        public async Task<Views.SessionView> Join(Guid id)
        {
            var user = await  _userManager.GetUserAsync(HttpContext.User);
            var session = silo.GetGrain<ISessionGrain>(id);
            return await session.AddParticipant(new CommonTypes.User(Guid.Parse(user.Id), user.UserName));
        }

        [HttpPost]
        [Route("{id}/leave")]
        public async Task<Views.SessionView> Leave(Guid id)
        {
            var user = await  _userManager.GetUserAsync(HttpContext.User);
            var session = silo.GetGrain<ISessionGrain>(id);
            return await session.RemoveParticipant(Guid.Parse(user.Id));
        }

	}
}
