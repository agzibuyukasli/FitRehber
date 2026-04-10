using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using DietitianClinic.DataAccess.Context;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

class Program {
    static void Main() {
        try {
            var optionsBuilder = new DbContextOptionsBuilder<DietitianClinicDbContext>();
            optionsBuilder.UseSqlServer(""Server=localhost\\SQLEXPRESS;Database=DietitianClinicDB_Dev;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"");
            var ctx = new DietitianClinicDbContext(optionsBuilder.Options);
            Console.WriteLine(""Cannot connect: "" + !ctx.Database.CanConnect());
            Console.WriteLine(""Messages count: "" + ctx.Messages.Count());
        } catch (Exception ex) {
            Console.WriteLine(""ERROR: "" + ex.ToString());
        }
    }
}
