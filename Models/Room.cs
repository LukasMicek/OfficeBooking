using System.ComponentModel.DataAnnotations;

namespace OfficeBooking.Models
{
    public class Room
    {
        public int Id { get; set; }

        [Required]
        [StringLength(80)]
        public string Name { get; set; } = string.Empty;

        [Range(1, 500)]
        public int Capacity { get; set; }

        public ICollection<RoomEquipment> RoomEquipments { get; set; } = new List<RoomEquipment>();

        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

    }
}
