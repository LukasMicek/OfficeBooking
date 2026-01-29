using FluentAssertions;
using OfficeBooking.Business;
using OfficeBooking.Models;

namespace OfficeBooking.Tests.Unit;

public class BookingRulesTests
{
    [Theory]
    [InlineData(8, 0, true)]
    [InlineData(12, 0, true)]
    [InlineData(20, 0, true)]
    [InlineData(7, 59, false)]
    [InlineData(20, 1, false)]
    [InlineData(6, 0, false)]
    [InlineData(22, 0, false)]
    public void IsWithinBusinessHours_ReturnsCorrectResult(int hour, int minute, bool expected)
    {
        var time = new TimeSpan(hour, minute, 0);

        var result = BookingRules.IsWithinBusinessHours(time);

        result.Should().Be(expected);
    }

    #region IntervalsOverlap

    [Fact]
    public void IntervalsOverlap_WhenNoOverlap_ReturnsFalse()
    {
        var startA = new DateTime(2024, 1, 1, 9, 0, 0);
        var endA = new DateTime(2024, 1, 1, 10, 0, 0);
        var startB = new DateTime(2024, 1, 1, 11, 0, 0);
        var endB = new DateTime(2024, 1, 1, 12, 0, 0);

        var result = BookingRules.IntervalsOverlap(startA, endA, startB, endB);

        result.Should().BeFalse();
    }

    [Fact]
    public void IntervalsOverlap_WhenOverlapping_ReturnsTrue()
    {
        var startA = new DateTime(2024, 1, 1, 9, 0, 0);
        var endA = new DateTime(2024, 1, 1, 11, 0, 0);
        var startB = new DateTime(2024, 1, 1, 10, 0, 0);
        var endB = new DateTime(2024, 1, 1, 12, 0, 0);

        var result = BookingRules.IntervalsOverlap(startA, endA, startB, endB);

        result.Should().BeTrue();
    }

    [Fact]
    public void IntervalsOverlap_WhenTouchingAtEnd_ReturnsFalse()
    {
        // [10:00-11:00] vs [11:00-12:00] => NOT overlap
        var startA = new DateTime(2024, 1, 1, 10, 0, 0);
        var endA = new DateTime(2024, 1, 1, 11, 0, 0);
        var startB = new DateTime(2024, 1, 1, 11, 0, 0);
        var endB = new DateTime(2024, 1, 1, 12, 0, 0);

        var result = BookingRules.IntervalsOverlap(startA, endA, startB, endB);

        result.Should().BeFalse();
    }

    [Fact]
    public void IntervalsOverlap_WhenTouchingAtStart_ReturnsFalse()
    {
        // [10:00-11:00] vs [09:00-10:00] => NOT overlap
        var startA = new DateTime(2024, 1, 1, 10, 0, 0);
        var endA = new DateTime(2024, 1, 1, 11, 0, 0);
        var startB = new DateTime(2024, 1, 1, 9, 0, 0);
        var endB = new DateTime(2024, 1, 1, 10, 0, 0);

        var result = BookingRules.IntervalsOverlap(startA, endA, startB, endB);

        result.Should().BeFalse();
    }

    [Fact]
    public void IntervalsOverlap_WhenNewContainedInExisting_ReturnsTrue()
    {
        // [10:00-11:00] vs [10:30-10:45] => overlap
        var startA = new DateTime(2024, 1, 1, 10, 0, 0);
        var endA = new DateTime(2024, 1, 1, 11, 0, 0);
        var startB = new DateTime(2024, 1, 1, 10, 30, 0);
        var endB = new DateTime(2024, 1, 1, 10, 45, 0);

        var result = BookingRules.IntervalsOverlap(startA, endA, startB, endB);

        result.Should().BeTrue();
    }

    [Fact]
    public void IntervalsOverlap_WhenOneContainsOther_ReturnsTrue()
    {
        var startA = new DateTime(2024, 1, 1, 9, 0, 0);
        var endA = new DateTime(2024, 1, 1, 14, 0, 0);
        var startB = new DateTime(2024, 1, 1, 10, 0, 0);
        var endB = new DateTime(2024, 1, 1, 12, 0, 0);

        var result = BookingRules.IntervalsOverlap(startA, endA, startB, endB);

        result.Should().BeTrue();
    }

