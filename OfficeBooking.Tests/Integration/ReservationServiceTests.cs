using FluentAssertions;
using OfficeBooking.Models;
using OfficeBooking.Services;

namespace OfficeBooking.Tests.Integration;

public class ReservationServiceTests : IDisposable
{
    private readonly TestDbFactory _dbFactory;

    public ReservationServiceTests()
    {
        _dbFactory = new TestDbFactory();
    }

    public void Dispose()
    {
        _dbFactory.Dispose();
    }

    [Fact]
    public async Task CreateAsync_WithValidData_SavesAndReturnsReservation()
    {
        // Arrange
        using var context = _dbFactory.CreateContext();
        var room = new Room { Name = "Conference Room A", Capacity = 10 };
        context.Rooms.Add(room);
        await context.SaveChangesAsync();

        var service = new ReservationService(context);
        var request = new CreateReservationRequest(
            RoomId: room.Id,
            UserId: "user-123",
            Title: "Team Meeting",
            Notes: "Weekly sync",
            AttendeesCount: 5,
            Start: DateTime.Now.AddDays(1).Date.AddHours(10),
            End: DateTime.Now.AddDays(1).Date.AddHours(11)
        );

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Reservation.Should().NotBeNull();
        result.Reservation!.Title.Should().Be("Team Meeting");
        result.Reservation.RoomId.Should().Be(room.Id);
        result.Reservation.Id.Should().BeGreaterThan(0);

        // Verify persisted
        var saved = await service.GetByIdAsync(result.Reservation.Id);
        saved.Should().NotBeNull();
        saved!.Title.Should().Be("Team Meeting");
    }

    [Fact]
    public async Task CreateAsync_WhenExceedingRoomCapacity_ReturnsFailure()
    {
        // Arrange
        using var context = _dbFactory.CreateContext();
        var room = new Room { Name = "Small Room", Capacity = 5 };
        context.Rooms.Add(room);
        await context.SaveChangesAsync();

        var service = new ReservationService(context);
        var request = new CreateReservationRequest(
            RoomId: room.Id,
            UserId: "user-123",
            Title: "Big Meeting",
            Notes: null,
            AttendeesCount: 10,
            Start: DateTime.Now.AddDays(1).Date.AddHours(10),
            End: DateTime.Now.AddDays(1).Date.AddHours(11)
        );

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("5");
        result.Reservation.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_WhenOverlappingReservation_ReturnsFailure()
    {
        // Arrange
        using var context = _dbFactory.CreateContext();
        var room = new Room { Name = "Conference Room B", Capacity = 10 };
        context.Rooms.Add(room);
        await context.SaveChangesAsync();

        var existingReservation = new Reservation
        {
            RoomId = room.Id,
            UserId = "user-456",
            Title = "Existing Meeting",
            AttendeesCount = 3,
            Start = DateTime.Now.AddDays(1).Date.AddHours(10),
            End = DateTime.Now.AddDays(1).Date.AddHours(11)
        };
        context.Reservations.Add(existingReservation);
        await context.SaveChangesAsync();

        var service = new ReservationService(context);
        var request = new CreateReservationRequest(
            RoomId: room.Id,
            UserId: "user-123",
            Title: "Overlapping Meeting",
            Notes: null,
            AttendeesCount: 5,
            Start: DateTime.Now.AddDays(1).Date.AddHours(10).AddMinutes(30),
            End: DateTime.Now.AddDays(1).Date.AddHours(11).AddMinutes(30)
        );

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
        result.Reservation.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_WhenRoomDoesNotExist_ReturnsFailure()
    {
        // Arrange
        using var context = _dbFactory.CreateContext();
        var service = new ReservationService(context);
        var request = new CreateReservationRequest(
            RoomId: 999,
            UserId: "user-123",
            Title: "Meeting",
            Notes: null,
            AttendeesCount: 5,
            Start: DateTime.Now.AddDays(1).Date.AddHours(10),
            End: DateTime.Now.AddDays(1).Date.AddHours(11)
        );

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateAsync_WhenCancelledReservationExists_AllowsOverlap()
    {
        // Arrange
        using var context = _dbFactory.CreateContext();
        var room = new Room { Name = "Conference Room C", Capacity = 10 };
        context.Rooms.Add(room);
        await context.SaveChangesAsync();

        var cancelledReservation = new Reservation
        {
            RoomId = room.Id,
            UserId = "user-456",
            Title = "Cancelled Meeting",
            AttendeesCount = 3,
            Start = DateTime.Now.AddDays(1).Date.AddHours(10),
            End = DateTime.Now.AddDays(1).Date.AddHours(11),
            IsCancelled = true,
            CancelledAt = DateTime.Now
        };
        context.Reservations.Add(cancelledReservation);
        await context.SaveChangesAsync();

        var service = new ReservationService(context);
        var request = new CreateReservationRequest(
            RoomId: room.Id,
            UserId: "user-123",
            Title: "New Meeting",
            Notes: null,
            AttendeesCount: 5,
            Start: DateTime.Now.AddDays(1).Date.AddHours(10),
            End: DateTime.Now.AddDays(1).Date.AddHours(11)
        );

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Reservation.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_WhenAdjacentReservation_AllowsBooking()
    {
        // Arrange
        using var context = _dbFactory.CreateContext();
        var room = new Room { Name = "Conference Room D", Capacity = 10 };
        context.Rooms.Add(room);
        await context.SaveChangesAsync();

        var existingReservation = new Reservation
        {
            RoomId = room.Id,
            UserId = "user-456",
            Title = "Morning Meeting",
            AttendeesCount = 3,
            Start = DateTime.Now.AddDays(1).Date.AddHours(9),
            End = DateTime.Now.AddDays(1).Date.AddHours(10)
        };
        context.Reservations.Add(existingReservation);
        await context.SaveChangesAsync();

        var service = new ReservationService(context);
        var request = new CreateReservationRequest(
            RoomId: room.Id,
            UserId: "user-123",
            Title: "Afternoon Meeting",
            Notes: null,
            AttendeesCount: 5,
            Start: DateTime.Now.AddDays(1).Date.AddHours(10),
            End: DateTime.Now.AddDays(1).Date.AddHours(11)
        );

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Reservation.Should().NotBeNull();
    }
}
