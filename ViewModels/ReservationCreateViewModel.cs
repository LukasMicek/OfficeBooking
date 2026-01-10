using System.ComponentModel.DataAnnotations;

namespace OfficeBooking.ViewModels
{
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
            var workStart = new TimeSpan(8, 0, 0);
            var workEnd = new TimeSpan(20, 0, 0);

            if (StartTime < workStart || StartTime > workEnd)
            {
                yield return new ValidationResult(
                    "Godzina rozpoczęcia musi być w godzinach pracy (08:00–20:00).",
                    new[] { nameof(StartTime) }
                );
            }

            if (EndTime < workStart || EndTime > workEnd)
            {
                yield return new ValidationResult(
                    "Godzina zakończenia musi być w godzinach pracy (08:00–20:00).",
                    new[] { nameof(EndTime) }
                );
            }

            if (EndDateTime <= StartDateTime)
            {
                yield return new ValidationResult(
                    "Data i godzina zakończenia muszą być późniejsze niż rozpoczęcia.",
                    new[] { nameof(EndDate), nameof(EndTime) }
                );
            }

            if (StartDateTime < DateTime.Now)
            {
                yield return new ValidationResult(
                    "Nie można utworzyć rezerwacji w przeszłości.",
                    new[] { nameof(StartDate), nameof(StartTime) }
                );
            }

            var maxDuration = TimeSpan.FromHours(8);
            var duration = EndDateTime - StartDateTime;

            if (duration > maxDuration)
            {
                yield return new ValidationResult(
                    "Maksymalny czas rezerwacji to 8 godzin.",
                    new[] { nameof(EndDate), nameof(EndTime) }
                );
            }


        }
    }
}