    #endregion

    #region HasTimeConflict

    [Fact]
    public void HasTimeConflict_WhenNoReservations_ReturnsFalse()
    {
        var reservations = new List<Reservation>();
        var newStart = new DateTime(2024, 1, 1, 9, 0, 0);
        var newEnd = new DateTime(2024, 1, 1, 10, 0, 0);

        var result = BookingRules.HasTimeConflict(reservations, newStart, newEnd);

        result.Should().BeFalse();
    }

    [Fact]
    public void HasTimeConflict_WhenConflictExists_ReturnsTrue()
    {
        var reservations = new List<Reservation>
        {
            new() { Id = 1, Start = new DateTime(2024, 1, 1, 9, 0, 0), End = new DateTime(2024, 1, 1, 11, 0, 0) }
        };
        var newStart = new DateTime(2024, 1, 1, 10, 0, 0);
        var newEnd = new DateTime(2024, 1, 1, 12, 0, 0);

        var result = BookingRules.HasTimeConflict(reservations, newStart, newEnd);

        result.Should().BeTrue();
    }

    [Fact]
    public void HasTimeConflict_WhenCancelled_IgnoresReservation()
    {
        var reservations = new List<Reservation>
        {
            new() { Id = 1, Start = new DateTime(2024, 1, 1, 9, 0, 0), End = new DateTime(2024, 1, 1, 11, 0, 0), IsCancelled = true }
        };
        var newStart = new DateTime(2024, 1, 1, 10, 0, 0);
        var newEnd = new DateTime(2024, 1, 1, 12, 0, 0);

        var result = BookingRules.HasTimeConflict(reservations, newStart, newEnd);

        result.Should().BeFalse();
    }

    [Fact]
    public void HasTimeConflict_WhenEditingSameReservation_IgnoresIt()
    {
        var reservations = new List<Reservation>
        {
            new() { Id = 5, Start = new DateTime(2024, 1, 1, 9, 0, 0), End = new DateTime(2024, 1, 1, 11, 0, 0) }
        };
        var newStart = new DateTime(2024, 1, 1, 9, 30, 0);
        var newEnd = new DateTime(2024, 1, 1, 11, 30, 0);

        var result = BookingRules.HasTimeConflict(reservations, newStart, newEnd, ignoreReservationId: 5);

        result.Should().BeFalse();
    }

    [Fact]
    public void HasTimeConflict_WhenEditingDifferentReservation_DetectsConflict()
    {
        var reservations = new List<Reservation>
        {
            new() { Id = 5, Start = new DateTime(2024, 1, 1, 9, 0, 0), End = new DateTime(2024, 1, 1, 11, 0, 0) },
            new() { Id = 6, Start = new DateTime(2024, 1, 1, 14, 0, 0), End = new DateTime(2024, 1, 1, 15, 0, 0) }
        };
        var newStart = new DateTime(2024, 1, 1, 10, 0, 0);
        var newEnd = new DateTime(2024, 1, 1, 12, 0, 0);

        var result = BookingRules.HasTimeConflict(reservations, newStart, newEnd, ignoreReservationId: 6);

        result.Should().BeTrue();
    }

    #endregion

    #region ValidateTimeRange

