using OfficeBooking.Business;
using OfficeBooking.Models;
using Xunit;

namespace OfficeBooking.Tests.Unit;

public class ReservationConflictTests
{
    [Fact]
    public void HasConflict_ReturnsTrue_WhenTimeRangesOverlap()
    {
        // rezerwacja 10:00�11:00
        var existing = new List<Reservation>
        {
            new Reservation
            {
                Id = 1,
                Start = new DateTime(2026, 1, 26, 10, 0, 0),
                End = new DateTime(2026, 1, 26, 11, 0, 0),
                IsCancelled = false
            }
        };

        // nowa rezerwacja 10:30�11:30 (konflikt)
        var result = ReservationConflict.HasConflict(
            existing,
            new DateTime(2026, 1, 26, 10, 30, 0),
            new DateTime(2026, 1, 26, 11, 30, 0));

        Assert.True(result);
    }

    [Fact]
    public void HasConflict_ReturnsFalse_WhenRangesOnlyTouchAtEdge()
    {
        // rezerwacja 10:00�11:00
        var existing = new List<Reservation>
        {
            new Reservation
            {
                Id = 1,
                Start = new DateTime(2026, 1, 26, 10, 0, 0),
                End = new DateTime(2026, 1, 26, 11, 0, 0),
                IsCancelled = false
            }
        };

        // nowa rezerwacja 11:00�12:00 (bez konfliktu ale na styku)
        var result = ReservationConflict.HasConflict(
            existing,
            new DateTime(2026, 1, 26, 11, 0, 0),
            new DateTime(2026, 1, 26, 12, 0, 0));

        Assert.False(result);
    }

    [Fact]
    public void HasConflict_IgnoresCancelledReservations()
    {
        // rezerwacja anulowana 10:00�11:00
        var existing = new List<Reservation>
        {
            new Reservation
            {
                Id = 1,
                Start = new DateTime(2026, 1, 26, 10, 0, 0),
                End = new DateTime(2026, 1, 26, 11, 0, 0),
                IsCancelled = true
            }
        };

        // nowa rezerwacja 10:30�11:30 (anulowana nie blokuje)
        var result = ReservationConflict.HasConflict(
            existing,
            new DateTime(2026, 1, 26, 10, 30, 0),
            new DateTime(2026, 1, 26, 11, 30, 0));

        Assert.False(result);
    }
}

