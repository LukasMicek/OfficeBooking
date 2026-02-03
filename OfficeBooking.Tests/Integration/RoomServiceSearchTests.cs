using FluentAssertions;
using OfficeBooking.Models;
using OfficeBooking.Services;

namespace OfficeBooking.Tests.Integration;

public class RoomServiceSearchTests : IDisposable
{
    private readonly TestDbFactory _dbFactory;
    private readonly DateTime _searchStart;
    private readonly DateTime _searchEnd;

    public RoomServiceSearchTests()
    {
        _dbFactory = new TestDbFactory();
        _searchStart = new DateTime(2026, 2, 10, 10, 0, 0);
        _searchEnd = new DateTime(2026, 2, 10, 11, 0, 0);
    }

    public void Dispose()
    {
        _dbFactory.Dispose();
    }

    #region Capacity Filter

    [Fact]
    public async Task SearchAvailableAsync_WhenRoomCapacityTooSmall_ExcludesRoom()
    {
        // Arrange
        using var context = _dbFactory.CreateContext();
        context.Rooms.Add(new Room { Name = "Small Room", Capacity = 2 });
        context.Rooms.Add(new Room { Name = "Big Room", Capacity = 10 });
        await context.SaveChangesAsync();

        var service = new RoomService(context);
        var request = new RoomSearchRequest(
            Start: _searchStart,
            End: _searchEnd,
            RequiredCapacity: 3,
            RequiredEquipmentIds: []
        );

        // Act
        var result = await service.SearchAvailableAsync(request);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Big Room");
    }

    [Fact]
    public async Task SearchAvailableAsync_WhenRoomCapacityExact_IncludesRoom()
    {
        // Arrange
        using var context = _dbFactory.CreateContext();
        context.Rooms.Add(new Room { Name = "Exact Room", Capacity = 5 });
        await context.SaveChangesAsync();

        var service = new RoomService(context);
        var request = new RoomSearchRequest(
            Start: _searchStart,
            End: _searchEnd,
            RequiredCapacity: 5,
            RequiredEquipmentIds: []
        );

        // Act
        var result = await service.SearchAvailableAsync(request);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Exact Room");
    }

    #endregion

    #region Equipment Filter

    [Fact]
    public async Task SearchAvailableAsync_WhenRoomMissingRequiredEquipment_ExcludesRoom()
    {
        // Arrange
        using var context = _dbFactory.CreateContext();
        var projector = new Equipment { Name = "Projector" };
        var whiteboard = new Equipment { Name = "Whiteboard" };
        context.Equipments.AddRange(projector, whiteboard);
        await context.SaveChangesAsync();

        var roomWithProjectorOnly = new Room { Name = "Room A", Capacity = 10 };
        var roomWithBoth = new Room { Name = "Room B", Capacity = 10 };
        context.Rooms.AddRange(roomWithProjectorOnly, roomWithBoth);
        await context.SaveChangesAsync();

        context.RoomEquipments.Add(new RoomEquipment { RoomId = roomWithProjectorOnly.Id, EquipmentId = projector.Id });
        context.RoomEquipments.Add(new RoomEquipment { RoomId = roomWithBoth.Id, EquipmentId = projector.Id });
        context.RoomEquipments.Add(new RoomEquipment { RoomId = roomWithBoth.Id, EquipmentId = whiteboard.Id });
        await context.SaveChangesAsync();

        var service = new RoomService(context);
        var request = new RoomSearchRequest(
            Start: _searchStart,
            End: _searchEnd,
            RequiredCapacity: 1,
            RequiredEquipmentIds: [projector.Id, whiteboard.Id]
        );

        // Act
        var result = await service.SearchAvailableAsync(request);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Room B");
    }

