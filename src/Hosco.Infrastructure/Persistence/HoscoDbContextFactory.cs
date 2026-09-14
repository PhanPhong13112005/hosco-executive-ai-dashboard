using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Hosco.Infrastructure.Persistence;

public sealed class HoscoDbContextFactory : IDesignTimeDbContextFactory<HoscoDbContext>
{
    public HoscoDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__HoscoDb")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=HoscoDev;Trusted_Connection=True;TrustServerCertificate=True";
        return new HoscoDbContext(new DbContextOptionsBuilder<HoscoDbContext>().UseSqlServer(connection).Options);
    }
}
