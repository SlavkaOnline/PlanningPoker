using System;
using System.Linq;
using System.Security.Claims;

namespace WebApi.Application
{
    public static class ClaimsPrincipalExtensions
    {
        public static (Guid id, string name) GetUserParams(this ClaimsPrincipal claimsPrincipal)
        {
            var id = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var name = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value;
            return (Guid.Parse(id), name);
        }
    }
}