using Microsoft.AspNetCore.Identity;

namespace Databases.Models;

public class Account : IdentityUser<Guid>
{
    public Account(Guid id, string userName, string email)
    {
        Id = id;
        UserName = userName;
        Email = email;
    }
}