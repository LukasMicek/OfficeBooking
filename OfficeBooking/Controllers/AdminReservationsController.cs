using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfficeBooking.Services;
using OfficeBooking.ViewModels;

namespace OfficeBooking.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminReservationsController : Controller
    {
        private readonly IAdminReservationService _adminReservationService;

        public AdminReservationsController(IAdminReservationService adminReservationService)
        {
            _adminReservationService = adminReservationService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(bool activeOnly = false)
        {
            var list = await _adminReservationService.GetListAsync(activeOnly);
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Cancel(int id)
        {
            var vm = await _adminReservationService.GetCancelViewModelAsync(id);
            if (vm == null)
            {
                TempData["Error"] = "Ta rezerwacja nie istnieje lub jest już anulowana.";
                return RedirectToAction(nameof(Index));
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(AdminCancelReservationViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var result = await _adminReservationService.CancelAsync(vm.Id, vm.CancelReason, DateTime.Now);

            switch (result.Status)
            {
                case AdminCancelStatus.NotFound:
                    return NotFound();

                case AdminCancelStatus.AlreadyCancelled:
                    TempData["Error"] = "Ta rezerwacja jest już anulowana.";
                    return RedirectToAction(nameof(Index));

                case AdminCancelStatus.Success:
                default:
                    TempData["Success"] = "Rezerwacja została anulowana.";
                    return RedirectToAction(nameof(Index));
            }
        }
    }
}
