using GrainInterfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using PlanningPoker.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApi.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class SessionController : ControllerBase
	{
		private readonly IClusterClient silo;

		public SessionController(IClusterClient silo)
		{
			this.silo = silo;
		}

		[HttpPost]
		public async Task<ActionResult> Create()
		{
			var id = Guid.NewGuid();
			var session = silo.GetGrain<ISessionGrain>(id);
			await session.SetOwner(new CommonTypes.User(Guid.NewGuid(), "Slava"));
			var state = await session.GetSate();
			return Ok(state);
		}

		[HttpGet]
		[Route("{id}/events")]
		public async Task<ActionResult> GetEvents(Guid id)
		{
			var session = silo.GetGrain<ISessionGrain>(id);
			var events = await session.GetEventsAfter(0);
			return Ok(events);

		}
	}
}
