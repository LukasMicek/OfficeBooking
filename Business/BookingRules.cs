using System.ComponentModel.DataAnnotations;
using OfficeBooking.Models;

namespace OfficeBooking.Business;

// All booking rules
public static class BookingRules
{
    public static readonly TimeSpan WorkDayStart = new(8, 0, 0);
    public static readonly TimeSpan WorkDayEnd = new(20, 0, 0);
    public static readonly TimeSpan MaxReservationDuration = TimeSpan.FromHours(8);

    public static bool IsWithinBusinessHours(TimeSpan time)
        => time >= WorkDayStart && time <= WorkDayEnd;

    // Two intervals overlap if each starts before the other ends
    public static bool IntervalsOverlap(DateTime startA, DateTime endA, DateTime startB, DateTime endB)
        => startA < endB && endA > startB;

    public static bool HasTimeConflict(
        IEnumerable<Reservation> existingReservations,
        DateTime newStart,
        DateTime newEnd,
        int? ignoreReservationId = null)
    {
        foreach (var reservation in existingReservations)
        {
            if (reservation.IsCancelled)
                continue;

            // Skip the reservation being edited
            if (ignoreReservationId.HasValue && reservation.Id == ignoreReservationId.Value)
                continue;

            if (IntervalsOverlap(newStart, newEnd, reservation.Start, reservation.End))
                return true;
        }

        return false;
    }

    public static IEnumerable<ValidationResult> ValidateTimeRange(
        TimeSpan startTime,
        TimeSpan endTime,
        DateTime startDateTime,
        DateTime endDateTime,
        bool allowPastBookings = false)
    {
        if (!IsWithinBusinessHours(startTime))
        {
            yield return new ValidationResult(
                "Godzina rozpoczęcia musi być w godzinach pracy (08:00–20:00).",
                new[] { "StartTime" }
            );
        }

        if (!IsWithinBusinessHours(endTime))
        {
            yield return new ValidationResult(
                "Godzina zakończenia musi być w godzinach pracy (08:00–20:00).",
                new[] { "EndTime" }
            );
        }

        if (endDateTime <= startDateTime)
        {
            yield return new ValidationResult(
                "Data i godzina zakończenia muszą być późniejsze niż rozpoczęcia.",
                new[] { "EndDate", "EndTime" }
            );
        }

        if (!allowPastBookings && startDateTime < DateTime.Now)
        {
            yield return new ValidationResult(
                "Nie można utworzyć rezerwacji w przeszłości.",
                new[] { "StartDate", "StartTime" }
            );
        }

        var duration = endDateTime - startDateTime;
        if (duration > MaxReservationDuration)
        {
            yield return new ValidationResult(
                $"Maksymalny czas rezerwacji to {MaxReservationDuration.TotalHours} godzin.",
                new[] { "EndDate", "EndTime" }
            );
        }
    }

    // Returns next available hour slot, or tomorrow 9-10 if outside business hours
    public static (DateTime start, DateTime end) GetDefaultTimeSlot()
    {
        var now = DateTime.Now;
        var start = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0).AddHours(1);
        var end = start.AddHours(1);

        if (start.TimeOfDay < WorkDayStart || end.TimeOfDay > WorkDayEnd)
        {
            var tomorrow = DateTime.Today.AddDays(1);
            start = tomorrow.AddHours(9);
            end = tomorrow.AddHours(10);
        }

        return (start, end);
    }
}
