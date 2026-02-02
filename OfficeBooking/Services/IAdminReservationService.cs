using OfficeBooking.ViewModels;

namespace OfficeBooking.Services;

public interface IAdminReservationService
{
    Task<IReadOnlyList<AdminReservationRowViewModel>> GetListAsync(bool activeOnly);
    Task<AdminCancelReservationViewModel?> GetCancelViewModelAsync(int id);
    Task<AdminCancelResult> CancelAsync(int id, string cancelReason, DateTime now);
}

public enum AdminCancelStatus
{
    Success,
    NotFound,
    AlreadyCancelled
}

public record AdminCancelResult(AdminCancelStatus Status)
{
    public static AdminCancelResult Ok() => new(AdminCancelStatus.Success);
    public static AdminCancelResult NotFound() => new(AdminCancelStatus.NotFound);
    public static AdminCancelResult AlreadyCancelled() => new(AdminCancelStatus.AlreadyCancelled);
}
