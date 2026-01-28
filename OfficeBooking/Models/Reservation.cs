using System.ComponentModel.DataAnnotations;

namespace OfficeBooking.Models
{
    public class Reservation
    {
        public int Id { get; set; }

        [Required]
        public int RoomId { get; set; }
        public Room Room { get; set; } = null!;

        [Required]
        public DateTime Start { get; set; }

        [Required]
        public DateTime End { get; set; }

        [Required]
        [StringLength(120)]
        public string Title { get; set; } = string.Empty;

        [Range(1, 100)]
        public int AttendeesCount { get; set; }


        [StringLength(500)]
        public string? Notes { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public bool IsCancelled { get; set; }

        public DateTime? CancelledAt { get; set; }

        [StringLength(200)]
        public string? CancelReason { get; set; }

    }
}

