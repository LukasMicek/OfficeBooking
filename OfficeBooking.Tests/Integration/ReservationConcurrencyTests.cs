using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OfficeBooking.Models;
using OfficeBooking.Services;

namespace OfficeBooking.Tests.Integration;

public class ReservationConcurrencyTests : IDisposable
{
    private readonly TestDbFactory _dbFactory;
    private readonly FakeTimeProvider _timeProvider;
    private readonly DateTime _now;

    public ReservationConcurrencyTests()
    {
        _dbFactory = new TestDbFactory();
        _now = new DateTime(2026, 1, 15, 9, 0, 0);
        _timeProvider = new FakeTimeProvider(_now);
    }

    public void Dispose() => _dbFactory.Dispose();

    [Fact]
    public async Task CreateAsync_WhenTwoRequestsOverlap_OnlyOneSucceeds()
    {
        // Arrange
        using var seedContext = _dbFactory.CreateContext();
        var room = new Room { Name = "Shared Room", Capacity = 10 };
        seedContext.Rooms.Add(room);
        await seedContext.SaveChangesAsync();
        var roomId = room.Id;

        // Shared lock provider: simulates a single app instance
        var lockProvider = new RoomLockProvider();

        var start = _now.AddDays(1).Date.AddHours(10);
        var end = _now.AddDays(1).Date.AddHours(11);

        var request1 = new CreateReservationRequest(
            RoomId: roomId,
            UserId: "user-A",
            Title: "Meeting A",
            Notes: null,
            AttendeesCount: 3,
            Start: start,
            End: end
        );

        var request2 = new CreateReservationRequest(
            RoomId: roomId,
            UserId: "user-B",
            Title: "Meeting B",
            Notes: null,
            AttendeesCount: 3,
            Start: start,
            End: end
        );

        // Act: two concurrent CreateAsync calls with separate DbContexts (like separate HTTP requests)
        var task1 = Task.Run(async () =>
        {
            using var context = _dbFactory.CreateContext();
            var service = new ReservationService(context, _timeProvider, lockProvider);
            return await service.CreateAsync(request1);
        });

        var task2 = Task.Run(async () =>
        {
            using var context = _dbFactory.CreateContext();
            var service = new ReservationService(context, _timeProvider, lockProvider);
            return await service.CreateAsync(request2);
        });

        var results = await Task.WhenAll(task1, task2);

        // Assert: exactly one succeeded and one failed with conflict
        var successes = results.Count(r => r.Success);
        var failures = results.Count(r => !r.Success);

        successes.Should().Be(1);
        failures.Should().Be(1);

        var failure = results.First(r => !r.Success);
        failure.Error.Should().Contain("Room is already booked");

        // Database contains exactly one reservation for this room/time
        using var verifyContext = _dbFactory.CreateContext();
        var reservations = await verifyContext.Reservations
            .Where(r => r.RoomId == roomId && r.Start == start && r.End == end)
            .ToListAsync();

        reservations.Should().HaveCount(1);
    }
}
