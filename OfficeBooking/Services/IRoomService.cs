using OfficeBooking.Models;

namespace OfficeBooking.Services;

public interface IRoomService
{
    Task<IReadOnlyList<Room>> GetAllAsync();
    Task<Room?> GetByIdAsync(int id);
    Task<Room?> GetByIdWithEquipmentAsync(int id);
    Task<IReadOnlyList<Equipment>> GetAllEquipmentAsync();
    Task<RoomResult> CreateAsync(CreateRoomRequest request);
    Task<RoomResult> UpdateAsync(int id, UpdateRoomRequest request);
    Task<RoomResult> DeleteAsync(int id);
    Task<IReadOnlyList<Room>> SearchAvailableAsync(RoomSearchRequest request);
}

public record CreateRoomRequest(
    string Name,
    int Capacity,
    IReadOnlyList<int> EquipmentIds
);

public record UpdateRoomRequest(
    string Name,
    int Capacity,
    IReadOnlyList<int> EquipmentIds
);

public record RoomSearchRequest(
    DateTime Start,
    DateTime End,
    int RequiredCapacity,
    IReadOnlyList<int> RequiredEquipmentIds
);

public record RoomResult(bool Success, string? Error = null, Room? Room = null)
{
    public static RoomResult Ok(Room room) => new(true, null, room);
    public static RoomResult Fail(string error) => new(false, error);
}
