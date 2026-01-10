using System.ComponentModel.DataAnnotations;

namespace OfficeBooking.Models
{
    public class Equipment
    {
        public int Id { get; set; }

        [Required]
        [StringLength(60)]
        public string Name { get; set; } = string.Empty;

        public ICollection<RoomEquipment> RoomEquipments { get; set; } = new List<RoomEquipment>();
    }
}
