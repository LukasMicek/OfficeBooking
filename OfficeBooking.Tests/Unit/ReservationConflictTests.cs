using FluentAssertions;
using OfficeBooking.Business;
using OfficeBooking.Models;

namespace OfficeBooking.Tests.Unit;

public class ReservationConflictTests
{
    #region Overlaps

    [Fact]
    public void Overlaps_WhenIntervalsOverlap_ReturnsTrue()
    {
        var result = ReservationConflict.Overlaps(
            new DateTime(2026, 1, 26, 10, 0, 0),
            new DateTime(2026, 1, 26, 11, 0, 0),
            new DateTime(2026, 1, 26, 10, 30, 0),
            new DateTime(2026, 1, 26, 11, 30, 0));

        result.Should().BeTrue();
    }

    [Fact]
    public void Overlaps_WhenNoOverlap_ReturnsFalse()
    {
        var result = ReservationConflict.Overlaps(
            new DateTime(2026, 1, 26, 10, 0, 0),
            new DateTime(2026, 1, 26, 11, 0, 0),
            new DateTime(2026, 1, 26, 12, 0, 0),
            new DateTime(2026, 1, 26, 13, 0, 0));

        result.Should().BeFalse();
    }

    #endregion

    #region HasConflict

    [Fact]
    public void HasConflict_WhenTimeRangesOverlap_ReturnsTrue()
    {
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

        var result = ReservationConflict.HasConflict(
            existing,
            new DateTime(2026, 1, 26, 10, 30, 0),
            new DateTime(2026, 1, 26, 11, 30, 0));

        result.Should().BeTrue();
    }

    [Fact]
    public void HasConflict_WhenNoOverlap_ReturnsFalse()
    {
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

        var result = ReservationConflict.HasConflict(
            existing,
            new DateTime(2026, 1, 26, 12, 0, 0),
            new DateTime(2026, 1, 26, 13, 0, 0));

        result.Should().BeFalse();
    }

    [Fact]
    public void HasConflict_WhenTouchingAtEnd_ReturnsFalse()
    {
        // [10:00-11:00] vs [11:00-12:00] => NOT conflict
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

        var result = ReservationConflict.HasConflict(
            existing,
            new DateTime(2026, 1, 26, 11, 0, 0),
            new DateTime(2026, 1, 26, 12, 0, 0));

        result.Should().BeFalse();
    }

    [Fact]
    public void HasConflict_WhenTouchingAtStart_ReturnsFalse()
    {
        // [10:00-11:00] vs [09:00-10:00] => NOT conflict
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

        var result = ReservationConflict.HasConflict(
            existing,
            new DateTime(2026, 1, 26, 9, 0, 0),
            new DateTime(2026, 1, 26, 10, 0, 0));

        result.Should().BeFalse();
    }

    [Fact]
    public void HasConflict_WhenNewContainedInExisting_ReturnsTrue()
    {
        // [10:00-11:00] vs [10:30-10:45] => conflict
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

        var result = ReservationConflict.HasConflict(
            existing,
            new DateTime(2026, 1, 26, 10, 30, 0),
            new DateTime(2026, 1, 26, 10, 45, 0));

        result.Should().BeTrue();
    }

    [Fact]
    public void HasConflict_IgnoresCancelledReservations()
    {
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

        var result = ReservationConflict.HasConflict(
            existing,
            new DateTime(2026, 1, 26, 10, 30, 0),
            new DateTime(2026, 1, 26, 11, 30, 0));

        result.Should().BeFalse();
    }

    [Fact]
    public void HasConflict_WhenIgnoringReservationId_IgnoresIt()
    {
        var existing = new List<Reservation>
        {
            new Reservation
            {
                Id = 5,
                Start = new DateTime(2026, 1, 26, 10, 0, 0),
                End = new DateTime(2026, 1, 26, 11, 0, 0),
                IsCancelled = false
            }
        };

        var result = ReservationConflict.HasConflict(
            existing,
            new DateTime(2026, 1, 26, 10, 30, 0),
            new DateTime(2026, 1, 26, 11, 30, 0),
            ignoreReservationId: 5);

        result.Should().BeFalse();
    }

    [Fact]
    public void HasConflict_WhenIgnoringDifferentId_StillDetectsConflict()
    {
        var existing = new List<Reservation>
        {
            new Reservation
            {
                Id = 5,
                Start = new DateTime(2026, 1, 26, 10, 0, 0),
                End = new DateTime(2026, 1, 26, 11, 0, 0),
                IsCancelled = false
            }
        };

        var result = ReservationConflict.HasConflict(
            existing,
            new DateTime(2026, 1, 26, 10, 30, 0),
            new DateTime(2026, 1, 26, 11, 30, 0),
            ignoreReservationId: 99);

        result.Should().BeTrue();
    }

    [Fact]
    public void HasConflict_WhenEmptyList_ReturnsFalse()
    {
        var existing = new List<Reservation>();

        var result = ReservationConflict.HasConflict(
            existing,
            new DateTime(2026, 1, 26, 10, 0, 0),
            new DateTime(2026, 1, 26, 11, 0, 0));

        result.Should().BeFalse();
    }

    [Fact]
    public void HasConflict_WithMultipleReservations_DetectsAnyConflict()
    {
        var existing = new List<Reservation>
        {
            new Reservation
            {
                Id = 1,
                Start = new DateTime(2026, 1, 26, 8, 0, 0),
                End = new DateTime(2026, 1, 26, 9, 0, 0),
                IsCancelled = false
            },
            new Reservation
            {
                Id = 2,
                Start = new DateTime(2026, 1, 26, 14, 0, 0),
                End = new DateTime(2026, 1, 26, 15, 0, 0),
                IsCancelled = false
            }
        };

        var result = ReservationConflict.HasConflict(
            existing,
            new DateTime(2026, 1, 26, 14, 30, 0),
            new DateTime(2026, 1, 26, 15, 30, 0));

        result.Should().BeTrue();
    }

    #endregion
}
