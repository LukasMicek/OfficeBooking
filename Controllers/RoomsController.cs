using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfficeBooking.Services;
using OfficeBooking.ViewModels;

namespace OfficeBooking.Controllers;

[Authorize(Roles = "Admin")]
public class RoomsController : Controller
{
    private readonly IRoomService _roomService;

    public RoomsController(IRoomService roomService)
    {
        _roomService = roomService;
    }

    public async Task<IActionResult> Index()
    {
        var rooms = await _roomService.GetAllAsync();
        return View(rooms);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var room = await _roomService.GetByIdAsync(id.Value);
        if (room == null)
            return NotFound();

        return View(room);
    }

    public async Task<IActionResult> Create()
    {
        var vm = new RoomFormViewModel
        {
            AllEquipments = (await _roomService.GetAllEquipmentAsync()).ToList()
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RoomFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.AllEquipments = (await _roomService.GetAllEquipmentAsync()).ToList();
            return View(vm);
        }

        var request = new CreateRoomRequest(
            vm.Name,
            vm.Capacity,
            vm.SelectedEquipmentIds
        );

        await _roomService.CreateAsync(request);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return NotFound();

        var room = await _roomService.GetByIdWithEquipmentAsync(id.Value);
        if (room == null)
            return NotFound();

        var vm = new RoomFormViewModel
        {
            Id = room.Id,
            Name = room.Name,
            Capacity = room.Capacity,
            SelectedEquipmentIds = room.RoomEquipments.Select(re => re.EquipmentId).ToList(),
            AllEquipments = (await _roomService.GetAllEquipmentAsync()).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, RoomFormViewModel vm)
    {
        if (id != vm.Id)
            return NotFound();

        if (!ModelState.IsValid)
        {
            vm.AllEquipments = (await _roomService.GetAllEquipmentAsync()).ToList();
            return View(vm);
        }

        var request = new UpdateRoomRequest(
            vm.Name,
            vm.Capacity,
            vm.SelectedEquipmentIds
        );

        var result = await _roomService.UpdateAsync(id, request);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            vm.AllEquipments = (await _roomService.GetAllEquipmentAsync()).ToList();
            return View(vm);
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
            return NotFound();

        var room = await _roomService.GetByIdAsync(id.Value);
        if (room == null)
            return NotFound();

        return View(room);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _roomService.DeleteAsync(id);

        if (!result.Success)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Index));
        }

        return RedirectToAction(nameof(Index));
    }
}
