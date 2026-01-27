using OfficeBooking.Models;

namespace OfficeBooking.Services;

public interface IReservationService
{
    Task<IReadOnlyList<Reservation>> GetUserReservationsAsync(string userId, bool includeCancelled = false);
    Task<IReadOnlyList<Reservation>> GetCancelledReservationsAsync(string userId);
    Task<Reservation?> GetByIdAsync(int id);
    Task<Reservation?> GetByIdForUserAsync(int id, string userId);
    Task<ReservationResult> CreateAsync(CreateReservationRequest request);
    Task<ReservationResult> UpdateAsync(int id, string userId, UpdateReservationRequest request);
    Task<ReservationResult> CancelAsync(int id, string userId, string? reason = null);
    Task<bool> CanModifyAsync(int id, string userId);
}

public record CreateReservationRequest(
    int RoomId,
    string UserId,
    string Title,
    string? Notes,
    int AttendeesCount,
    DateTime Start,
    DateTime End
);

public record UpdateReservationRequest(
    string Title,
    string? Notes,
    int AttendeesCount,
    DateTime Start,
    DateTime End
);

public record ReservationResult(bool Success, string? Error = null, Reservation? Reservation = null)
{
    public static ReservationResult Ok(Reservation reservation) => new(true, null, reservation);
    public static ReservationResult Fail(string error) => new(false, error);
}
