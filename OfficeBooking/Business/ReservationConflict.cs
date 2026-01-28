using OfficeBooking.Models;

namespace OfficeBooking.Business;

// Kept for backwards compatibility - delegates to BookingRules
public static class ReservationConflict
{
    public static bool Overlaps(DateTime startA, DateTime endA, DateTime startB, DateTime endB)
        => BookingRules.IntervalsOverlap(startA, endA, startB, endB);

    public static bool HasConflict(
        IEnumerable<Reservation> existingReservations,
        DateTime newStart,
        DateTime newEnd,
        int? ignoreReservationId = null)
        => BookingRules.HasTimeConflict(existingReservations, newStart, newEnd, ignoreReservationId);
}
