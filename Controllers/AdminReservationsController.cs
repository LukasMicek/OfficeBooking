using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeBooking.Data;
using OfficeBooking.ViewModels;

namespace OfficeBooking.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminReservationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminReservationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(bool activeOnly = false)
        {
            var list = await (
                from r in _context.Reservations
                join room in _context.Rooms on r.RoomId equals room.Id
                join u in _context.Users on r.UserId equals u.Id
                where !activeOnly || !r.IsCancelled
                orderby r.Start descending
                select new AdminReservationRowViewModel
                {
                    Id = r.Id,
                    RoomName = room.Name,
                    Title = r.Title,
                    Start = r.Start,
                    End = r.End,
                    AttendeesCount = r.AttendeesCount,
                    UserEmail = u.Email ?? "(brak email)",
                    IsCancelled = r.IsCancelled
                }
            ).ToListAsync();

            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Cancel(int id)
        {
            var vm = await (
                from r in _context.Reservations
                join room in _context.Rooms on r.RoomId equals room.Id
                join u in _context.Users on r.UserId equals u.Id
                where r.Id == id
                select new AdminCancelReservationViewModel
                {
                    Id = r.Id,
                    RoomName = room.Name,
                    Title = r.Title,
                    Start = r.Start,
                    End = r.End,
                    UserEmail = u.Email ?? "(brak email)"
                }
            ).FirstOrDefaultAsync();

            if (vm == null) return NotFound();

            var isAlreadyCancelled = await _context.Reservations
                .AnyAsync(r => r.Id == id && r.IsCancelled);

            if (isAlreadyCancelled)
            {
                TempData["Error"] = "Ta rezerwacja jest już anulowana.";
                return RedirectToAction(nameof(Index));
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(AdminCancelReservationViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var reservation = await _context.Reservations.FirstOrDefaultAsync(r => r.Id == vm.Id);
            if (reservation == null) return NotFound();

            if (reservation.IsCancelled)
            {
                TempData["Error"] = "Ta rezerwacja jest już anulowana.";
                return RedirectToAction(nameof(Index));
            }

            reservation.IsCancelled = true;
            reservation.CancelledAt = DateTime.Now;
            reservation.CancelReason = vm.CancelReason;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Rezerwacja została anulowana.";
            return RedirectToAction(nameof(Index));
        }
    }
}
