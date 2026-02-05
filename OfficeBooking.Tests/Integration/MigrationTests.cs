using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OfficeBooking.Data;

namespace OfficeBooking.Tests.Integration;

public class MigrationTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public MigrationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public void AllMigrations_ApplyCleanly()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        // Act
        using var context = new ApplicationDbContext(options);
        var applyAction = () => context.Database.Migrate();

        // Assert
        applyAction.Should().NotThrow();
    }

    [Fact]
    public void Indexes_ExistAfterMigration()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new ApplicationDbContext(options);
        context.Database.Migrate();

        // Act
        var indexes = context.Database
            .SqlQueryRaw<string>("SELECT name FROM sqlite_master WHERE type = 'index' AND name NOT LIKE 'sqlite_%'")
            .ToList();

        // Assert
        indexes.Should().Contain("IX_Reservations_RoomId_IsCancelled_Start_End");
        indexes.Should().Contain("IX_RoomEquipments_EquipmentId");
        indexes.Should().Contain("IX_Rooms_Name");
    }
}
