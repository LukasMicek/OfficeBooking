using FluentAssertions;
using OfficeBooking.Business;
using OfficeBooking.Models;

namespace OfficeBooking.Tests.Unit;

public class ReservationConflictTests
{
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

    [Theory]
    [InlineData(10, 11, 11, 12)] // Adjacent: end == start
    [InlineData(10, 11, 9, 10)]  // Adjacent: start == end
    public void Overlaps_WhenAdjacent_ReturnsFalse(int startA, int endA, int startB, int endB)
    {
        var result = ReservationConflict.Overlaps(
            new DateTime(2026, 1, 26, startA, 0, 0),
            new DateTime(2026, 1, 26, endA, 0, 0),
            new DateTime(2026, 1, 26, startB, 0, 0),
            new DateTime(2026, 1, 26, endB, 0, 0));

        result.Should().BeFalse();
    }

    [Fact]
    public void HasConflict_WhenOverlappingReservation_ReturnsTrue()
    {
        var existing = new List<Reservation>
        {
            new() { Id = 1, Start = new DateTime(2026, 1, 26, 10, 0, 0), End = new DateTime(2026, 1, 26, 11, 0, 0), IsCancelled = false }
        };

        var result = ReservationConflict.HasConflict(
            existing,
            new DateTime(2026, 1, 26, 10, 30, 0),
            new DateTime(2026, 1, 26, 11, 30, 0));

        result.Should().BeTrue();
    }

    [Fact]
    public void HasConflict_WhenEmptyList_ReturnsFalse()
    {
        var result = ReservationConflict.HasConflict(
            new List<Reservation>(),
            new DateTime(2026, 1, 26, 10, 0, 0),
            new DateTime(2026, 1, 26, 11, 0, 0));

        result.Should().BeFalse();
    }

    [Fact]
    public void HasConflict_IgnoresCancelledReservations()
    {
        var existing = new List<Reservation>
        {
            new() { Id = 1, Start = new DateTime(2026, 1, 26, 10, 0, 0), End = new DateTime(2026, 1, 26, 11, 0, 0), IsCancelled = true }
        };

        var result = ReservationConflict.HasConflict(
            existing,
            new DateTime(2026, 1, 26, 10, 30, 0),
            new DateTime(2026, 1, 26, 11, 30, 0));

        result.Should().BeFalse();
    }

    [Fact]
    public void HasConflict_WhenIgnoringReservationId_ExcludesIt()
    {
        var existing = new List<Reservation>
        {
            new() { Id = 5, Start = new DateTime(2026, 1, 26, 10, 0, 0), End = new DateTime(2026, 1, 26, 11, 0, 0), IsCancelled = false }
        };

        var result = ReservationConflict.HasConflict(
            existing,
            new DateTime(2026, 1, 26, 10, 30, 0),
            new DateTime(2026, 1, 26, 11, 30, 0),
            ignoreReservationId: 5);

        result.Should().BeFalse();
    }
}
