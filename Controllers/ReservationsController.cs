using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeBooking.Data;
using OfficeBooking.Services;
using OfficeBooking.ViewModels;

namespace OfficeBooking.Controllers;

[Authorize]
public class ReservationsController : Controller
{
    private readonly IReservationService _reservationService;
    private readonly ApplicationDbContext _context;

    public ReservationsController(IReservationService reservationService, ApplicationDbContext context)
    {
        _reservationService = reservationService;
        _context = context;
    }

    private string? GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    [HttpGet]
    public async Task<IActionResult> My()
    {
        if (User.IsInRole("Admin"))
            return Forbid();

        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Forbid();

        var reservations = await _reservationService.GetUserReservationsAsync(userId);
        return View(reservations);
    }

    [HttpGet]
    public async Task<IActionResult> History()
    {
        if (User.IsInRole("Admin"))
            return Forbid();

        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Forbid();

        var history = await _reservationService.GetCancelledReservationsAsync(userId);
        return View(history);
    }

    [HttpGet]
    public async Task<IActionResult> Create(
        int roomId,
        DateTime? startDate,
        TimeSpan? startTime,
        DateTime? endDate,
        TimeSpan? endTime)
    {
        var room = await _context.Rooms
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == roomId);

        if (room == null)
            return NotFound();

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReservationCreateViewModel vm)
    {
        var room = await _context.Rooms
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == vm.RoomId);

        if (room == null)
            return NotFound();

        vm.RoomName = room.Name;
        vm.RoomCapacity = room.Capacity;

        if (!ModelState.IsValid)
            return View(vm);

        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Forbid();

        var request = new CreateReservationRequest(
            vm.RoomId,
            userId,
            vm.Title,
            vm.Notes,
            vm.AttendeesCount,
            vm.StartDateTime,
            vm.EndDateTime
        );

        var result = await _reservationService.CreateAsync(request);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(vm);
        }

        TempData["Success"] = "Rezerwacja została utworzona.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Forbid();

        var reservation = await _reservationService.GetByIdForUserAsync(id, userId);
        if (reservation == null)
            return NotFound();

        if (reservation.Start <= DateTime.Now)
        {
            TempData["Error"] = "Nie można anulować rezerwacji, która już się rozpoczęła.";
            return RedirectToAction(nameof(My));
        }

        return View(reservation);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Forbid();

        var result = await _reservationService.CancelAsync(id, userId);

        if (!result.Success)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(My));
        }

        TempData["Success"] = "Rezerwacja została anulowana.";
        return RedirectToAction(nameof(My));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Forbid();

        var reservation = await _reservationService.GetByIdForUserAsync(id, userId);
        if (reservation == null)
            return NotFound();

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

        ViewData["ReservationId"] = reservation.Id;
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ReservationCreateViewModel vm)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Forbid();

        var reservation = await _reservationService.GetByIdForUserAsync(id, userId);
        if (reservation == null)
            return NotFound();

        vm.RoomId = reservation.RoomId;
        vm.RoomName = reservation.Room.Name;
        vm.RoomCapacity = reservation.Room.Capacity;

        if (!ModelState.IsValid)
        {
            ViewData["ReservationId"] = id;
            return View(vm);
        }

        var request = new UpdateReservationRequest(
            vm.Title,
            vm.Notes,
            vm.AttendeesCount,
            vm.StartDateTime,
            vm.EndDateTime
        );

        var result = await _reservationService.UpdateAsync(id, userId, request);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            ViewData["ReservationId"] = id;
            return View(vm);
        }

        TempData["Success"] = "Rezerwacja została zaktualizowana.";
        return RedirectToAction(nameof(My));
    }
}
