using System.Reflection;
using LinqToDB.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace Databases;

public static class DataBasesExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services ,IConfiguration configuration)
    {
        return services.AddDbContext<DataBaseContext>(
            options => options.UseNpgsql(configuration.GetConnectionString("default")));
    }

    public static IApplicationBuilder CreateDataBaseIfNotExist(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataBaseContext>();
        var result = context.Database.EnsureCreated();
        
        if (result)
        {
            using var fileProvider =
                new PhysicalFileProvider(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            var sql = File.ReadAllText(fileProvider.GetFileInfo("orleans.sql").PhysicalPath);
            using var tran = context.Database.BeginTransaction();
            context.Database.ExecuteSqlRaw(sql);
            tran.Commit();
        }

        return app;
    }
}


public class DataBaseContext : DbContext
{

    public DataBaseContext(DbContextOptions<DataBaseContext> options) : base(options)
    {
        LinqToDBForEFTools.Initialize();
    }
    
}