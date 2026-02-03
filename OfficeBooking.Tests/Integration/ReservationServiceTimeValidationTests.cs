using FluentAssertions;
using OfficeBooking.Models;
using OfficeBooking.Services;

namespace OfficeBooking.Tests.Integration;

public class ReservationServiceTimeValidationTests : IDisposable
{
    private readonly TestDbFactory _dbFactory;
    private readonly FakeTimeProvider _timeProvider;
    private readonly DateTime _now;

    public ReservationServiceTimeValidationTests()
    {
        _dbFactory = new TestDbFactory();
        // Use a fixed "now" for deterministic tests
        _now = new DateTime(2026, 1, 15, 9, 0, 0);
        _timeProvider = new FakeTimeProvider(_now);
    }

    public void Dispose()
    {
        _dbFactory.Dispose();
    }

    #region CreateAsync Time Validation

    [Fact]
    public async Task CreateAsync_WhenEndEqualsStart_ReturnsFailure()
    {
        // Arrange
        using var context = _dbFactory.CreateContext();
        var room = new Room { Name = "Room A", Capacity = 10 };
        context.Rooms.Add(room);
        await context.SaveChangesAsync();

        var service = new ReservationService(context, _timeProvider);
        var sameTime = _now.AddDays(1).Date.AddHours(10);
        var request = new CreateReservationRequest(
            RoomId: room.Id,
            UserId: "user-123",
            Title: "Invalid Meeting",
            Notes: null,
            AttendeesCount: 5,
            Start: sameTime,
            End: sameTime
        );

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("zakończenia");
    }

    [Fact]
    public async Task CreateAsync_WhenEndBeforeStart_ReturnsFailure()
    {
        // Arrange
        using var context = _dbFactory.CreateContext();
        var room = new Room { Name = "Room B", Capacity = 10 };
        context.Rooms.Add(room);
        await context.SaveChangesAsync();

        var service = new ReservationService(context, _timeProvider);
        var request = new CreateReservationRequest(
            RoomId: room.Id,
            UserId: "user-123",
            Title: "Invalid Meeting",
            Notes: null,
            AttendeesCount: 5,
            Start: _now.AddDays(1).Date.AddHours(12),
            End: _now.AddDays(1).Date.AddHours(10)
        );

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("zakończenia");
    }

    [Fact]
    public async Task CreateAsync_WhenStartInPast_ReturnsFailure()
    {
        // Arrange
        using var context = _dbFactory.CreateContext();
        var room = new Room { Name = "Room C", Capacity = 10 };
        context.Rooms.Add(room);
        await context.SaveChangesAsync();

        var service = new ReservationService(context, _timeProvider);
        var request = new CreateReservationRequest(
            RoomId: room.Id,
            UserId: "user-123",
            Title: "Past Meeting",
            Notes: null,
            AttendeesCount: 5,
            Start: _now.AddDays(-1).Date.AddHours(10),
            End: _now.AddDays(-1).Date.AddHours(11)
        );

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("przeszłości");
    }

    [Fact]
    public async Task CreateAsync_WhenStartOutsideBusinessHours_ReturnsFailure()
    {
        // Arrange
        using var context = _dbFactory.CreateContext();
        var room = new Room { Name = "Room D", Capacity = 10 };
        context.Rooms.Add(room);
        await context.SaveChangesAsync();

        var service = new ReservationService(context, _timeProvider);
        var request = new CreateReservationRequest(
            RoomId: room.Id,
            UserId: "user-123",
            Title: "Early Meeting",
            Notes: null,
            AttendeesCount: 5,
            Start: _now.AddDays(1).Date.AddHours(6),
            End: _now.AddDays(1).Date.AddHours(7)
        );

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("godzinach pracy");
    }

    [Fact]
    public async Task CreateAsync_WhenDurationExceedsMax_ReturnsFailure()
    {
        // Arrange
        using var context = _dbFactory.CreateContext();
        var room = new Room { Name = "Room E", Capacity = 10 };
        context.Rooms.Add(room);
        await context.SaveChangesAsync();

        var service = new ReservationService(context, _timeProvider);
        var request = new CreateReservationRequest(
            RoomId: room.Id,
            UserId: "user-123",
            Title: "Long Meeting",
            Notes: null,
            AttendeesCount: 5,
            Start: _now.AddDays(1).Date.AddHours(8),
            End: _now.AddDays(1).Date.AddHours(18)
        );

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("8");
    }

    #endregion

    #region UpdateAsync Time Validation

