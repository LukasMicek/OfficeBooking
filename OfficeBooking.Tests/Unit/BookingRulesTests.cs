using FluentAssertions;
using OfficeBooking.Business;

namespace OfficeBooking.Tests.Unit;

public class BookingRulesTests
{
    [Theory]
    [InlineData(8, 0, true)]   // Business start
    [InlineData(12, 0, true)]  // Mid-day
    [InlineData(20, 0, true)]  // Business end
    [InlineData(7, 59, false)] // Just before opening
    [InlineData(20, 1, false)] // Just after closing
    public void IsWithinBusinessHours_ReturnsCorrectResult(int hour, int minute, bool expected)
    {
        var time = new TimeSpan(hour, minute, 0);

        var result = BookingRules.IsWithinBusinessHours(time);

        result.Should().Be(expected);
    }

    #region ValidateTimeRangeForService

    [Fact]
    public void ValidateTimeRangeForService_WhenValid_ReturnsNull()
    {
        var now = new DateTime(2026, 1, 15, 9, 0, 0);
        var start = new DateTime(2026, 1, 16, 10, 0, 0);
        var end = new DateTime(2026, 1, 16, 11, 0, 0);

        var result = BookingRules.ValidateTimeRangeForService(start, end, now);

        result.Should().BeNull();
    }

    [Fact]
    public void ValidateTimeRangeForService_WhenEndBeforeOrEqualsStart_ReturnsError()
    {
        var now = new DateTime(2026, 1, 15, 9, 0, 0);
        var time = new DateTime(2026, 1, 16, 10, 0, 0);

        var resultEqual = BookingRules.ValidateTimeRangeForService(time, time, now);
        var resultBefore = BookingRules.ValidateTimeRangeForService(time.AddHours(1), time, now);

        resultEqual.Should().Contain("zakończenia");
        resultBefore.Should().Contain("zakończenia");
    }

    [Fact]
    public void ValidateTimeRangeForService_WhenStartInPast_ReturnsError()
    {
        var now = new DateTime(2026, 1, 15, 9, 0, 0);

        var result = BookingRules.ValidateTimeRangeForService(
            new DateTime(2026, 1, 14, 10, 0, 0),
            new DateTime(2026, 1, 14, 11, 0, 0),
            now);

        result.Should().Contain("przeszłości");
    }

    [Fact]
    public void ValidateTimeRangeForService_WhenOutsideBusinessHours_ReturnsError()
    {
        var now = new DateTime(2026, 1, 15, 9, 0, 0);

        var result = BookingRules.ValidateTimeRangeForService(
            new DateTime(2026, 1, 16, 6, 0, 0),
            new DateTime(2026, 1, 16, 7, 0, 0),
            now);

        result.Should().Contain("godzinach pracy");
    }

    [Fact]
    public void ValidateTimeRangeForService_WhenDurationExceedsMax_ReturnsError()
    {
        var now = new DateTime(2026, 1, 15, 9, 0, 0);

        var result = BookingRules.ValidateTimeRangeForService(
            new DateTime(2026, 1, 16, 8, 0, 0),
            new DateTime(2026, 1, 16, 18, 0, 0),
            now);

        result.Should().Contain("8");
    }

    #endregion

    #region GetDefaultTimeSlot

    [Fact]
    public void GetDefaultTimeSlot_ReturnsOneHourSlotWithinBusinessHours()
    {
        var (start, end) = BookingRules.GetDefaultTimeSlot();

        (end - start).Should().Be(TimeSpan.FromHours(1));
        BookingRules.IsWithinBusinessHours(start.TimeOfDay).Should().BeTrue();
        BookingRules.IsWithinBusinessHours(end.TimeOfDay).Should().BeTrue();
    }

    #endregion
}
