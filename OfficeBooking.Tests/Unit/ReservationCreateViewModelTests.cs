using System.ComponentModel.DataAnnotations;
using OfficeBooking.ViewModels;

namespace OfficeBooking.Tests.Unit;

public class ReservationCreateViewModelTests
{
    [Fact]
    public void Validate_WhenStartDateInPast_ReturnsError()
    {
        var viewModel = new ReservationCreateViewModel
        {
            RoomId = 1,
            Title = "Test Meeting",
            AttendeesCount = 5,
            StartDate = DateTime.Today.AddDays(-1),
            StartTime = new TimeSpan(10, 0, 0),
            EndDate = DateTime.Today.AddDays(-1),
            EndTime = new TimeSpan(11, 0, 0)
        };

        var context = new ValidationContext(viewModel);
        var results = viewModel.Validate(context).ToList();

        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.MemberNames.Contains("StartDate") || r.MemberNames.Contains("StartTime"));
    }

    [Fact]
    public void Validate_WhenFutureDate_ReturnsNoErrors()
    {
        var viewModel = new ReservationCreateViewModel
        {
            RoomId = 1,
            Title = "Test Meeting",
            AttendeesCount = 5,
            StartDate = DateTime.Today.AddDays(1),
            StartTime = new TimeSpan(10, 0, 0),
            EndDate = DateTime.Today.AddDays(1),
            EndTime = new TimeSpan(11, 0, 0)
        };

        var context = new ValidationContext(viewModel);
        var results = viewModel.Validate(context).ToList();

        Assert.Empty(results);
    }
}
