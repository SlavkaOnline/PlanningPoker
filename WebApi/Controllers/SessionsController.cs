using GrainInterfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using PlanningPoker.Domain;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using Gateway;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
		private readonly IClusterClient _silo;
		private readonly CardsTypeProvider _cardsTypeProvider;

		public SessionsController(IClusterClient silo, CardsTypeProvider cardsTypeProvider)
		{
			_silo = silo;
			_cardsTypeProvider = cardsTypeProvider;
		}

        [HttpGet]
        [Route("{id:guid}")]
        public async Task<Views.SessionView> Get(Guid id)
        {
            var session = _silo.GetGrain<ISessionGrain>(id);
            return await session.GetState();
        }

        [HttpGet]
        [Route("{id:guid}/events")]
        public async Task<IReadOnlyList<Views.EventView<Session.Event>>> GetEvents(Guid id)
        {
            var session = _silo.GetGrain<IDomainGrain<Session.Event>>(id);
            return await session.GetEventsAfter(0);
        }

		[HttpPost]
		public async Task<Views.SessionView> Create(Requests.CreateSession request)
        {
            var id = Guid.NewGuid();
            var session = _silo.GetGrain<ISessionGrain>(id);
			return await session.Start(request.Title, HttpContext.User.GetDomainUser());
        }

		[HttpPost]
		[Route("{id:guid}/stories")]
		public async Task<Views.SessionView> AddStory(Guid id, Requests.CreateStory request)
		{
			var cards = string.IsNullOrEmpty(request.CardsId)
				? request.CustomCards
				: _cardsTypeProvider.GetCardsByTypeId(request.CardsId);

			var session = _silo.GetGrain<ISessionGrain>(id);
            return await session.AddStory(HttpContext.User.GetDomainUser(), request.Title, cards);
        }

        [HttpPost]
        [Route("{id:guid}/join")]
        public async Task<Views.SessionView> Join(Guid id)
        {
            var session = _silo.GetGrain<ISessionGrain>(id);
            return await session.AddParticipant(HttpContext.User.GetDomainUser());
        }

        [HttpPost]
        [Route("{id:guid}/leave")]
        public async Task<Views.SessionView> Leave(Guid id)
        {
            var session = _silo.GetGrain<ISessionGrain>(id);
            return await session.RemoveParticipant(HttpContext.User.GetDomainUser().Id);
        }

        [HttpPost]
        [Route("{id:guid}/activestory/{storyId:guid}")]
        public async Task<Views.SessionView> SetActiveStory(Guid id, Guid storyId)
        {
            var session = _silo.GetGrain<ISessionGrain>(id);
            return await session.SetActiveStory(HttpContext.User.GetDomainUser(), storyId, DateTime.UtcNow);
        }

        [HttpGet]
        [Route("cards_types")]
        public IEnumerable<Views.CardsType> GetCardsTypes() =>
	        _cardsTypeProvider.CardsTypes.Select(ct => new Views.CardsType(ct.Id, ct.Caption));

        [HttpPost]
        [Route("{id:guid}/groups")]
        public async Task<Views.SessionView> AddGroup(Guid id, Requests.CreateGroup request)
        {
            var session = _silo.GetGrain<ISessionGrain>(id);
            return await session.AddGroup(HttpContext.User.GetDomainUser(), new Group(Guid.NewGuid(), request.Name));
        }

        [HttpDelete]
        [Route("{id:guid}/groups/{groupId:guid}")]
        public async Task<Views.SessionView> RemoveGroup(Guid id, Guid groupId)
        {
            var session = _silo.GetGrain<ISessionGrain>(id);
            return await session.RemoveGroup(HttpContext.User.GetDomainUser(), groupId);
        }

        [HttpPost]
        [Route("{id:guid}/groups/{groupId:guid}/participant")]
        public async Task<Views.SessionView> MoveParticipantToGroup(Guid id, Guid groupId, Requests.MoveParticipantToGroup request)
        {
            var session = _silo.GetGrain<ISessionGrain>(id);
            return await session.MoveParticipantToGroup(HttpContext.User.GetDomainUser(), request.ParticipantId,  groupId);
        }

	}
}
