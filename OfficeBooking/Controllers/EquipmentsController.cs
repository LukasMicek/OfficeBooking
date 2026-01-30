using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfficeBooking.Models;
using OfficeBooking.Services;

namespace OfficeBooking.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EquipmentsController : Controller
    {
        private readonly IEquipmentService _equipmentService;

        public EquipmentsController(IEquipmentService equipmentService)
        {
            _equipmentService = equipmentService;
        }

        public async Task<IActionResult> Index()
        {
            var equipments = await _equipmentService.GetAllAsync();
            return View(equipments);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var equipment = await _equipmentService.GetByIdAsync(id.Value);
            if (equipment == null)
            {
                return NotFound();
            }

            return View(equipment);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name")] Equipment equipment)
        {
            if (ModelState.IsValid)
            {
                await _equipmentService.CreateAsync(equipment.Name);
                return RedirectToAction(nameof(Index));
            }
            return View(equipment);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var equipment = await _equipmentService.GetByIdAsync(id.Value);
            if (equipment == null)
            {
                return NotFound();
            }
            return View(equipment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] Equipment equipment)
        {
            if (id != equipment.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var result = await _equipmentService.UpdateAsync(id, equipment.Name);
                if (!result.Success)
                {
                    return NotFound();
                }
                return RedirectToAction(nameof(Index));
            }
            return View(equipment);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var equipment = await _equipmentService.GetByIdAsync(id.Value);
            if (equipment == null)
            {
                return NotFound();
            }

            return View(equipment);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _equipmentService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
