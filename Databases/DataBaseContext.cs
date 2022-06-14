using System.Reflection;
using Databases.Models;
using LinqToDB.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace Databases;

public static class DataBasesExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddDbContext<DataBaseContext>(
            options => options.UseNpgsql(configuration.GetConnectionString("default")));
    }

    public static IApplicationBuilder CreateDataBaseIfNotExist(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataBaseContext>();

        context.Database.Migrate();
        using var fileProvider =
            new PhysicalFileProvider(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
        var sql = File.ReadAllText(fileProvider.GetFileInfo("orleans.sql").PhysicalPath);
        using var tran = context.Database.BeginTransaction();
        context.Database.ExecuteSqlRaw(sql);
        tran.Commit();


        return app;
    }
}

public class DataBaseContext : IdentityDbContext<Account, IdentityRole<Guid>, Guid>
{
    public DataBaseContext(DbContextOptions<DataBaseContext> options) : base(options)
    {
        LinqToDBForEFTools.Initialize();
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("Identity");
        builder.Entity<Account>(entity => { entity.ToTable(name: "user"); });
        builder.Entity<IdentityRole<Guid>>(entity => { entity.ToTable(name: "role"); });
        builder.Entity<IdentityUserRole<Guid>>(entity => { entity.ToTable("user_roles"); });
        builder.Entity<IdentityUserClaim<Guid>>(entity => { entity.ToTable("user_claims"); });
        builder.Entity<IdentityUserLogin<Guid>>(entity => { entity.ToTable("User_logins"); });
        builder.Entity<IdentityRoleClaim<Guid>>(entity => { entity.ToTable("role_claims"); });
        builder.Entity<IdentityUserToken<Guid>>(entity => { entity.ToTable("user_tokens"); });
    }
}