using Microsoft.EntityFrameworkCore;
using OfficeBooking.Data;
using OfficeBooking.ViewModels;

namespace OfficeBooking.Services;

public class AdminReservationService : IAdminReservationService
{
    private readonly ApplicationDbContext _context;

    public AdminReservationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AdminReservationRowViewModel>> GetListAsync(bool activeOnly)
    {
        return await (
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
                IsCancelled = r.IsCancelled,
                CancelledAt = r.CancelledAt,
                CancelReason = r.CancelReason
            }
        ).ToListAsync();
    }

    public async Task<AdminCancelReservationViewModel?> GetCancelViewModelAsync(int id)
    {
        var reservation = await _context.Reservations.FirstOrDefaultAsync(r => r.Id == id);
        if (reservation == null)
            return null;

        if (reservation.IsCancelled)
            return null;

        return await (
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
    }

    public async Task<AdminCancelResult> CancelAsync(int id, string cancelReason, DateTime now)
    {
        var reservation = await _context.Reservations.FirstOrDefaultAsync(r => r.Id == id);
        if (reservation == null)
            return AdminCancelResult.NotFound();

        if (reservation.IsCancelled)
            return AdminCancelResult.AlreadyCancelled();

        reservation.IsCancelled = true;
        reservation.CancelledAt = now;
        reservation.CancelReason = cancelReason;

        await _context.SaveChangesAsync();

        return AdminCancelResult.Ok();
    }
}
