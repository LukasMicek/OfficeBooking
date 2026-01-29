using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OfficeBooking.Data;

namespace OfficeBooking.Tests.Integration;

public class TestDbFactory : IDisposable
{
    private readonly SqliteConnection _connection;

    public TestDbFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    public ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();

        return context;
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
