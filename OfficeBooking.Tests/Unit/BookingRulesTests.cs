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

        Assert.Equal(expected, result);
    }

    [Fact]
    public void IntervalsOverlap_WhenNoOverlap_ReturnsFalse()
    {
        var startA = new DateTime(2024, 1, 1, 9, 0, 0);
        var endA = new DateTime(2024, 1, 1, 10, 0, 0);
        var startB = new DateTime(2024, 1, 1, 11, 0, 0);
        var endB = new DateTime(2024, 1, 1, 12, 0, 0);

        var result = BookingRules.IntervalsOverlap(startA, endA, startB, endB);

        Assert.False(result);
    }

    [Fact]
    public void IntervalsOverlap_WhenOverlapping_ReturnsTrue()
    {
        var startA = new DateTime(2024, 1, 1, 9, 0, 0);
        var endA = new DateTime(2024, 1, 1, 11, 0, 0);
        var startB = new DateTime(2024, 1, 1, 10, 0, 0);
        var endB = new DateTime(2024, 1, 1, 12, 0, 0);

        var result = BookingRules.IntervalsOverlap(startA, endA, startB, endB);

        Assert.True(result);
    }

    [Fact]
    public void IntervalsOverlap_WhenTouching_ReturnsFalse()
    {
        var startA = new DateTime(2024, 1, 1, 9, 0, 0);
        var endA = new DateTime(2024, 1, 1, 10, 0, 0);
        var startB = new DateTime(2024, 1, 1, 10, 0, 0);
        var endB = new DateTime(2024, 1, 1, 11, 0, 0);

        var result = BookingRules.IntervalsOverlap(startA, endA, startB, endB);

        Assert.False(result);
    }

    [Fact]
    public void IntervalsOverlap_WhenOneContainsOther_ReturnsTrue()
    {
        var startA = new DateTime(2024, 1, 1, 9, 0, 0);
        var endA = new DateTime(2024, 1, 1, 14, 0, 0);
        var startB = new DateTime(2024, 1, 1, 10, 0, 0);
        var endB = new DateTime(2024, 1, 1, 12, 0, 0);

        var result = BookingRules.IntervalsOverlap(startA, endA, startB, endB);

        Assert.True(result);
    }

    [Fact]
    public void HasTimeConflict_WhenNoReservations_ReturnsFalse()
    {
        var reservations = new List<Reservation>();
        var newStart = new DateTime(2024, 1, 1, 9, 0, 0);
        var newEnd = new DateTime(2024, 1, 1, 10, 0, 0);

        var result = BookingRules.HasTimeConflict(reservations, newStart, newEnd);

        Assert.False(result);
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

        Assert.True(result);
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

        Assert.False(result);
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

        Assert.False(result);
    }

    [Fact]
    public void ValidateTimeRange_WhenValid_ReturnsNoErrors()
    {
        var startTime = new TimeSpan(9, 0, 0);
        var endTime = new TimeSpan(10, 0, 0);
        var startDateTime = DateTime.Now.AddDays(1).Date.Add(startTime);
        var endDateTime = DateTime.Now.AddDays(1).Date.Add(endTime);

        var errors = BookingRules.ValidateTimeRange(startTime, endTime, startDateTime, endDateTime).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateTimeRange_WhenOutsideBusinessHours_ReturnsError()
    {
        var startTime = new TimeSpan(7, 0, 0); 
        var endTime = new TimeSpan(8, 0, 0);
        var startDateTime = DateTime.Now.AddDays(1).Date.Add(startTime);
        var endDateTime = DateTime.Now.AddDays(1).Date.Add(endTime);

        var errors = BookingRules.ValidateTimeRange(startTime, endTime, startDateTime, endDateTime).ToList();

        Assert.Contains(errors, e => e.MemberNames.Contains("StartTime"));
    }

    [Fact]
    public void ValidateTimeRange_WhenDurationTooLong_ReturnsError()
    {
        var startTime = new TimeSpan(8, 0, 0);
        var endTime = new TimeSpan(18, 0, 0); 
        var startDateTime = DateTime.Now.AddDays(1).Date.Add(startTime);
        var endDateTime = DateTime.Now.AddDays(1).Date.Add(endTime);

        var errors = BookingRules.ValidateTimeRange(startTime, endTime, startDateTime, endDateTime).ToList();

        Assert.Contains(errors, e => e.MemberNames.Contains("EndTime"));
    }

    [Fact]
    public void ValidateTimeRange_WhenEndBeforeStart_ReturnsError()
    {
        var startTime = new TimeSpan(12, 0, 0);
        var endTime = new TimeSpan(10, 0, 0); 
        var startDateTime = DateTime.Now.AddDays(1).Date.Add(startTime);
        var endDateTime = DateTime.Now.AddDays(1).Date.Add(endTime);

        var errors = BookingRules.ValidateTimeRange(startTime, endTime, startDateTime, endDateTime).ToList();

        Assert.Contains(errors, e => e.MemberNames.Contains("EndTime"));
    }

    [Fact]
    public void GetDefaultTimeSlot_ReturnsOneHourSlot()
    {
        var (start, end) = BookingRules.GetDefaultTimeSlot();

        Assert.Equal(TimeSpan.FromHours(1), end - start);
    }

    [Fact]
    public void GetDefaultTimeSlot_ReturnsWithinBusinessHours()
    {
        var (start, end) = BookingRules.GetDefaultTimeSlot();

        Assert.True(BookingRules.IsWithinBusinessHours(start.TimeOfDay));
        Assert.True(BookingRules.IsWithinBusinessHours(end.TimeOfDay));
    }
}
