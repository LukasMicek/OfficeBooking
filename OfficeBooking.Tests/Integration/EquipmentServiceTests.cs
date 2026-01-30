using FluentAssertions;
using OfficeBooking.Models;
using OfficeBooking.Services;

namespace OfficeBooking.Tests.Integration;

public class EquipmentServiceTests : IDisposable
{
    private readonly TestDbFactory _dbFactory;

    public EquipmentServiceTests()
    {
        _dbFactory = new TestDbFactory();
    }

    public void Dispose()
    {
        _dbFactory.Dispose();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEquipments()
    {
        // Arrange
        using var context = _dbFactory.CreateContext();
        context.Equipments.AddRange(
            new Equipment { Name = "Projector" },
            new Equipment { Name = "Whiteboard" }
        );
        await context.SaveChangesAsync();

        var service = new EquipmentService(context);

        // Act
        var result = await service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Select(e => e.Name).Should().Contain(new[] { "Projector", "Whiteboard" });
    }

    [Fact]
    public async Task GetAllAsync_WhenEmpty_ReturnsEmptyList()
    {
        // Arrange
        using var context = _dbFactory.CreateContext();
        var service = new EquipmentService(context);

        // Act
        var result = await service.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsEquipment()
    {
        // Arrange
        using var context = _dbFactory.CreateContext();
        var equipment = new Equipment { Name = "Projector" };
        context.Equipments.Add(equipment);
        await context.SaveChangesAsync();

        var service = new EquipmentService(context);

        // Act
        var result = await service.GetByIdAsync(equipment.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Projector");
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ReturnsNull()
    {
        // Arrange
        using var context = _dbFactory.CreateContext();
        var service = new EquipmentService(context);

        // Act
        var result = await service.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_SavesAndReturnsEquipment()
    {
        // Arrange
        using var context = _dbFactory.CreateContext();
        var service = new EquipmentService(context);

        // Act
        var result = await service.CreateAsync("Video Conference System");

        // Assert
        result.Success.Should().BeTrue();
        result.Equipment.Should().NotBeNull();
        result.Equipment!.Name.Should().Be("Video Conference System");
        result.Equipment.Id.Should().BeGreaterThan(0);

        // Verify persisted
        var saved = await service.GetByIdAsync(result.Equipment.Id);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Video Conference System");
    }

    [Fact]
    public async Task UpdateAsync_WhenExists_UpdatesEquipment()
    {
        // Arrange
        using var context = _dbFactory.CreateContext();
        var equipment = new Equipment { Name = "Old Name" };
        context.Equipments.Add(equipment);
        await context.SaveChangesAsync();

        var service = new EquipmentService(context);

        // Act
        var result = await service.UpdateAsync(equipment.Id, "New Name");

        // Assert
        result.Success.Should().BeTrue();
        result.Equipment.Should().NotBeNull();
        result.Equipment!.Name.Should().Be("New Name");

        // Verify persisted
        var saved = await service.GetByIdAsync(equipment.Id);
        saved!.Name.Should().Be("New Name");
    }

    [Fact]
    public async Task UpdateAsync_WhenNotExists_ReturnsFailure()
    {
        // Arrange
        using var context = _dbFactory.CreateContext();
        var service = new EquipmentService(context);

        // Act
        var result = await service.UpdateAsync(999, "New Name");

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task DeleteAsync_WhenExists_RemovesEquipment()
    {
        // Arrange
        using var context = _dbFactory.CreateContext();
        var equipment = new Equipment { Name = "To Delete" };
        context.Equipments.Add(equipment);
        await context.SaveChangesAsync();
        var id = equipment.Id;

        var service = new EquipmentService(context);

        // Act
        var result = await service.DeleteAsync(id);

        // Assert
        result.Success.Should().BeTrue();

        // Verify removed
        var deleted = await service.GetByIdAsync(id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WhenNotExists_ReturnsFailure()
    {
        // Arrange
        using var context = _dbFactory.CreateContext();
        var service = new EquipmentService(context);

        // Act
        var result = await service.DeleteAsync(999);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExistsAsync_WhenExists_ReturnsTrue()
    {
        // Arrange
        using var context = _dbFactory.CreateContext();
        var equipment = new Equipment { Name = "Existing" };
        context.Equipments.Add(equipment);
        await context.SaveChangesAsync();

        var service = new EquipmentService(context);

        // Act
        var result = await service.ExistsAsync(equipment.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenNotExists_ReturnsFalse()
    {
        // Arrange
        using var context = _dbFactory.CreateContext();
        var service = new EquipmentService(context);

        // Act
        var result = await service.ExistsAsync(999);

        // Assert
        result.Should().BeFalse();
    }
}
