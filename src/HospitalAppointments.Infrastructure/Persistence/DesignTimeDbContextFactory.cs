using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HospitalAppointments.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Run EF commands from the solution root. No demo account creation at design time.
        var databasePath = Path.GetFullPath(Path.Combine("src", "HospitalAppointments.Web", "data", "clinic.db"));
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
        var connection = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath, ForeignKeys = true, DefaultTimeout = 5
        };
        return new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection.ToString()).Options);
    }
}
