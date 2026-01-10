namespace OfficeBooking.ViewModels
{
    public class AdminReservationRowViewModel
    {
        public int Id { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;

        public DateTime Start { get; set; }
        public DateTime End { get; set; }

        public int AttendeesCount { get; set; }
        public string UserEmail { get; set; } = string.Empty;

        public bool IsCancelled { get; set; }
    }
}
