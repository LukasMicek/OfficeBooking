using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeBooking.Data;
using OfficeBooking.Models;
using OfficeBooking.ViewModels;

namespace OfficeBooking.Controllers
{
    [Authorize]
    public class ReservationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReservationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> My()
        {
            if (User.IsInRole("Admin"))
            {
                return Forbid();
            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Forbid();

            var myReservations = await _context.Reservations
                .Include(r => r.Room)
                .Where(r => r.UserId == userId && !r.IsCancelled)
                .OrderByDescending(r => r.Start)
                .ToListAsync();

            return View(myReservations);
        }

        [HttpGet]
        public async Task<IActionResult> History()
        {
            if (User.IsInRole("Admin"))
            {
                return Forbid();
            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Forbid();

            var history = await _context.Reservations
                .Include(r => r.Room)
                .Where(r => r.UserId == userId && r.IsCancelled)
                .OrderByDescending(r => r.CancelledAt)
                .ToListAsync();

            return View(history);
        }


        [HttpGet]
        public async Task<IActionResult> Create(int roomId, DateTime? startDate, TimeSpan? startTime, DateTime? endDate, TimeSpan? endTime)
        {
            var room = await _context.Rooms.FirstOrDefaultAsync(r => r.Id == roomId);
            if (room == null) return NotFound();

            var vm = new ReservationCreateViewModel
            {
                RoomId = room.Id,
                RoomName = room.Name,
                RoomCapacity = room.Capacity,

                StartDate = startDate ?? DateTime.Today,
                StartTime = startTime ?? new TimeSpan(9, 0, 0),
                EndDate = endDate ?? DateTime.Today,
                EndTime = endTime ?? new TimeSpan(10, 0, 0),

                AttendeesCount = 1
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Forbid();

            var reservation = await _context.Reservations
                .Include(r => r.Room)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (reservation == null) return NotFound();

            if (reservation.Start <= DateTime.Now)
            {
                TempData["Error"] = "Nie można usunąć rezerwacji, która już się rozpoczęła.";
                return RedirectToAction(nameof(My));
            }

            return View(reservation);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Forbid();

            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (reservation == null) return NotFound();

            if (reservation.Start <= DateTime.Now)
            {
                TempData["Error"] = "Nie można usunąć rezerwacji, która już się rozpoczęła.";
                return RedirectToAction(nameof(My));
            }

            reservation.IsCancelled = true;
            reservation.CancelledAt = DateTime.Now;
            reservation.CancelReason = "Anulowane przez użytkownika";

            await _context.SaveChangesAsync();

            TempData["Success"] = "Rezerwacja została anulowana.";
            return RedirectToAction(nameof(My));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Forbid();

            var reservation = await _context.Reservations
                .Include(r => r.Room)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (reservation == null) return NotFound();

            if (reservation.Start <= DateTime.Now)
            {
                TempData["Error"] = "Nie można edytować rezerwacji, która już się rozpoczęła.";
                return RedirectToAction(nameof(My));
            }

            var vm = new ReservationCreateViewModel
            {
                RoomId = reservation.RoomId,
                RoomName = reservation.Room.Name,
                RoomCapacity = reservation.Room.Capacity,

                Title = reservation.Title,
                Notes = reservation.Notes,
                AttendeesCount = reservation.AttendeesCount,

                StartDate = reservation.Start.Date,
                StartTime = reservation.Start.TimeOfDay,
                EndDate = reservation.End.Date,
                EndTime = reservation.End.TimeOfDay
            };

            // Przekaż Id rezerwacji przez ViewData 
            ViewData["ReservationId"] = reservation.Id;

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ReservationCreateViewModel vm)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Forbid();

            var reservation = await _context.Reservations
                .Include(r => r.Room)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (reservation == null) return NotFound();

            if (reservation.Start <= DateTime.Now)
            {
                TempData["Error"] = "Nie można edytować rezerwacji, która już się rozpoczęła.";
                return RedirectToAction(nameof(My));
            }

            // Odśwież dane sali do widoku, gdyby wróciło na formularz
            vm.RoomId = reservation.RoomId;
            vm.RoomName = reservation.Room.Name;
            vm.RoomCapacity = reservation.Room.Capacity;

            if (!ModelState.IsValid)
            {
                ViewData["ReservationId"] = reservation.Id;
                return View(vm);
            }

            if (vm.AttendeesCount > reservation.Room.Capacity)
            {
                ModelState.AddModelError(nameof(vm.AttendeesCount),
                    $"Liczba uczestników nie może przekraczać pojemności sali ({reservation.Room.Capacity}).");
                ViewData["ReservationId"] = reservation.Id;
                return View(vm);
            }

            var start = vm.StartDateTime;
            var end = vm.EndDateTime;

            // Konflikt z inną rezerwacją tej sali
            var hasConflict = await _context.Reservations.AnyAsync(r =>
                r.RoomId == reservation.RoomId &&
                r.Id != reservation.Id &&
                !r.IsCancelled &&
                start < r.End &&
                end > r.Start);

            if (hasConflict)
            {
                ModelState.AddModelError("", "Sala jest już zajęta w wybranym przedziale czasu.");
                ViewData["ReservationId"] = reservation.Id;
                return View(vm);
            }

            // Zapis zmian
            reservation.Title = vm.Title;
            reservation.Notes = vm.Notes;
            reservation.AttendeesCount = vm.AttendeesCount;
            reservation.Start = start;
            reservation.End = end;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Rezerwacja została zaktualizowana.";
            return RedirectToAction(nameof(My));
        }
        
        [HttpPost]
        public async Task<IActionResult> Create(ReservationCreateViewModel vm)
        {
            var room = await _context.Rooms.FirstOrDefaultAsync(r => r.Id == vm.RoomId);
            if (room == null) return NotFound();

            vm.RoomName = room.Name;
            vm.RoomCapacity = room.Capacity;

            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            if (vm.AttendeesCount > room.Capacity)
            {
                ModelState.AddModelError(nameof(vm.AttendeesCount),
                    $"Liczba uczestników nie może przekraczać pojemności sali ({room.Capacity}).");
                return View(vm);
            }

            var start = vm.StartDateTime;
            var end = vm.EndDateTime;

            var hasConflict = await _context.Reservations.AnyAsync(r =>
                r.RoomId == vm.RoomId &&
                !r.IsCancelled &&
                start < r.End &&
                end > r.Start);

            if (hasConflict)
            {
                ModelState.AddModelError("", "Sala jest już zajęta w wybranym przedziale czasu.");
                return View(vm);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Forbid();

            var reservation = new Reservation
            {
                RoomId = vm.RoomId,
                Title = vm.Title,
                Notes = vm.Notes,
                AttendeesCount = vm.AttendeesCount,
                Start = start,
                End = end,
                UserId = userId
            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Rezerwacja została utworzona.";
            return RedirectToAction("Index", "Home");
        }
    }
}

