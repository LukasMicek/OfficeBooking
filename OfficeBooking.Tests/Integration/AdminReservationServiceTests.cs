using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using OfficeBooking.Models;
using OfficeBooking.Services;

namespace OfficeBooking.Tests.Integration;

public class AdminReservationServiceTests : IDisposable
{
    private readonly TestDbFactory _dbFactory;

    public AdminReservationServiceTests()
    {
        _dbFactory = new TestDbFactory();
    }

    public void Dispose()
    {
        _dbFactory.Dispose();
    }

    private async Task<(Room room, IdentityUser user)> SeedRoomAndUserAsync()
    {
        using var context = _dbFactory.CreateContext();

        var user = new IdentityUser
        {
            Id = "user-123",
            UserName = "test@example.com",
            Email = "test@example.com",
            NormalizedEmail = "TEST@EXAMPLE.COM"
        };
        context.Users.Add(user);

        var room = new Room { Name = "Conference Room", Capacity = 10 };
        context.Rooms.Add(room);

        await context.SaveChangesAsync();
        return (room, user);
    }

    [Fact]
    public async Task GetListAsync_ReturnsAllReservations()
    {
        // Arrange
        var (room, user) = await SeedRoomAndUserAsync();

        using var context = _dbFactory.CreateContext();
        context.Reservations.AddRange(
            new Reservation
            {
                RoomId = room.Id,
                UserId = user.Id,
                Title = "Meeting 1",
                AttendeesCount = 5,
                Start = DateTime.Now.AddDays(1),
                End = DateTime.Now.AddDays(1).AddHours(1),
                IsCancelled = false
            },
            new Reservation
            {
                RoomId = room.Id,
                UserId = user.Id,
                Title = "Meeting 2",
                AttendeesCount = 3,
                Start = DateTime.Now.AddDays(2),
                End = DateTime.Now.AddDays(2).AddHours(1),
                IsCancelled = true,
                CancelledAt = DateTime.Now,
                CancelReason = "Changed plans"
            }
        );
        await context.SaveChangesAsync();

        var service = new AdminReservationService(context);

        // Act
        var result = await service.GetListAsync(activeOnly: false);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetListAsync_WithActiveOnly_ReturnsOnlyNotCancelled()
    {
        // Arrange
        var (room, user) = await SeedRoomAndUserAsync();

        using var context = _dbFactory.CreateContext();
        context.Reservations.AddRange(
            new Reservation
            {
                RoomId = room.Id,
                UserId = user.Id,
                Title = "Active Meeting",
                AttendeesCount = 5,
                Start = DateTime.Now.AddDays(1),
                End = DateTime.Now.AddDays(1).AddHours(1),
                IsCancelled = false
            },
            new Reservation
            {
                RoomId = room.Id,
                UserId = user.Id,
                Title = "Cancelled Meeting",
                AttendeesCount = 3,
                Start = DateTime.Now.AddDays(2),
                End = DateTime.Now.AddDays(2).AddHours(1),
                IsCancelled = true
            }
        );
        await context.SaveChangesAsync();

        var service = new AdminReservationService(context);

        // Act
        var result = await service.GetListAsync(activeOnly: true);

        // Assert
        result.Should().HaveCount(1);
        result.First().Title.Should().Be("Active Meeting");
        result.First().IsCancelled.Should().BeFalse();
    }

    [Fact]
    public async Task GetCancelViewModelAsync_WhenExists_ReturnsViewModel()
    {
        // Arrange
        var (room, user) = await SeedRoomAndUserAsync();

        using var context = _dbFactory.CreateContext();
        var reservation = new Reservation
        {
            RoomId = room.Id,
            UserId = user.Id,
            Title = "Test Meeting",
            AttendeesCount = 5,
            Start = DateTime.Now.AddDays(1),
            End = DateTime.Now.AddDays(1).AddHours(1),
            IsCancelled = false
        };
        context.Reservations.Add(reservation);
        await context.SaveChangesAsync();

        var service = new AdminReservationService(context);

        // Act
        var result = await service.GetCancelViewModelAsync(reservation.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(reservation.Id);
        result.Title.Should().Be("Test Meeting");
        result.RoomName.Should().Be("Conference Room");
        result.UserEmail.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetCancelViewModelAsync_WhenNotExists_ReturnsNull()
    {
        // Arrange
        using var context = _dbFactory.CreateContext();
        var service = new AdminReservationService(context);

        // Act
        var result = await service.GetCancelViewModelAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCancelViewModelAsync_WhenAlreadyCancelled_ReturnsNull()
    {
        // Arrange
        var (room, user) = await SeedRoomAndUserAsync();

        using var context = _dbFactory.CreateContext();
        var reservation = new Reservation
        {
            RoomId = room.Id,
            UserId = user.Id,
            Title = "Cancelled Meeting",
            AttendeesCount = 5,
            Start = DateTime.Now.AddDays(1),
            End = DateTime.Now.AddDays(1).AddHours(1),
            IsCancelled = true,
            CancelledAt = DateTime.Now,
            CancelReason = "Already cancelled"
        };
        context.Reservations.Add(reservation);
        await context.SaveChangesAsync();

        var service = new AdminReservationService(context);

        // Act
        var result = await service.GetCancelViewModelAsync(reservation.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CancelAsync_WhenValid_SetsCancelledFieldsAndReturnsSuccess()
    {
        // Arrange
        var (room, user) = await SeedRoomAndUserAsync();

        using var context = _dbFactory.CreateContext();
        var reservation = new Reservation
        {
            RoomId = room.Id,
            UserId = user.Id,
            Title = "Test Meeting",
            AttendeesCount = 5,
            Start = DateTime.Now.AddDays(1),
            End = DateTime.Now.AddDays(1).AddHours(1),
            IsCancelled = false
        };
        context.Reservations.Add(reservation);
        await context.SaveChangesAsync();
        var id = reservation.Id;

        var service = new AdminReservationService(context);
        var now = new DateTime(2026, 1, 15, 14, 30, 0);

        // Act
        var result = await service.CancelAsync(id, "Meeting cancelled by admin", now);

        // Assert
        result.Status.Should().Be(AdminCancelStatus.Success);

        // Verify persisted
        var updated = await context.Reservations.FindAsync(id);
        updated!.IsCancelled.Should().BeTrue();
        updated.CancelledAt.Should().Be(now);
        updated.CancelReason.Should().Be("Meeting cancelled by admin");
    }

    [Fact]
    public async Task CancelAsync_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        using var context = _dbFactory.CreateContext();
        var service = new AdminReservationService(context);

        // Act
        var result = await service.CancelAsync(999, "Some reason", DateTime.Now);

        // Assert
        result.Status.Should().Be(AdminCancelStatus.NotFound);
    }

    [Fact]
    public async Task CancelAsync_WhenAlreadyCancelled_ReturnsAlreadyCancelled()
    {
        // Arrange
        var (room, user) = await SeedRoomAndUserAsync();

        using var context = _dbFactory.CreateContext();
        var reservation = new Reservation
        {
            RoomId = room.Id,
            UserId = user.Id,
            Title = "Already Cancelled",
            AttendeesCount = 5,
            Start = DateTime.Now.AddDays(1),
            End = DateTime.Now.AddDays(1).AddHours(1),
            IsCancelled = true,
            CancelledAt = DateTime.Now.AddDays(-1),
            CancelReason = "Previous cancellation"
        };
        context.Reservations.Add(reservation);
        await context.SaveChangesAsync();

        var service = new AdminReservationService(context);

        // Act
        var result = await service.CancelAsync(reservation.Id, "Try again", DateTime.Now);

        // Assert
        result.Status.Should().Be(AdminCancelStatus.AlreadyCancelled);
    }
}
