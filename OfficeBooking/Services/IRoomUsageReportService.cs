using OfficeBooking.ViewModels;

namespace OfficeBooking.Services;

public interface IRoomUsageReportService
{
    Task<RoomUsageReportViewModel> GetReportAsync(int? year, int? month, DateTime today);
}
