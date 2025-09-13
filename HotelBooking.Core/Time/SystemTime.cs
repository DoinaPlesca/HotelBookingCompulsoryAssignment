using System;

namespace HotelBooking.Core.Time
{
    /// <summary> Testable clock:helper to control "current time" in tests.
    // - Set(...) pins a fake "now"
    // - Reset() returns to real time
    // - Now returns the pinned time if set, otherwise DateTime.Now</summary>
    public static class SystemTime
    {
        // Holds the pinned time; MinValue means "not pinned"
        private static DateTime _date;
        
        // Pin the current time used by Now (use in test setup)
        public static void Set(DateTime custom) => _date = custom;

        // Unpin time so Now uses the real system clock again
        public static void Reset() => _date = DateTime.MinValue;

        // Current time: returns pinned value if set; otherwise the real DateTime.Now
        public static DateTime Now
        {
            get => _date != DateTime.MinValue ? _date : DateTime.Now;
        }
    }
}