namespace OfficeBooking.Tests;

// A fake TimeProvider for deterministic testing.
// Allows tests to control what "now" means.
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

    // Advances the fake time by the specified amount.
    public void Advance(TimeSpan duration)
    {
        _now = _now.Add(duration);
    }

    // Sets the fake time to a specific value.
    public void SetNow(DateTimeOffset now)
    {
        _now = now;
    }

    // Sets the fake time to a specific value.
    public void SetNow(DateTime now)
    {
        _now = new DateTimeOffset(now);
    }
}
