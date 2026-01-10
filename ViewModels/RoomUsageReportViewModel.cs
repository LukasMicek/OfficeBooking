namespace OfficeBooking.ViewModels
{
    public class RoomUsageReportRowViewModel
    {
        public int RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public int TotalMinutes { get; set; }
    }

    public class RoomUsageReportViewModel
    {
        public int Year { get; set; }
        public int Month { get; set; }

        public List<RoomUsageReportRowViewModel> Rows { get; set; } = new();
    }
}
