using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfficeBooking.Services;

namespace OfficeBooking.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminReportsController : Controller
    {
        private readonly IRoomUsageReportService _roomUsageReportService;
        private readonly TimeProvider _timeProvider;

        public AdminReportsController(
            IRoomUsageReportService roomUsageReportService,
            TimeProvider timeProvider)
        {
            _roomUsageReportService = roomUsageReportService;
            _timeProvider = timeProvider;
        }

        [HttpGet]
        public async Task<IActionResult> RoomUsage(int? year, int? month)
        {
            var today = DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);
            var vm = await _roomUsageReportService.GetReportAsync(year, month, today.ToDateTime(TimeOnly.MinValue));
            return View(vm);
        }
    }
}
