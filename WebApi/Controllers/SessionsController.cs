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
        public SessionsController(IClusterClient silo)
        {
            this.silo = silo;
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
		public async Task<Views.SessionView> Create(Requests.CreateSession request)
        {
            var id = Guid.NewGuid();
            var session = silo.GetGrain<ISessionGrain>(id);
			return await session.Start(request.Title, HttpContext.User.GetDomainUser());
        }

		[HttpPost]
		[Route("{id}/stories")]
		public async Task<Views.SessionView> AddStory(Guid id, Requests.CreateStory request)
		{
            var session = silo.GetGrain<ISessionGrain>(id);
            return await session.AddStory(HttpContext.User.GetDomainUser(), request.Title);
        }

        [HttpPost]
        [Route("{id}/join")]
        public async Task<Views.SessionView> Join(Guid id)
        {
            var session = silo.GetGrain<ISessionGrain>(id);
            return await session.AddParticipant(HttpContext.User.GetDomainUser());
        }

        [HttpPost]
        [Route("{id}/leave")]
        public async Task<Views.SessionView> Leave(Guid id)
        {
            var session = silo.GetGrain<ISessionGrain>(id);
            return await session.RemoveParticipant(HttpContext.User.GetDomainUser().Id);
        }

        [HttpPost]
        [Route("{id}/activestory")]
        public async Task<Views.SessionView> SetActiveStory(Guid id, Requests.SetActiveStory request)
        {
            var session = silo.GetGrain<ISessionGrain>(id);
            return await session.SetActiveStory(HttpContext.User.GetDomainUser(), Guid.Parse(request.Id));
        }
	}
}
