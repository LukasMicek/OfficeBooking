using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfficeBooking.Services;

namespace OfficeBooking.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminReportsController : Controller
    {
        private readonly IRoomUsageReportService _roomUsageReportService;

        public AdminReportsController(IRoomUsageReportService roomUsageReportService)
        {
            _roomUsageReportService = roomUsageReportService;
        }

        [HttpGet]
        public async Task<IActionResult> RoomUsage(int? year, int? month)
        {
            var vm = await _roomUsageReportService.GetReportAsync(year, month, DateTime.Today);
            return View(vm);
        }
    }
}
