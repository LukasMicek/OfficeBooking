namespace OfficeBooking.Services;

public interface IRoomLockProvider
{
    Task<IDisposable> AcquireAsync(int roomId, CancellationToken cancellationToken = default);
}
