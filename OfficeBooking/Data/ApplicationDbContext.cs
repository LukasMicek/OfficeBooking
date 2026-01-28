using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OfficeBooking.Models;

namespace OfficeBooking.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {

        }

        public DbSet<Room> Rooms => Set<Room>();
        public DbSet<Equipment> Equipments => Set<Equipment>();
        public DbSet<RoomEquipment> RoomEquipments => Set<RoomEquipment>();
        public DbSet<Reservation> Reservations => Set<Reservation>();


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<RoomEquipment>()
                .HasKey(x => new { x.RoomId, x.EquipmentId });

            builder.Entity<RoomEquipment>()
                .HasOne(x => x.Room)
                .WithMany(r => r.RoomEquipments)
                .HasForeignKey(x => x.RoomId);

            builder.Entity<RoomEquipment>()
                .HasOne(x => x.Equipment)
                .WithMany(e => e.RoomEquipments)
                .HasForeignKey(x => x.EquipmentId);

            builder.Entity<Reservation>()
                .HasIndex(r => new { r.RoomId, r.Start, r.End });

        }

    }
}
