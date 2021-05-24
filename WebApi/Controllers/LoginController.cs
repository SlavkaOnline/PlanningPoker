using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Application;

namespace WebApi.Controllers
{
    [AllowAnonymous]
    [Route("api/[controller]")]
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
            var token = _jwtTokenProvider.CreateToken(id, request.Name);
            return new AuthUserModel()
            {
                Id = id.ToString(),
                Name = request.Name,
                Token = token
            };
        }
    }
}