using FluentAssertions;
using OfficeBooking.Models;
using OfficeBooking.Services;

namespace OfficeBooking.Tests.Integration;

public class RoomUsageReportServiceTests : IDisposable
{
    private readonly TestDbFactory _dbFactory;

    public RoomUsageReportServiceTests()
    {
        _dbFactory = new TestDbFactory();
    }

    public void Dispose()
    {
        _dbFactory.Dispose();
    }

    [Fact]
    public async Task GetReportAsync_WhenYearMonthNull_UsesTodayValues()
    {
        // Arrange
        using var context = _dbFactory.CreateContext();
        var service = new RoomUsageReportService(context);
        var today = new DateTime(2026, 3, 15);

        // Act
        var result = await service.GetReportAsync(null, null, today);

        // Assert
        result.Year.Should().Be(2026);
        result.Month.Should().Be(3);
    }

    [Fact]
    public async Task GetReportAsync_WhenYearMonthProvided_UsesProvidedValues()
    {
        // Arrange
        using var context = _dbFactory.CreateContext();
        var service = new RoomUsageReportService(context);
        var today = new DateTime(2026, 3, 15);

        // Act
        var result = await service.GetReportAsync(2025, 6, today);

        // Assert
        result.Year.Should().Be(2025);
        result.Month.Should().Be(6);
    }

    [Fact]
    public async Task GetReportAsync_IgnoresCancelledReservations()
    {
        // Arrange
        using var context = _dbFactory.CreateContext();
        var room = new Room { Name = "Room A", Capacity = 10 };
        context.Rooms.Add(room);
        await context.SaveChangesAsync();

        context.Reservations.AddRange(
            new Reservation
            {
                RoomId = room.Id,
                UserId = "user-1",
                Title = "Active",
                AttendeesCount = 5,
                Start = new DateTime(2026, 3, 10, 10, 0, 0),
                End = new DateTime(2026, 3, 10, 11, 0, 0),
                IsCancelled = false
            },
            new Reservation
            {
                RoomId = room.Id,
                UserId = "user-1",
                Title = "Cancelled",
                AttendeesCount = 5,
                Start = new DateTime(2026, 3, 10, 14, 0, 0),
                End = new DateTime(2026, 3, 10, 16, 0, 0),
                IsCancelled = true
            }
        );
        await context.SaveChangesAsync();

        var service = new RoomUsageReportService(context);

        // Act
        var result = await service.GetReportAsync(2026, 3, DateTime.Today);

        // Assert
        result.Rows.Should().HaveCount(1);
        result.Rows[0].TotalMinutes.Should().Be(60); // Only 1 hour from active
    }

    [Fact]
    public async Task GetReportAsync_IncludesReservationsOverlappingMonthBoundaries()
    {
        // Arrange
        using var context = _dbFactory.CreateContext();
        var room = new Room { Name = "Room B", Capacity = 10 };
        context.Rooms.Add(room);
        await context.SaveChangesAsync();

        context.Reservations.AddRange(
            // Starts in Feb, ends in March (should be included for March report)
            new Reservation
            {
                RoomId = room.Id,
                UserId = "user-1",
                Title = "Cross Feb-Mar",
                AttendeesCount = 5,
                Start = new DateTime(2026, 2, 28, 23, 0, 0),
                End = new DateTime(2026, 3, 1, 1, 0, 0),
                IsCancelled = false
            },
            // Starts in March, ends in April (should be included for March report)
            new Reservation
            {
                RoomId = room.Id,
                UserId = "user-1",
                Title = "Cross Mar-Apr",
                AttendeesCount = 5,
                Start = new DateTime(2026, 3, 31, 23, 0, 0),
                End = new DateTime(2026, 4, 1, 1, 0, 0),
                IsCancelled = false
            },
            // Completely in March
            new Reservation
            {
                RoomId = room.Id,
                UserId = "user-1",
                Title = "In March",
                AttendeesCount = 5,
                Start = new DateTime(2026, 3, 15, 10, 0, 0),
                End = new DateTime(2026, 3, 15, 11, 0, 0),
                IsCancelled = false
            }
        );
        await context.SaveChangesAsync();

        var service = new RoomUsageReportService(context);

        // Act
        var result = await service.GetReportAsync(2026, 3, DateTime.Today);

        // Assert
        result.Rows.Should().HaveCount(1);
        // 2h (cross Feb-Mar) + 2h (cross Mar-Apr) + 1h (in March) = 5h = 300 min
        result.Rows[0].TotalMinutes.Should().Be(300);
    }

