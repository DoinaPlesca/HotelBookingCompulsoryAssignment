using System;
using HotelBooking.Core.Time;

namespace HotelBooking.UnitTests;

/// Pins “today” for each test class and resets afterwards.
public abstract class UsesSystemTime : IDisposable
{
    // The fixed moment used as "now" in tests
    protected static readonly DateTime FixedNow = new DateTime(2025, 1, 15, 12, 0, 0, DateTimeKind.Local);

    // Pin the clock at construction (runs before each test in xUnit)
    protected UsesSystemTime() => SystemTime.Set(FixedNow);
    
    // Reset the clock after each test
    public void Dispose() => SystemTime.Reset();

    // Helper: "today + n days" based on the pinned clock
    protected static DateTime T(int daysFromToday) => SystemTime.Now.Date.AddDays(daysFromToday);
}
