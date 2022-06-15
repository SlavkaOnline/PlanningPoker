using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Databases.Models;

public class Account : IdentityUser<Guid>
{
    public string Name { get; set; }
    
    public Account(Guid id, string userName, string email, string name)
    {
        Id = id;
        UserName = userName;
        Email = email;
        Name = name;
    }
    
    public class Config: IEntityTypeConfiguration<Account>
    {
        public void Configure(EntityTypeBuilder<Account> builder)
        {
            builder.Property(x => x.Name)
                .HasMaxLength(256);
        }
    }
}