    [Fact]
    public async Task SearchAvailableAsync_WhenNoEquipmentRequired_IncludesAllRooms()
    {
        // Arrange
        using var context = _dbFactory.CreateContext();
        context.Rooms.Add(new Room { Name = "Room X", Capacity = 5 });
        context.Rooms.Add(new Room { Name = "Room Y", Capacity = 5 });
        await context.SaveChangesAsync();

        var service = new RoomService(context);
        var request = new RoomSearchRequest(
            Start: _searchStart,
            End: _searchEnd,
            RequiredCapacity: 1,
            RequiredEquipmentIds: []
        );

        // Act
        var result = await service.SearchAvailableAsync(request);

        // Assert
        result.Should().HaveCount(2);
        result.Select(r => r.Name).Should().BeEquivalentTo(["Room X", "Room Y"]);
    }

    #endregion

    #region Availability Filter (Overlap Logic)

    [Fact]
    public async Task SearchAvailableAsync_WhenActiveReservationOverlaps_ExcludesRoom()
    {
        // Arrange
        using var context = _dbFactory.CreateContext();
        var room = new Room { Name = "Busy Room", Capacity = 10 };
        context.Rooms.Add(room);
        await context.SaveChangesAsync();

        context.Reservations.Add(new Reservation
        {
            RoomId = room.Id,
            UserId = "user-1",
            Title = "Meeting",
            AttendeesCount = 5,
            Start = _searchStart.AddMinutes(-30),
            End = _searchEnd.AddMinutes(-30),
            IsCancelled = false
        });
        await context.SaveChangesAsync();

        var service = new RoomService(context);
        var request = new RoomSearchRequest(
            Start: _searchStart,
            End: _searchEnd,
            RequiredCapacity: 1,
            RequiredEquipmentIds: []
        );

        // Act
        var result = await service.SearchAvailableAsync(request);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAvailableAsync_WhenCancelledReservationOverlaps_IncludesRoom()
    {
        // Arrange
        using var context = _dbFactory.CreateContext();
        var room = new Room { Name = "Available Room", Capacity = 10 };
        context.Rooms.Add(room);
        await context.SaveChangesAsync();

        context.Reservations.Add(new Reservation
        {
            RoomId = room.Id,
            UserId = "user-1",
            Title = "Cancelled Meeting",
            AttendeesCount = 5,
            Start = _searchStart,
            End = _searchEnd,
            IsCancelled = true,
            CancelledAt = DateTime.Now.AddDays(-1)
        });
        await context.SaveChangesAsync();

        var service = new RoomService(context);
        var request = new RoomSearchRequest(
            Start: _searchStart,
            End: _searchEnd,
            RequiredCapacity: 1,
            RequiredEquipmentIds: []
        );

        // Act
        var result = await service.SearchAvailableAsync(request);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Available Room");
    }

    [Fact]
    public async Task SearchAvailableAsync_WhenAdjacentReservationEndEqualsStart_IncludesRoom()
    {
        // Arrange
        using var context = _dbFactory.CreateContext();
        var room = new Room { Name = "Adjacent Room", Capacity = 10 };
        context.Rooms.Add(room);
        await context.SaveChangesAsync();

        context.Reservations.Add(new Reservation
        {
            RoomId = room.Id,
            UserId = "user-1",
            Title = "Earlier Meeting",
            AttendeesCount = 5,
            Start = _searchStart.AddHours(-1),
            End = _searchStart,
            IsCancelled = false
        });
        await context.SaveChangesAsync();

        var service = new RoomService(context);
        var request = new RoomSearchRequest(
            Start: _searchStart,
            End: _searchEnd,
            RequiredCapacity: 1,
            RequiredEquipmentIds: []
        );

        // Act
        var result = await service.SearchAvailableAsync(request);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Adjacent Room");
    }

    [Fact]
    public async Task SearchAvailableAsync_WhenAdjacentReservationStartEqualsEnd_IncludesRoom()
    {
        // Arrange
        using var context = _dbFactory.CreateContext();
        var room = new Room { Name = "Adjacent Room 2", Capacity = 10 };
        context.Rooms.Add(room);
        await context.SaveChangesAsync();

        context.Reservations.Add(new Reservation
        {
            RoomId = room.Id,
            UserId = "user-1",
            Title = "Later Meeting",
            AttendeesCount = 5,
            Start = _searchEnd,
            End = _searchEnd.AddHours(1),
            IsCancelled = false
        });
        await context.SaveChangesAsync();

        var service = new RoomService(context);
        var request = new RoomSearchRequest(
            Start: _searchStart,
            End: _searchEnd,
            RequiredCapacity: 1,
            RequiredEquipmentIds: []
        );

        // Act
        var result = await service.SearchAvailableAsync(request);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Adjacent Room 2");
    }

    #endregion

    #region Combined Scenario

    [Fact]
    public async Task SearchAvailableAsync_CombinedFilters_ReturnsOnlyMatchingRoom()
    {
        // Arrange
        using var context = _dbFactory.CreateContext();

        var projector = new Equipment { Name = "Projector" };
        var whiteboard = new Equipment { Name = "Whiteboard" };
        context.Equipments.AddRange(projector, whiteboard);
        await context.SaveChangesAsync();

        // Room 1: Too small capacity (2)
        var room1 = new Room { Name = "Small Room", Capacity = 2 };
        // Room 2: Missing whiteboard
        var room2 = new Room { Name = "Missing Equipment", Capacity = 10 };
        // Room 3: Has overlapping active reservation
        var room3 = new Room { Name = "Busy Room", Capacity = 10 };
        // Room 4: Perfect match
        var room4 = new Room { Name = "Perfect Room", Capacity = 10 };

        context.Rooms.AddRange(room1, room2, room3, room4);
        await context.SaveChangesAsync();

        // Equipment assignments
        context.RoomEquipments.AddRange(
            new RoomEquipment { RoomId = room1.Id, EquipmentId = projector.Id },
            new RoomEquipment { RoomId = room1.Id, EquipmentId = whiteboard.Id },
            new RoomEquipment { RoomId = room2.Id, EquipmentId = projector.Id },
            new RoomEquipment { RoomId = room3.Id, EquipmentId = projector.Id },
            new RoomEquipment { RoomId = room3.Id, EquipmentId = whiteboard.Id },
            new RoomEquipment { RoomId = room4.Id, EquipmentId = projector.Id },
            new RoomEquipment { RoomId = room4.Id, EquipmentId = whiteboard.Id }
        );
        await context.SaveChangesAsync();

        // Active reservation for room3
        context.Reservations.Add(new Reservation
        {
            RoomId = room3.Id,
            UserId = "user-1",
            Title = "Blocking Meeting",
            AttendeesCount = 5,
            Start = _searchStart,
            End = _searchEnd,
            IsCancelled = false
        });
        await context.SaveChangesAsync();

        var service = new RoomService(context);
        var request = new RoomSearchRequest(
            Start: _searchStart,
            End: _searchEnd,
            RequiredCapacity: 5,
            RequiredEquipmentIds: [projector.Id, whiteboard.Id]
        );

        // Act
        var result = await service.SearchAvailableAsync(request);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Perfect Room");
        result[0].Id.Should().Be(room4.Id);
    }

    [Fact]
    public async Task SearchAvailableAsync_MultipleMatchingRooms_ReturnsOrderedByName()
    {
        // Arrange
        using var context = _dbFactory.CreateContext();
        context.Rooms.Add(new Room { Name = "Zebra Room", Capacity = 10 });
        context.Rooms.Add(new Room { Name = "Alpha Room", Capacity = 10 });
        context.Rooms.Add(new Room { Name = "Beta Room", Capacity = 10 });
        await context.SaveChangesAsync();

        var service = new RoomService(context);
        var request = new RoomSearchRequest(
            Start: _searchStart,
            End: _searchEnd,
            RequiredCapacity: 1,
            RequiredEquipmentIds: []
        );

        // Act
        var result = await service.SearchAvailableAsync(request);

        // Assert
        result.Should().HaveCount(3);
        result.Select(r => r.Name).Should().ContainInOrder("Alpha Room", "Beta Room", "Zebra Room");
    }

    #endregion
}
