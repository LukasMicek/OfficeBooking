using Microsoft.EntityFrameworkCore;
using OfficeBooking.Business;
using OfficeBooking.Data;
using OfficeBooking.Models;

namespace OfficeBooking.Services;

public class ReservationService : IReservationService
{
    private readonly ApplicationDbContext _context;
    private readonly TimeProvider _timeProvider;

    private const string DefaultCancelReason = "Cancelled by user";

    public ReservationService(ApplicationDbContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<Reservation>> GetUserReservationsAsync(string userId, bool includeCancelled = false)
    {
        var query = _context.Reservations
            .AsNoTracking()
            .Include(r => r.Room)
            .Where(r => r.UserId == userId);

        if (!includeCancelled)
            query = query.Where(r => !r.IsCancelled);

        return await query
            .OrderByDescending(r => r.Start)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Reservation>> GetCancelledReservationsAsync(string userId)
    {
        return await _context.Reservations
            .AsNoTracking()
            .Include(r => r.Room)
            .Where(r => r.UserId == userId && r.IsCancelled)
            .OrderByDescending(r => r.CancelledAt)
            .ToListAsync();
    }

    public async Task<Reservation?> GetByIdAsync(int id)
    {
        return await _context.Reservations
            .AsNoTracking()
            .Include(r => r.Room)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<Reservation?> GetByIdForUserAsync(int id, string userId)
    {
        return await _context.Reservations
            .AsNoTracking()
            .Include(r => r.Room)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
    }

    public async Task<ReservationResult> CreateAsync(CreateReservationRequest request)
    {
        var now = _timeProvider.GetLocalNow().DateTime;
        var timeError = BookingRules.ValidateTimeRangeForService(request.Start, request.End, now);
        if (timeError != null)
            return ReservationResult.Fail(timeError);

        var room = await _context.Rooms
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.RoomId);

        if (room == null)
            return ReservationResult.Fail("Room does not exist.");

        if (request.AttendeesCount > room.Capacity)
            return ReservationResult.Fail($"Number of attendees cannot exceed room capacity ({room.Capacity}).");

        var existingReservations = await _context.Reservations
            .AsNoTracking()
            .Where(r => r.RoomId == request.RoomId)
            .ToListAsync();

        if (ReservationConflict.HasConflict(existingReservations, request.Start, request.End))
            return ReservationResult.Fail("Room is already booked for the selected time slot.");

        var reservation = new Reservation
        {
            RoomId = request.RoomId,
            UserId = request.UserId,
            Title = request.Title,
            Notes = request.Notes,
            AttendeesCount = request.AttendeesCount,
            Start = request.Start,
            End = request.End
        };

        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();

        return ReservationResult.Ok(reservation);
    }

    public async Task<ReservationResult> UpdateAsync(int id, string userId, UpdateReservationRequest request)
    {
        var reservation = await _context.Reservations
            .Include(r => r.Room)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

        if (reservation == null)
            return ReservationResult.Fail("Reservation does not exist.");

        var now = _timeProvider.GetLocalNow().DateTime;
        if (reservation.Start <= now)
            return ReservationResult.Fail("Cannot edit a reservation that has already started.");

        var timeError = BookingRules.ValidateTimeRangeForService(request.Start, request.End, now);
        if (timeError != null)
            return ReservationResult.Fail(timeError);

        if (request.AttendeesCount > reservation.Room.Capacity)
            return ReservationResult.Fail($"Number of attendees cannot exceed room capacity ({reservation.Room.Capacity}).");

        var existingReservations = await _context.Reservations
            .AsNoTracking()
            .Where(r => r.RoomId == reservation.RoomId)
            .ToListAsync();

        if (ReservationConflict.HasConflict(existingReservations, request.Start, request.End, ignoreReservationId: id))
            return ReservationResult.Fail("Room is already booked for the selected time slot.");

        reservation.Title = request.Title;
        reservation.Notes = request.Notes;
        reservation.AttendeesCount = request.AttendeesCount;
        reservation.Start = request.Start;
        reservation.End = request.End;

        await _context.SaveChangesAsync();

        return ReservationResult.Ok(reservation);
    }

    public async Task<ReservationResult> CancelAsync(int id, string userId, string? reason = null)
    {
        var reservation = await _context.Reservations
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

        if (reservation == null)
            return ReservationResult.Fail("Reservation does not exist.");

        var now = _timeProvider.GetLocalNow().DateTime;
        if (reservation.Start <= now)
            return ReservationResult.Fail("Cannot cancel a reservation that has already started.");

        reservation.IsCancelled = true;
        reservation.CancelledAt = now;
        reservation.CancelReason = reason ?? DefaultCancelReason;

        await _context.SaveChangesAsync();

        return ReservationResult.Ok(reservation);
    }

    public async Task<bool> CanModifyAsync(int id, string userId)
    {
        var reservation = await _context.Reservations
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

        var now = _timeProvider.GetLocalNow().DateTime;
        return reservation != null && reservation.Start > now;
    }
}
