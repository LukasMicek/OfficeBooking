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
        DateTime now,
        bool allowPastBookings = false)
    {
        if (!IsWithinBusinessHours(startTime))
        {
            yield return new ValidationResult(
                "Start time must be within business hours (08:00–20:00).",
                new[] { "StartTime" }
            );
        }

        if (!IsWithinBusinessHours(endTime))
        {
            yield return new ValidationResult(
                "End time must be within business hours (08:00–20:00).",
                new[] { "EndTime" }
            );
        }

        if (endDateTime <= startDateTime)
        {
            yield return new ValidationResult(
                "End date/time must be later than start date/time.",
                new[] { "EndTime" }
            );
        }

        if (!allowPastBookings && startDateTime < now)
        {
            yield return new ValidationResult(
                "Cannot create a reservation in the past.",
                new[] { "StartDate" }
            );
        }

        var duration = endDateTime - startDateTime;
        if (duration > MaxReservationDuration)
        {
            yield return new ValidationResult(
                $"Maximum reservation duration is {MaxReservationDuration.TotalHours} hours.",
                new[] { "EndTime" }
            );
        }
    }

    // Service-level validation (simpler, returns string error or null)
    public static string? ValidateTimeRangeForService(DateTime start, DateTime end, DateTime now)
    {
        if (end <= start)
            return "End date/time must be later than start date/time.";

        if (start < now)
            return "Cannot create a reservation in the past.";

        if (!IsWithinBusinessHours(start.TimeOfDay))
            return "Start time must be within business hours (08:00–20:00).";

        if (!IsWithinBusinessHours(end.TimeOfDay))
            return "End time must be within business hours (08:00–20:00).";

        var duration = end - start;
        if (duration > MaxReservationDuration)
            return $"Maximum reservation duration is {MaxReservationDuration.TotalHours} hours.";

        return null;
    }

    // Returns next available hour slot, or tomorrow 9-10 if outside business hours
    public static (DateTime start, DateTime end) GetDefaultTimeSlot(DateTime now)
    {
        var start = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0).AddHours(1);
        var end = start.AddHours(1);

        if (start.TimeOfDay < WorkDayStart || end.TimeOfDay > WorkDayEnd)
        {
            var tomorrow = now.Date.AddDays(1);
            start = tomorrow.AddHours(9);
            end = tomorrow.AddHours(10);
        }

        return (start, end);
    }
}