    [Fact]
    public void ValidateTimeRange_WhenValid_ReturnsNoErrors()
    {
        var startTime = new TimeSpan(9, 0, 0);
        var endTime = new TimeSpan(10, 0, 0);
        var startDateTime = DateTime.Now.AddDays(1).Date.Add(startTime);
        var endDateTime = DateTime.Now.AddDays(1).Date.Add(endTime);

        var errors = BookingRules.ValidateTimeRange(startTime, endTime, startDateTime, endDateTime).ToList();

        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateTimeRange_WhenOutsideBusinessHours_ReturnsError()
    {
        var startTime = new TimeSpan(7, 0, 0);
        var endTime = new TimeSpan(8, 0, 0);
        var startDateTime = DateTime.Now.AddDays(1).Date.Add(startTime);
        var endDateTime = DateTime.Now.AddDays(1).Date.Add(endTime);

        var errors = BookingRules.ValidateTimeRange(startTime, endTime, startDateTime, endDateTime).ToList();

        errors.Should().Contain(e => e.MemberNames.Contains("StartTime"));
    }

    [Fact]
    public void ValidateTimeRange_WhenDurationTooLong_ReturnsError()
    {
        var startTime = new TimeSpan(8, 0, 0);
        var endTime = new TimeSpan(18, 0, 0);
        var startDateTime = DateTime.Now.AddDays(1).Date.Add(startTime);
        var endDateTime = DateTime.Now.AddDays(1).Date.Add(endTime);

        var errors = BookingRules.ValidateTimeRange(startTime, endTime, startDateTime, endDateTime).ToList();

        errors.Should().Contain(e => e.MemberNames.Contains("EndTime"));
    }

    [Fact]
    public void ValidateTimeRange_WhenEndBeforeStart_ReturnsError()
    {
        var startTime = new TimeSpan(12, 0, 0);
        var endTime = new TimeSpan(10, 0, 0);
        var startDateTime = DateTime.Now.AddDays(1).Date.Add(startTime);
        var endDateTime = DateTime.Now.AddDays(1).Date.Add(endTime);

        var errors = BookingRules.ValidateTimeRange(startTime, endTime, startDateTime, endDateTime).ToList();

        errors.Should().Contain(e => e.MemberNames.Contains("EndTime"));
    }

    [Fact]
    public void ValidateTimeRange_WhenEndEqualsStart_ReturnsError()
    {
        var startTime = new TimeSpan(10, 0, 0);
        var endTime = new TimeSpan(10, 0, 0);
        var startDateTime = DateTime.Now.AddDays(1).Date.Add(startTime);
        var endDateTime = DateTime.Now.AddDays(1).Date.Add(endTime);

        var errors = BookingRules.ValidateTimeRange(startTime, endTime, startDateTime, endDateTime).ToList();

        errors.Should().Contain(e => e.MemberNames.Contains("EndTime") || e.MemberNames.Contains("EndDate"));
    }

    [Fact]
    public void ValidateTimeRange_WhenStartInPast_ReturnsError()
    {
        var startTime = new TimeSpan(10, 0, 0);
        var endTime = new TimeSpan(11, 0, 0);
        var startDateTime = DateTime.Now.AddDays(-1).Date.Add(startTime);
        var endDateTime = DateTime.Now.AddDays(-1).Date.Add(endTime);

        var errors = BookingRules.ValidateTimeRange(startTime, endTime, startDateTime, endDateTime).ToList();

        errors.Should().Contain(e => e.MemberNames.Contains("StartDate") || e.MemberNames.Contains("StartTime"));
    }

    [Fact]
    public void ValidateTimeRange_WhenStartInPastButAllowed_ReturnsNoError()
    {
        var startTime = new TimeSpan(10, 0, 0);
        var endTime = new TimeSpan(11, 0, 0);
        var startDateTime = DateTime.Now.AddDays(-1).Date.Add(startTime);
        var endDateTime = DateTime.Now.AddDays(-1).Date.Add(endTime);

        var errors = BookingRules.ValidateTimeRange(startTime, endTime, startDateTime, endDateTime, allowPastBookings: true).ToList();

        errors.Should().NotContain(e => e.MemberNames.Contains("StartDate") || e.MemberNames.Contains("StartTime"));
    }

    #endregion

    #region GetDefaultTimeSlot

    [Fact]
    public void GetDefaultTimeSlot_ReturnsOneHourSlot()
    {
        var (start, end) = BookingRules.GetDefaultTimeSlot();

        (end - start).Should().Be(TimeSpan.FromHours(1));
    }

    [Fact]
    public void GetDefaultTimeSlot_ReturnsWithinBusinessHours()
    {
        var (start, end) = BookingRules.GetDefaultTimeSlot();

        BookingRules.IsWithinBusinessHours(start.TimeOfDay).Should().BeTrue();
        BookingRules.IsWithinBusinessHours(end.TimeOfDay).Should().BeTrue();
    }

    #endregion
}