    [Fact]
    public async Task GetReportAsync_ExcludesReservationsOutsideMonth()
    {
        // Arrange
        using var context = _dbFactory.CreateContext();
        var room = new Room { Name = "Room C", Capacity = 10 };
        context.Rooms.Add(room);
        await context.SaveChangesAsync();

        context.Reservations.AddRange(
            // In February (before March)
            new Reservation
            {
                RoomId = room.Id,
                UserId = "user-1",
                Title = "Feb meeting",
                AttendeesCount = 5,
                Start = new DateTime(2026, 2, 15, 10, 0, 0),
                End = new DateTime(2026, 2, 15, 11, 0, 0),
                IsCancelled = false
            },
            // In April (after March)
            new Reservation
            {
                RoomId = room.Id,
                UserId = "user-1",
                Title = "Apr meeting",
                AttendeesCount = 5,
                Start = new DateTime(2026, 4, 15, 10, 0, 0),
                End = new DateTime(2026, 4, 15, 11, 0, 0),
                IsCancelled = false
            }
        );
        await context.SaveChangesAsync();

        var service = new RoomUsageReportService(context);

        // Act
        var result = await service.GetReportAsync(2026, 3, DateTime.Today);

        // Assert
        result.Rows.Should().BeEmpty();
    }

    [Fact]
    public async Task GetReportAsync_OrdersRowsByTotalMinutesDescending()
    {
        // Arrange
        using var context = _dbFactory.CreateContext();
        var roomA = new Room { Name = "Room Low", Capacity = 10 };
        var roomB = new Room { Name = "Room High", Capacity = 10 };
        var roomC = new Room { Name = "Room Mid", Capacity = 10 };
        context.Rooms.AddRange(roomA, roomB, roomC);
        await context.SaveChangesAsync();

        context.Reservations.AddRange(
            new Reservation
            {
                RoomId = roomA.Id,
                UserId = "user-1",
                Title = "Low usage",
                AttendeesCount = 5,
                Start = new DateTime(2026, 3, 10, 10, 0, 0),
                End = new DateTime(2026, 3, 10, 10, 30, 0), // 30 min
                IsCancelled = false
            },
            new Reservation
            {
                RoomId = roomB.Id,
                UserId = "user-1",
                Title = "High usage",
                AttendeesCount = 5,
                Start = new DateTime(2026, 3, 10, 10, 0, 0),
                End = new DateTime(2026, 3, 10, 12, 0, 0), // 120 min
                IsCancelled = false
            },
            new Reservation
            {
                RoomId = roomC.Id,
                UserId = "user-1",
                Title = "Mid usage",
                AttendeesCount = 5,
                Start = new DateTime(2026, 3, 10, 10, 0, 0),
                End = new DateTime(2026, 3, 10, 11, 0, 0), // 60 min
                IsCancelled = false
            }
        );
        await context.SaveChangesAsync();

        var service = new RoomUsageReportService(context);

        // Act
        var result = await service.GetReportAsync(2026, 3, DateTime.Today);

        // Assert
        result.Rows.Should().HaveCount(3);
        result.Rows[0].TotalMinutes.Should().Be(120);
        result.Rows[0].RoomName.Should().Be("Room High");
        result.Rows[1].TotalMinutes.Should().Be(60);
        result.Rows[1].RoomName.Should().Be("Room Mid");
        result.Rows[2].TotalMinutes.Should().Be(30);
        result.Rows[2].RoomName.Should().Be("Room Low");
    }

    [Fact]
    public async Task GetReportAsync_AggregatesMultipleReservationsPerRoom()
    {
        // Arrange
        using var context = _dbFactory.CreateContext();
        var room = new Room { Name = "Room D", Capacity = 10 };
        context.Rooms.Add(room);
        await context.SaveChangesAsync();

        context.Reservations.AddRange(
            new Reservation
            {
                RoomId = room.Id,
                UserId = "user-1",
                Title = "Meeting 1",
                AttendeesCount = 5,
                Start = new DateTime(2026, 3, 10, 10, 0, 0),
                End = new DateTime(2026, 3, 10, 11, 0, 0), // 60 min
                IsCancelled = false
            },
            new Reservation
            {
                RoomId = room.Id,
                UserId = "user-1",
                Title = "Meeting 2",
                AttendeesCount = 5,
                Start = new DateTime(2026, 3, 11, 14, 0, 0),
                End = new DateTime(2026, 3, 11, 15, 30, 0), // 90 min
                IsCancelled = false
            }
        );
        await context.SaveChangesAsync();

        var service = new RoomUsageReportService(context);

        // Act
        var result = await service.GetReportAsync(2026, 3, DateTime.Today);

        // Assert
        result.Rows.Should().HaveCount(1);
        result.Rows[0].TotalMinutes.Should().Be(150); // 60 + 90
    }
}
