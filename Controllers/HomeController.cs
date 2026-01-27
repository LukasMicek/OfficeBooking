using Microsoft.AspNetCore.Mvc;
using OfficeBooking.Services;
using OfficeBooking.ViewModels;

namespace OfficeBooking.Controllers;

public class HomeController : Controller
{
    private readonly IRoomService _roomService;

    public HomeController(IRoomService roomService)
    {
        _roomService = roomService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var now = DateTime.Now;

        var start = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0).AddHours(1);
        var end = start.AddHours(1);

        var workStart = new TimeSpan(8, 0, 0);
        var workEnd = new TimeSpan(20, 0, 0);

        if (start.TimeOfDay < workStart || end.TimeOfDay > workEnd)
        {
            var tomorrow = DateTime.Today.AddDays(1);
            start = tomorrow.AddHours(9);
            end = tomorrow.AddHours(10);
        }

        var vm = new RoomSearchViewModel
        {
            StartDate = start.Date,
            StartTime = start.TimeOfDay,
            EndDate = end.Date,
            EndTime = end.TimeOfDay,
            AllEquipments = (await _roomService.GetAllEquipmentAsync()).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(RoomSearchViewModel vm)
    {
        vm.AllEquipments = (await _roomService.GetAllEquipmentAsync()).ToList();

        if (!ModelState.IsValid)
            return View(vm);

        var request = new RoomSearchRequest(
            vm.StartDateTime,
            vm.EndDateTime,
            vm.RequiredCapacity,
            vm.EquipmentIds
        );

        vm.Results = (await _roomService.SearchAvailableAsync(request)).ToList();

        return View(vm);
    }
}
