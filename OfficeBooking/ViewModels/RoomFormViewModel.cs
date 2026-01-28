using OfficeBooking.Models;
using System.ComponentModel.DataAnnotations;

namespace OfficeBooking.ViewModels
{
    public class RoomFormViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(80)]
        [Display(Name = "Nazwa")]
        public string Name { get; set; } = string.Empty;

        [Range(1, 500)]
        [Display(Name = "Pojemność")]
        public int Capacity { get; set; }

        public List<int> SelectedEquipmentIds { get; set; } = new();

        public List<Equipment> AllEquipments { get; set; } = new();
    }
}
