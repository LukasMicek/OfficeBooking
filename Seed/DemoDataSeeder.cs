using Microsoft.EntityFrameworkCore;
using OfficeBooking.Data;
using OfficeBooking.Models;

namespace OfficeBooking.Seed
{
    public static class DemoDataSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Jeśli są już sale, to nic nie rób
            if (await context.Rooms.AnyAsync())
                return;

            // Wyposażenie
            var equipments = new List<Equipment>
            {
                new Equipment { Name = "Rzutnik" },
                new Equipment { Name = "TV" },
                new Equipment { Name = "Tablica" },
                new Equipment { Name = "Wideokonferencja" },
                new Equipment { Name = "Głośniki" },
            };

            context.Equipments.AddRange(equipments);

            // Sale
            var rooms = new List<Room>
            {
                new Room { Name = "Sala A1", Capacity = 6 },
                new Room { Name = "Sala B2", Capacity = 10 },
                new Room { Name = "Sala C3", Capacity = 16 },
                new Room { Name = "Sala D4", Capacity = 25 },
                new Room { Name = "Sala E5", Capacity = 40 }
            };

            context.Rooms.AddRange(rooms);
            await context.SaveChangesAsync();

            // Przypisania M:N (RoomEquipment)
            // Pobieramy id po zapisie
            var eq = await context.Equipments.OrderBy(e => e.Name).ToListAsync();
            var rm = await context.Rooms.OrderBy(r => r.Name).ToListAsync();

            // Pomocniczo: znajdź po nazwie
            int EqId(string name) => eq.First(e => e.Name == name).Id;
            int RoomId(string name) => rm.First(r => r.Name == name).Id;

            var links = new List<RoomEquipment>
            {
                // Sala A1
                new RoomEquipment { RoomId = RoomId("Sala A1"), EquipmentId = EqId("TV") },
                new RoomEquipment { RoomId = RoomId("Sala A1"), EquipmentId = EqId("Tablica") },

                // Sala B2
                new RoomEquipment { RoomId = RoomId("Sala B2"), EquipmentId = EqId("Rzutnik") },
                new RoomEquipment { RoomId = RoomId("Sala B2"), EquipmentId = EqId("Tablica") },

                // Sala C3
                new RoomEquipment { RoomId = RoomId("Sala C3"), EquipmentId = EqId("Rzutnik") },
                new RoomEquipment { RoomId = RoomId("Sala C3"), EquipmentId = EqId("Wideokonferencja") },
                new RoomEquipment { RoomId = RoomId("Sala C3"), EquipmentId = EqId("Głośniki") },

                // Sala D4
                new RoomEquipment { RoomId = RoomId("Sala D4"), EquipmentId = EqId("Rzutnik") },
                new RoomEquipment { RoomId = RoomId("Sala D4"), EquipmentId = EqId("TV") },
                new RoomEquipment { RoomId = RoomId("Sala D4"), EquipmentId = EqId("Wideokonferencja") },
                new RoomEquipment { RoomId = RoomId("Sala D4"), EquipmentId = EqId("Głośniki") },

                // Sala E5
                new RoomEquipment { RoomId = RoomId("Sala E5"), EquipmentId = EqId("Rzutnik") },
                new RoomEquipment { RoomId = RoomId("Sala E5"), EquipmentId = EqId("TV") },
                new RoomEquipment { RoomId = RoomId("Sala E5"), EquipmentId = EqId("Wideokonferencja") },
                new RoomEquipment { RoomId = RoomId("Sala E5"), EquipmentId = EqId("Głośniki") }
            };

            context.RoomEquipments.AddRange(links);
            await context.SaveChangesAsync();
        }
    }
}
