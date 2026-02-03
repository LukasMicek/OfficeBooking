namespace OfficeBooking.Tests;

/// <summary>
/// A fake TimeProvider for deterministic testing.
/// Allows tests to control what "now" means.
/// </summary>
public class FakeTimeProvider : TimeProvider
{
    private DateTimeOffset _now;

    public FakeTimeProvider(DateTimeOffset now)
    {
        _now = now;
    }

    public FakeTimeProvider(DateTime now)
        : this(new DateTimeOffset(now))
    {
    }

    public override DateTimeOffset GetUtcNow() => _now.ToUniversalTime();

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Local;

    /// <summary>
    /// Advances the fake time by the specified amount.
    /// </summary>
    public void Advance(TimeSpan duration)
    {
        _now = _now.Add(duration);
    }

    /// <summary>
    /// Sets the fake time to a specific value.
    /// </summary>
    public void SetNow(DateTimeOffset now)
    {
        _now = now;
    }

    /// <summary>
    /// Sets the fake time to a specific value.
    /// </summary>
    public void SetNow(DateTime now)
    {
        _now = new DateTimeOffset(now);
    }
}
