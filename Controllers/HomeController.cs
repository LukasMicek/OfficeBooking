using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeBooking.Data;
using OfficeBooking.ViewModels;

namespace OfficeBooking.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var now = DateTime.Now;

            // start: za godzinê, zaokr¹glone do pe³nej godziny
            var start = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0).AddHours(1);
            var end = start.AddHours(1);

            // je¿eli wypadamy poza godziny pracy, to ustaw na jutro 09:00–10:00
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

                AllEquipments = await _context.Equipments
                    .OrderBy(e => e.Name)
                    .ToListAsync()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(RoomSearchViewModel vm)
        {
            vm.AllEquipments = await _context.Equipments
                .OrderBy(e => e.Name)
                .ToListAsync();

            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var start = vm.StartDateTime;
            var end = vm.EndDateTime;

            var query = _context.Rooms
                .Include(r => r.RoomEquipments).ThenInclude(re => re.Equipment)
                .Include(r => r.Reservations)
                .AsQueryable();

            query = query.Where(r => r.Capacity >= vm.RequiredCapacity);

            if (vm.EquipmentIds != null && vm.EquipmentIds.Count > 0)
            {
                query = query.Where(r => vm.EquipmentIds.All(eqId =>
                    r.RoomEquipments.Any(re => re.EquipmentId == eqId)));
            }

            query = query.Where(r => !r.Reservations.Any(res =>
                !res.IsCancelled &&
                start < res.End && end > res.Start));

            vm.Results = await query
                .OrderBy(r => r.Name)
                .ToListAsync();

            return View(vm);
        }
    }
}
