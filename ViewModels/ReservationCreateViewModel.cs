using System.ComponentModel.DataAnnotations;
using OfficeBooking.Business;

namespace OfficeBooking.ViewModels;

public class ReservationCreateViewModel : IValidatableObject
{
    [Required]
    public int RoomId { get; set; }

    public string RoomName { get; set; } = string.Empty;

    public int RoomCapacity { get; set; }

    [Display(Name = "Tytuł")]
    [Required]
    [StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Liczba uczestników")]
    [Range(1, 500)]
    public int AttendeesCount { get; set; } = 1;

    [Display(Name = "Notatka")]
    [StringLength(500)]
    public string? Notes { get; set; }

    [Display(Name = "Data rozpoczęcia")]
    [DataType(DataType.Date)]
    [Required]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [Display(Name = "Godzina rozpoczęcia")]
    [DataType(DataType.Time)]
    [Required]
    public TimeSpan StartTime { get; set; } = new TimeSpan(9, 0, 0);

    [Display(Name = "Data zakończenia")]
    [DataType(DataType.Date)]
    [Required]
    public DateTime EndDate { get; set; } = DateTime.Today;

    [Display(Name = "Godzina zakończenia")]
    [DataType(DataType.Time)]
    [Required]
    public TimeSpan EndTime { get; set; } = new TimeSpan(10, 0, 0);

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
