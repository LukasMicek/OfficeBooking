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

            // Indexes for query performance

            // Reservations: overlap queries filter by RoomId + IsCancelled, then check Start/End range
            builder.Entity<Reservation>()
                .HasIndex(r => new { r.RoomId, r.IsCancelled, r.Start, r.End })
                .HasDatabaseName("IX_Reservations_RoomId_IsCancelled_Start_End");

            // RoomEquipments: support "find rooms with equipment X" lookups
            builder.Entity<RoomEquipment>()
                .HasIndex(re => re.EquipmentId)
                .HasDatabaseName("IX_RoomEquipments_EquipmentId");

            // Rooms: support ORDER BY Name
            builder.Entity<Room>()
                .HasIndex(r => r.Name)
                .HasDatabaseName("IX_Rooms_Name");

        }

    }
}
