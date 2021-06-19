using System;
using System.Linq;
using System.Security.Claims;
using PlanningPoker.Domain;

namespace WebApi.Application
{
    public static class ClaimsPrincipalExtensions
    {
        public static CommonTypes.User GetDomainUser(this ClaimsPrincipal claimsPrincipal)
        {
            var id = claimsPrincipal.FindFirst(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? "";
            var name = claimsPrincipal.FindFirst(c => c.Type == ClaimTypes.GivenName)?.Value ?? "";
            var picture = claimsPrincipal.FindFirst("picture")?.Value ?? "";
            return new CommonTypes.User(Guid.Parse(id), name, picture);
        }
    }
}