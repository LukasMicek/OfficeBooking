using Microsoft.EntityFrameworkCore;
using OfficeBooking.Data;
using OfficeBooking.Models;

namespace OfficeBooking.Services;

public class EquipmentService : IEquipmentService
{
    private readonly ApplicationDbContext _context;

    public EquipmentService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Equipment>> GetAllAsync()
    {
        return await _context.Equipments.ToListAsync();
    }

    public async Task<Equipment?> GetByIdAsync(int id)
    {
        return await _context.Equipments.FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<EquipmentResult> CreateAsync(string name)
    {
        var equipment = new Equipment { Name = name };
        _context.Equipments.Add(equipment);
        await _context.SaveChangesAsync();

        return EquipmentResult.Ok(equipment);
    }

    public async Task<EquipmentResult> UpdateAsync(int id, string name)
    {
        var equipment = await _context.Equipments.FindAsync(id);
        if (equipment == null)
        {
            return EquipmentResult.Fail("Wyposażenie nie istnieje.");
        }

        equipment.Name = name;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await ExistsAsync(id))
            {
                return EquipmentResult.Fail("Wyposażenie nie istnieje.");
            }
            throw;
        }

        return EquipmentResult.Ok(equipment);
    }

    public async Task<EquipmentResult> DeleteAsync(int id)
    {
        var equipment = await _context.Equipments.FindAsync(id);
        if (equipment == null)
        {
            return EquipmentResult.Fail("Wyposażenie nie istnieje.");
        }

        _context.Equipments.Remove(equipment);
        await _context.SaveChangesAsync();

        return EquipmentResult.Ok(equipment);
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Equipments.AnyAsync(e => e.Id == id);
    }
}
