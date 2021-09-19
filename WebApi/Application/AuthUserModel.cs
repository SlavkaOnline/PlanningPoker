using System;

namespace WebApi.Application
{
    public class AuthUserModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Token { get; set; }
        public string Picture { get; set; }
    }
}