using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeBooking.Data;
using OfficeBooking.ViewModels;

namespace OfficeBooking.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> RoomUsage(int? year, int? month)
        {
            var y = year ?? DateTime.Today.Year;
            var m = month ?? DateTime.Today.Month;

            var from = new DateTime(y, m, 1);
            var to = from.AddMonths(1);

            // Bierzemy tylko rezerwacje nieanulowane, które nachodzą na miesiąc
            var reservations = await _context.Reservations
                .Where(r => !r.IsCancelled && r.Start < to && r.End > from)
                .Select(r => new { r.RoomId, r.Start, r.End })
                .ToListAsync();

            // Zliczamy minuty
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

            var vm = new RoomUsageReportViewModel
            {
                Year = y,
                Month = m,
                Rows = rows
            };

            return View(vm);
        }
    }
}

