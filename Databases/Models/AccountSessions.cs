using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Databases.Models;

public class AccountSession
{
    public Guid Id { get; set; }

    public Guid AccountId { get; set; }

    public Guid SessionId { get; set; }

    public string SessionName { get; set; }

    public string SessionType { get; set; } = "poker";
    public DateTime CreatedUtc { get; set; }


    public AccountSession(
        Guid id,
        Guid accountId,
        Guid sessionId,
        string sessionName,
        DateTime createdUtc)
    {
        Id = id;
        AccountId = accountId;
        SessionId = sessionId;
        SessionName = sessionName;
        CreatedUtc = createdUtc;
    }
    
    private class Config : IEntityTypeConfiguration<AccountSession>
    {
        public void Configure(EntityTypeBuilder<AccountSession> builder)
        {
            builder.ToTable("account_session");
            builder.Property(x => x.SessionName).HasMaxLength(256);
            builder.Property(x => x.SessionType).HasMaxLength(64);
            builder.HasIndex(x => new {x.AccountId, x.CreatedUtc});
        }
    }

}