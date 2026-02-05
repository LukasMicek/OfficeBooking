using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using OfficeBooking.Business;
using OfficeBooking.Models;

namespace OfficeBooking.ViewModels;

public class RoomSearchViewModel : IValidatableObject
{
    [Display(Name = "Start date")]
    [DataType(DataType.Date)]
    [Required]
    public DateTime StartDate { get; set; }

    [Display(Name = "Start time")]
    [DataType(DataType.Time)]
    [Required]
    public TimeSpan StartTime { get; set; }

    [Display(Name = "End date")]
    [DataType(DataType.Date)]
    [Required]
    public DateTime EndDate { get; set; }

    [Display(Name = "End time")]
    [DataType(DataType.Time)]
    [Required]
    public TimeSpan EndTime { get; set; }

    [Display(Name = "Minimum capacity")]
    [Range(1, 500)]
    public int RequiredCapacity { get; set; } = 1;

    [Display(Name = "Required equipment")]
    public List<int> EquipmentIds { get; set; } = new();

    [ValidateNever]
    public List<Equipment> AllEquipments { get; set; } = new();

    [ValidateNever]
    public List<Room>? Results { get; set; }

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
