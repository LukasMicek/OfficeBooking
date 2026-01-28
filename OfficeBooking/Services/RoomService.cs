using Microsoft.EntityFrameworkCore;
using OfficeBooking.Data;
using OfficeBooking.Models;

namespace OfficeBooking.Services;

public class RoomService : IRoomService
{
    private readonly ApplicationDbContext _context;

    public RoomService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Room>> GetAllAsync()
    {
        return await _context.Rooms
            .AsNoTracking()
            .Include(r => r.RoomEquipments)
                .ThenInclude(re => re.Equipment)
            .OrderBy(r => r.Name)
            .ToListAsync();
    }

    public async Task<Room?> GetByIdAsync(int id)
    {
        return await _context.Rooms
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<Room?> GetByIdWithEquipmentAsync(int id)
    {
        return await _context.Rooms
            .AsNoTracking()
            .Include(r => r.RoomEquipments)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<IReadOnlyList<Equipment>> GetAllEquipmentAsync()
    {
        return await _context.Equipments
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .ToListAsync();
    }

    public async Task<RoomResult> CreateAsync(CreateRoomRequest request)
    {
        var room = new Room
        {
            Name = request.Name,
            Capacity = request.Capacity
        };

        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();

        if (request.EquipmentIds.Count > 0)
        {
            foreach (var eqId in request.EquipmentIds.Distinct())
            {
                _context.RoomEquipments.Add(new RoomEquipment
                {
                    RoomId = room.Id,
                    EquipmentId = eqId
                });
            }
            await _context.SaveChangesAsync();
        }

        return RoomResult.Ok(room);
    }

    public async Task<RoomResult> UpdateAsync(int id, UpdateRoomRequest request)
    {
        var room = await _context.Rooms
            .Include(r => r.RoomEquipments)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (room == null)
            return RoomResult.Fail("Sala nie istnieje.");

        room.Name = request.Name;
        room.Capacity = request.Capacity;

        var selectedIds = request.EquipmentIds.Distinct().ToList();
        var existingIds = room.RoomEquipments.Select(re => re.EquipmentId).ToList();

        var toRemove = room.RoomEquipments
            .Where(re => !selectedIds.Contains(re.EquipmentId))
            .ToList();
        _context.RoomEquipments.RemoveRange(toRemove);

        var toAdd = selectedIds.Where(eqId => !existingIds.Contains(eqId));
        foreach (var eqId in toAdd)
        {
            _context.RoomEquipments.Add(new RoomEquipment
            {
                RoomId = room.Id,
                EquipmentId = eqId
            });
        }

        await _context.SaveChangesAsync();
        return RoomResult.Ok(room);
    }

    public async Task<RoomResult> DeleteAsync(int id)
    {
        var hasActiveReservations = await _context.Reservations
            .AnyAsync(r => r.RoomId == id && !r.IsCancelled);

        if (hasActiveReservations)
            return RoomResult.Fail("Nie można usunąć sali, która ma aktywne rezerwacje. Najpierw anuluj rezerwacje tej sali.");

        var roomEquipments = await _context.RoomEquipments
            .Where(re => re.RoomId == id)
            .ToListAsync();
        _context.RoomEquipments.RemoveRange(roomEquipments);

        var room = await _context.Rooms.FindAsync(id);
        if (room == null)
            return RoomResult.Fail("Sala nie istnieje.");

        _context.Rooms.Remove(room);
        await _context.SaveChangesAsync();

        return RoomResult.Ok(room);
    }

    public async Task<IReadOnlyList<Room>> SearchAvailableAsync(RoomSearchRequest request)
    {
        var query = _context.Rooms
            .AsNoTracking()
            .Include(r => r.RoomEquipments)
                .ThenInclude(re => re.Equipment)
            .Include(r => r.Reservations)
            .AsQueryable();

        // Filter by capacity
        query = query.Where(r => r.Capacity >= request.RequiredCapacity);

        // Filter by required equipment
        if (request.RequiredEquipmentIds.Count > 0)
        {
            query = query.Where(r => request.RequiredEquipmentIds.All(eqId =>
                r.RoomEquipments.Any(re => re.EquipmentId == eqId)));
        }

        // Filter by availability (no overlapping reservations)
        query = query.Where(r => !r.Reservations.Any(res =>
            !res.IsCancelled &&
            request.Start < res.End &&
            request.End > res.Start));

        return await query
            .OrderBy(r => r.Name)
            .ToListAsync();
    }
}
