using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using OfficeBooking.Business;
using OfficeBooking.Models;

namespace OfficeBooking.ViewModels;

public class RoomSearchViewModel : IValidatableObject
{
    [Display(Name = "Data rozpoczęcia")]
    [DataType(DataType.Date)]
    [Required]
    public DateTime StartDate { get; set; }

    [Display(Name = "Godzina rozpoczęcia")]
    [DataType(DataType.Time)]
    [Required]
    public TimeSpan StartTime { get; set; }

    [Display(Name = "Data zakończenia")]
    [DataType(DataType.Date)]
    [Required]
    public DateTime EndDate { get; set; }

    [Display(Name = "Godzina zakończenia")]
    [DataType(DataType.Time)]
    [Required]
    public TimeSpan EndTime { get; set; }

    [Display(Name = "Minimalna pojemność")]
    [Range(1, 500)]
    public int RequiredCapacity { get; set; } = 1;

    [Display(Name = "Wymagane wyposażenie")]
    public List<int> EquipmentIds { get; set; } = new();

    [ValidateNever]
    public List<Equipment> AllEquipments { get; set; } = new();

    [ValidateNever]
    public List<Room>? Results { get; set; }

    public DateTime StartDateTime => StartDate.Date + StartTime;
    public DateTime EndDateTime => EndDate.Date + EndTime;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        return BookingRules.ValidateTimeRange(
            StartTime,
            EndTime,
            StartDateTime,
            EndDateTime,
            allowPastBookings: false
        );
    }
}
