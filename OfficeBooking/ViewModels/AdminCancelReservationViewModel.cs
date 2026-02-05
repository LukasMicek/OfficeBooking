using System.ComponentModel.DataAnnotations;

namespace OfficeBooking.ViewModels
{
    public class AdminCancelReservationViewModel
    {
        public int Id { get; set; }

        public string RoomName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;

        public DateTime Start { get; set; }
        public DateTime End { get; set; }

        public string UserEmail { get; set; } = string.Empty;

        [Display(Name = "Cancellation reason")]
        [Required]
        [StringLength(200)]
        public string CancelReason { get; set; } = string.Empty;
    }
}
