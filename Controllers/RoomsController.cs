using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeBooking.Data;
using OfficeBooking.Models;
using Microsoft.AspNetCore.Authorization;
using OfficeBooking.ViewModels;


namespace OfficeBooking.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RoomsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RoomsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Rooms
        public async Task<IActionResult> Index()
        {
            var rooms = await _context.Rooms
                .Include(r => r.RoomEquipments)
                    .ThenInclude(re => re.Equipment)
                .OrderBy(r => r.Name)
                .ToListAsync();

            return View(rooms);
        }


        // GET: Rooms/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var room = await _context.Rooms
                .FirstOrDefaultAsync(m => m.Id == id);
            if (room == null)
            {
                return NotFound();
            }

            return View(room);
        }

        // GET: Rooms/Create
        public async Task<IActionResult> Create()
        {
            var vm = new RoomFormViewModel
            {
                AllEquipments = await _context.Equipments
                    .OrderBy(e => e.Name)
                    .ToListAsync()
            };

            return View(vm);
        }


        // POST: Rooms/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RoomFormViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.AllEquipments = await _context.Equipments
                    .OrderBy(e => e.Name)
                    .ToListAsync();

                return View(vm);
            }

            var room = new Room
            {
                Name = vm.Name,
                Capacity = vm.Capacity
            };

            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();

            if (vm.SelectedEquipmentIds != null && vm.SelectedEquipmentIds.Count > 0)
            {
                foreach (var eqId in vm.SelectedEquipmentIds.Distinct())
                {
                    _context.RoomEquipments.Add(new RoomEquipment
                    {
                        RoomId = room.Id,
                        EquipmentId = eqId
                    });
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }


        // GET: Rooms/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var room = await _context.Rooms
                .Include(r => r.RoomEquipments)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null) return NotFound();

            var vm = new RoomFormViewModel
            {
                Id = room.Id,
                Name = room.Name,
                Capacity = room.Capacity,
                SelectedEquipmentIds = room.RoomEquipments.Select(re => re.EquipmentId).ToList(),
                AllEquipments = await _context.Equipments.OrderBy(e => e.Name).ToListAsync()
            };

            return View(vm);
        }


        // POST: Rooms/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RoomFormViewModel vm)
        {
            if (id != vm.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                vm.AllEquipments = await _context.Equipments
                    .OrderBy(e => e.Name)
                    .ToListAsync();

                return View(vm);
            }

            var room = await _context.Rooms
                .Include(r => r.RoomEquipments)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null) return NotFound();

            // Aktualizacja podstawowych pól sali
            room.Name = vm.Name;
            room.Capacity = vm.Capacity;

            // Aktualizacja powiązań wyposażenia
            var selected = (vm.SelectedEquipmentIds ?? new List<int>()).Distinct().ToList();
            var existing = room.RoomEquipments.Select(re => re.EquipmentId).ToList();

            // Usuń te, które były, a nie są zaznaczone
            var toRemove = room.RoomEquipments.Where(re => !selected.Contains(re.EquipmentId)).ToList();
            _context.RoomEquipments.RemoveRange(toRemove);

            // Dodaj te, których nie było
            var toAdd = selected.Where(eqId => !existing.Contains(eqId)).ToList();
            foreach (var eqId in toAdd)
            {
                _context.RoomEquipments.Add(new RoomEquipment
                {
                    RoomId = room.Id,
                    EquipmentId = eqId
                });
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        // GET: Rooms/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var room = await _context.Rooms
                .FirstOrDefaultAsync(m => m.Id == id);
            if (room == null)
            {
                return NotFound();
            }

            return View(room);
        }

        // POST: Rooms/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var hasActiveReservations = await _context.Reservations.AnyAsync(r =>
                r.RoomId == id && !r.IsCancelled);

            if (hasActiveReservations)
            {
                TempData["Error"] = "Nie można usunąć sali, która ma aktywne rezerwacje. Najpierw anuluj rezerwacje tej sali.";
                return RedirectToAction(nameof(Index));
            }

            var links = await _context.RoomEquipments.Where(x => x.RoomId == id).ToListAsync();
            _context.RoomEquipments.RemoveRange(links);

            var room = await _context.Rooms.FindAsync(id);
            if (room != null)
            {
                _context.Rooms.Remove(room);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool RoomExists(int id)
        {
            return _context.Rooms.Any(e => e.Id == id);
        }
    }
}
