using System.ComponentModel.DataAnnotations;
using OfficeBooking.Business;

namespace OfficeBooking.ViewModels;

public class ReservationCreateViewModel : IValidatableObject
{
    [Required]
    public int RoomId { get; set; }

    public string RoomName { get; set; } = string.Empty;

    public int RoomCapacity { get; set; }

    [Display(Name = "Title")]
    [Required]
    [StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Number of attendees")]
    [Range(1, 500)]
    public int AttendeesCount { get; set; } = 1;

    [Display(Name = "Notes")]
    [StringLength(500)]
    public string? Notes { get; set; }

    [Display(Name = "Start date")]
    [DataType(DataType.Date)]
    [Required]
    public DateTime StartDate { get; set; }

    [Display(Name = "Start time")]
    [DataType(DataType.Time)]
    [Required]
    public TimeSpan StartTime { get; set; } = new TimeSpan(9, 0, 0);

    [Display(Name = "End date")]
    [DataType(DataType.Date)]
    [Required]
    public DateTime EndDate { get; set; }

    [Display(Name = "End time")]
    [DataType(DataType.Time)]
    [Required]
    public TimeSpan EndTime { get; set; } = new TimeSpan(10, 0, 0);

    public DateTime StartDateTime => StartDate.Date + StartTime;
    public DateTime EndDateTime => EndDate.Date + EndTime;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var timeProvider = validationContext.GetService(typeof(TimeProvider)) as TimeProvider
                           ?? TimeProvider.System;
        var now = timeProvider.GetLocalNow().DateTime;

        return BookingRules.ValidateTimeRange(
            StartTime,
            EndTime,
            StartDateTime,
            EndDateTime,
            now,
            allowPastBookings: false
        );
    }
}
