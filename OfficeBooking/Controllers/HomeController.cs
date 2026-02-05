using Microsoft.AspNetCore.Mvc;
using OfficeBooking.Business;
using OfficeBooking.Services;
using OfficeBooking.ViewModels;

namespace OfficeBooking.Controllers;

public class HomeController : Controller
{
    private readonly IRoomService _roomService;
    private readonly TimeProvider _timeProvider;

    public HomeController(IRoomService roomService, TimeProvider timeProvider)
    {
        _roomService = roomService;
        _timeProvider = timeProvider;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var now = _timeProvider.GetLocalNow().DateTime;
        var (start, end) = BookingRules.GetDefaultTimeSlot(now);

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
