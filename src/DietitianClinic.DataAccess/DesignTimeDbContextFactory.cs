using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace DietitianClinic.DataAccess.Context
{
    /// <summary>
    /// Design-time factory for creating the DbContext used by EF Core tools (migrations, database update, etc.).
    /// This is needed because the tools can't create the DbContext from the application DI container.
    /// </summary>
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DietitianClinicDbContext>
    {
        public DietitianClinicDbContext CreateDbContext(string[] args)
        {
            // Build config the same way the app does
            var basePath = Directory.GetCurrentDirectory();
            var builder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            var configuration = builder.Build();
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            var optionsBuilder = new DbContextOptionsBuilder<DietitianClinicDbContext>()
                .UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly("DietitianClinic.DataAccess");
                });

            return new DietitianClinicDbContext(optionsBuilder.Options);
        }
    }
}
