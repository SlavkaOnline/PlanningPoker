using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Application;

namespace WebApi.Controllers
{
    [AllowAnonymous]
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly JwtTokenProvider _jwtTokenProvider;

        public LoginController(JwtTokenProvider jwtTokenProvider)
        {
            _jwtTokenProvider = jwtTokenProvider;
        }

        [HttpPost]
        public AuthUserModel Login(AuthUserRequest request)
        {
            var id = Guid.NewGuid();
            var token = _jwtTokenProvider.CreateToken(id, request.Name, "");
            return new AuthUserModel()
            {
                Id = id.ToString(),
                Name = request.Name,
                Token = token,
                Picture = ""
            };
        }

        [Route("google-login")]
        [HttpGet]
        public ActionResult GoogleLogin([FromQuery] string returnUrl)
        {
            var properties = new AuthenticationProperties
                {RedirectUri = Url.Action(nameof(GoogleResponse), "Login", new {returnUrl})};
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [Route("google-response")]
        [HttpGet]
        public async Task<ActionResult<AuthUserModel>> GoogleResponse(string returnUrl)
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!result.Succeeded) return BadRequest();
            var name = (result.Ticket?.Principal.FindFirst(ClaimTypes.GivenName)?.Value ?? "")
                       + " "
                       + (result.Ticket?.Principal.FindFirst(ClaimTypes.Surname)?.Value ?? "");
            var picture = result.Ticket?.Principal.FindFirst("picture")?.Value ?? "";
            var id = Guid.NewGuid();
            var token = _jwtTokenProvider.CreateToken(id, name, picture);
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return new RedirectResult($"{returnUrl}?access_token={token}");
        }
    }
}