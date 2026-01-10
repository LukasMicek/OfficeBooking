using OfficeBooking.Models;
using System.ComponentModel.DataAnnotations;

namespace OfficeBooking.ViewModels
{
    public class RoomFormViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(80)]
        public string Name { get; set; } = string.Empty;

        [Range(1, 500)]
        public int Capacity { get; set; }

        // checkbox
        public List<int> SelectedEquipmentIds { get; set; } = new();

        // show checkbox
        public List<Equipment> AllEquipments { get; set; } = new();
    }
}
