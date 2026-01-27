using Microsoft.EntityFrameworkCore;
using OfficeBooking.Business;
using OfficeBooking.Data;
using OfficeBooking.Models;

namespace OfficeBooking.Services;

public class ReservationService : IReservationService
{
    private readonly ApplicationDbContext _context;

    private const string DefaultCancelReason = "Anulowane przez użytkownika";

    public ReservationService(ApplicationDbContext context)
    {
        _context = context;
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
        var room = await _context.Rooms
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.RoomId);

        if (room == null)
            return ReservationResult.Fail("Sala nie istnieje.");

        if (request.AttendeesCount > room.Capacity)
            return ReservationResult.Fail($"Liczba uczestników nie może przekraczać pojemności sali ({room.Capacity}).");

        var existingReservations = await _context.Reservations
            .AsNoTracking()
            .Where(r => r.RoomId == request.RoomId)
            .ToListAsync();

        if (ReservationConflict.HasConflict(existingReservations, request.Start, request.End))
            return ReservationResult.Fail("Sala jest już zajęta w wybranym przedziale czasu.");

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
            return ReservationResult.Fail("Rezerwacja nie istnieje.");

        if (reservation.Start <= DateTime.Now)
            return ReservationResult.Fail("Nie można edytować rezerwacji, która już się rozpoczęła.");

        if (request.AttendeesCount > reservation.Room.Capacity)
            return ReservationResult.Fail($"Liczba uczestników nie może przekraczać pojemności sali ({reservation.Room.Capacity}).");

        var existingReservations = await _context.Reservations
            .AsNoTracking()
            .Where(r => r.RoomId == reservation.RoomId)
            .ToListAsync();

        if (ReservationConflict.HasConflict(existingReservations, request.Start, request.End, ignoreReservationId: id))
            return ReservationResult.Fail("Sala jest już zajęta w wybranym przedziale czasu.");

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
            return ReservationResult.Fail("Rezerwacja nie istnieje.");

        if (reservation.Start <= DateTime.Now)
            return ReservationResult.Fail("Nie można anulować rezerwacji, która już się rozpoczęła.");

        reservation.IsCancelled = true;
        reservation.CancelledAt = DateTime.Now;
        reservation.CancelReason = reason ?? DefaultCancelReason;

        await _context.SaveChangesAsync();

        return ReservationResult.Ok(reservation);
    }

    public async Task<bool> CanModifyAsync(int id, string userId)
    {
        var reservation = await _context.Reservations
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

        return reservation != null && reservation.Start > DateTime.Now;
    }
}
