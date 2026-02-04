using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OfficeBooking.Migrations
{
    /// <inheritdoc />
    public partial class AddQueryPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reservations_RoomId_Start_End",
                table: "Reservations");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_Name",
                table: "Rooms",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_RoomId_IsCancelled_Start_End",
                table: "Reservations",
                columns: new[] { "RoomId", "IsCancelled", "Start", "End" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Rooms_Name",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_RoomId_IsCancelled_Start_End",
                table: "Reservations");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_RoomId_Start_End",
                table: "Reservations",
                columns: new[] { "RoomId", "Start", "End" });
        }
    }
}
