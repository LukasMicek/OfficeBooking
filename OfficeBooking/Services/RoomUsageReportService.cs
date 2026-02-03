using Microsoft.EntityFrameworkCore;
using OfficeBooking.Data;
using OfficeBooking.ViewModels;

namespace OfficeBooking.Services;

public class RoomUsageReportService : IRoomUsageReportService
{
    private readonly ApplicationDbContext _context;

    public RoomUsageReportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RoomUsageReportViewModel> GetReportAsync(int? year, int? month, DateTime today)
    {
        var y = year ?? today.Year;
        var m = month ?? today.Month;

        var from = new DateTime(y, m, 1);
        var to = from.AddMonths(1);

        var reservations = await _context.Reservations
            .Where(r => !r.IsCancelled && r.Start < to && r.End > from)
            .Select(r => new { r.RoomId, r.Start, r.End })
            .ToListAsync();

        var minutesByRoom = reservations
            .GroupBy(r => r.RoomId)
            .Select(g => new
            {
                RoomId = g.Key,
                TotalMinutes = (int)g.Sum(x => (x.End - x.Start).TotalMinutes)
            })
            .ToList();

        var rooms = await _context.Rooms
            .Select(r => new { r.Id, r.Name })
            .ToListAsync();

        var rows = minutesByRoom
            .Join(rooms, a => a.RoomId, b => b.Id, (a, b) => new RoomUsageReportRowViewModel
            {
                RoomId = b.Id,
                RoomName = b.Name,
                TotalMinutes = a.TotalMinutes
            })
            .OrderByDescending(r => r.TotalMinutes)
            .ToList();

        return new RoomUsageReportViewModel
        {
            Year = y,
            Month = m,
            Rows = rows
        };
    }
}
