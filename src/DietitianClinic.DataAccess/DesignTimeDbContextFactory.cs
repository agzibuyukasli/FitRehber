using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace DietitianClinic.DataAccess.Context
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DietitianClinicDbContext>
    {
        public DietitianClinicDbContext CreateDbContext(string[] args)
        {
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