    [Fact]
    public async Task UpdateAsync_WhenEndEqualsStart_ReturnsFailure()
    {
        // Arrange
        using var context = _dbFactory.CreateContext();
        var room = new Room { Name = "Room F", Capacity = 10 };
        context.Rooms.Add(room);
        await context.SaveChangesAsync();

        var reservation = new Reservation
        {
            RoomId = room.Id,
            UserId = "user-123",
            Title = "Original Meeting",
            AttendeesCount = 5,
            Start = _now.AddDays(2).Date.AddHours(10),
            End = _now.AddDays(2).Date.AddHours(11)
        };
        context.Reservations.Add(reservation);
        await context.SaveChangesAsync();

        var service = new ReservationService(context, _timeProvider);
        var sameTime = _now.AddDays(3).Date.AddHours(10);
        var updateRequest = new UpdateReservationRequest(
            Title: "Updated Meeting",
            Notes: null,
            AttendeesCount: 5,
            Start: sameTime,
            End: sameTime
        );

        // Act
        var result = await service.UpdateAsync(reservation.Id, "user-123", updateRequest);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("zakończenia");
    }

    [Fact]
    public async Task UpdateAsync_WhenEndBeforeStart_ReturnsFailure()
    {
        // Arrange
        using var context = _dbFactory.CreateContext();
        var room = new Room { Name = "Room G", Capacity = 10 };
        context.Rooms.Add(room);
        await context.SaveChangesAsync();

        var reservation = new Reservation
        {
            RoomId = room.Id,
            UserId = "user-123",
            Title = "Original Meeting",
            AttendeesCount = 5,
            Start = _now.AddDays(2).Date.AddHours(10),
            End = _now.AddDays(2).Date.AddHours(11)
        };
        context.Reservations.Add(reservation);
        await context.SaveChangesAsync();

        var service = new ReservationService(context, _timeProvider);
        var updateRequest = new UpdateReservationRequest(
            Title: "Updated Meeting",
            Notes: null,
            AttendeesCount: 5,
            Start: _now.AddDays(3).Date.AddHours(12),
            End: _now.AddDays(3).Date.AddHours(10)
        );

        // Act
        var result = await service.UpdateAsync(reservation.Id, "user-123", updateRequest);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("zakończenia");
    }

    [Fact]
    public async Task UpdateAsync_WhenNewStartInPast_ReturnsFailure()
    {
        // Arrange
        using var context = _dbFactory.CreateContext();
        var room = new Room { Name = "Room H", Capacity = 10 };
        context.Rooms.Add(room);
        await context.SaveChangesAsync();

        var reservation = new Reservation
        {
            RoomId = room.Id,
            UserId = "user-123",
            Title = "Original Meeting",
            AttendeesCount = 5,
            Start = _now.AddDays(2).Date.AddHours(10),
            End = _now.AddDays(2).Date.AddHours(11)
        };
        context.Reservations.Add(reservation);
        await context.SaveChangesAsync();

        var service = new ReservationService(context, _timeProvider);
        var updateRequest = new UpdateReservationRequest(
            Title: "Updated Meeting",
            Notes: null,
            AttendeesCount: 5,
            Start: _now.AddDays(-1).Date.AddHours(10),
            End: _now.AddDays(-1).Date.AddHours(11)
        );

        // Act
        var result = await service.UpdateAsync(reservation.Id, "user-123", updateRequest);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("przeszłości");
    }

    [Fact]
    public async Task UpdateAsync_WhenValidTimeRange_Succeeds()
    {
        // Arrange
        using var context = _dbFactory.CreateContext();
        var room = new Room { Name = "Room I", Capacity = 10 };
        context.Rooms.Add(room);
        await context.SaveChangesAsync();

        var reservation = new Reservation
        {
            RoomId = room.Id,
            UserId = "user-123",
            Title = "Original Meeting",
            AttendeesCount = 5,
            Start = _now.AddDays(2).Date.AddHours(10),
            End = _now.AddDays(2).Date.AddHours(11)
        };
        context.Reservations.Add(reservation);
        await context.SaveChangesAsync();

        var service = new ReservationService(context, _timeProvider);
        var updateRequest = new UpdateReservationRequest(
            Title: "Updated Meeting",
            Notes: "New notes",
            AttendeesCount: 6,
            Start: _now.AddDays(3).Date.AddHours(14),
            End: _now.AddDays(3).Date.AddHours(15)
        );

        // Act
        var result = await service.UpdateAsync(reservation.Id, "user-123", updateRequest);

        // Assert
        result.Success.Should().BeTrue();
        result.Reservation!.Title.Should().Be("Updated Meeting");
    }

    #endregion
}
