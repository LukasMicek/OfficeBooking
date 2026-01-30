using OfficeBooking.Models;

namespace OfficeBooking.Services;

public interface IEquipmentService
{
    Task<IReadOnlyList<Equipment>> GetAllAsync();
    Task<Equipment?> GetByIdAsync(int id);
    Task<EquipmentResult> CreateAsync(string name);
    Task<EquipmentResult> UpdateAsync(int id, string name);
    Task<EquipmentResult> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}

public record EquipmentResult(bool Success, string? Error = null, Equipment? Equipment = null)
{
    public static EquipmentResult Ok(Equipment equipment) => new(true, null, equipment);
    public static EquipmentResult Fail(string error) => new(false, error);
}
