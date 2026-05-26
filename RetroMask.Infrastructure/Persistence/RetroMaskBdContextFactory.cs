using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace RetroMask.Infrastructure.Persistence;

public class RetroMaskBdContextFactory : IDesignTimeDbContextFactory<RetroMaskDbContext>
{
    public RetroMaskDbContext CreateDbContext(string[] args)
    {
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "../RetroMask.API");
        
        var configure = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json")
            .Build();

        var optionBuilder = new DbContextOptionsBuilder<RetroMaskDbContext>();
        optionBuilder.UseSqlServer(
            configure.GetConnectionString("DefaultConnection"),
            sql => sql.MigrationsAssembly(typeof(RetroMaskDbContext).Assembly.FullName)
        );

        return new RetroMaskDbContext(optionBuilder.Options);
    }
}