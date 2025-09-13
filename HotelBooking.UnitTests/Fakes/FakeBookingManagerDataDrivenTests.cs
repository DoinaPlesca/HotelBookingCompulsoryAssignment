using System;
using System.Linq;
using System.Threading.Tasks;
using HotelBooking.Core;
using HotelBooking.Core.Time;
using Xunit;

namespace HotelBooking.UnitTests.Fakes;

public class FakeBookingManagerDataDrivenTests : UsesSystemTime
{
    private static DateTime D(int days) => SystemTime.Now.Date.AddDays(days);
    private static readonly DateTime FullyStart = D(10);
    private static readonly DateTime FullyEnd   = D(20);

    public static TheoryData<int,int,string> OverlapShapes => new()
    {
        { 11, 13, "start-inside" },
        {  9, 10, "end-inside" },
        { 10, 20, "exact-match" },
        {  9, 21, "straddle" },
        {  8, 10, "back-to-back-start" }, // end == existing start (disallowed)
        { 20, 22, "back-to-back-end" },   // start == existing end  (disallowed)
    };

    [Theory]
    [MemberData(nameof(OverlapShapes))]
    public async Task FindAvailableRoom_OverlapShapes_ReturnsMinusOne(int from, int to, string _case)
    {
        var sut = new BookingManager(
            new FakeBookingRepository(FullyStart, FullyEnd),
            new FakeRoomRepository());

        var roomId = await sut.FindAvailableRoom(D(from), D(to));
        Assert.Equal(-1, roomId);
    }

    public static TheoryData<int,int> NonOverlapWindows => new() { { 3, 5 }, { 25, 27 } };

    [Theory]
    [MemberData(nameof(NonOverlapWindows))]
    public async Task FindAvailableRoom_NonOverlap_ReturnsFreeRoom_AndRoomIsTrulyFree(int from, int to)
    {
        var bookingRepo = new FakeBookingRepository(FullyStart, FullyEnd);
        var sut = new BookingManager(bookingRepo, new FakeRoomRepository());

        var start = D(from); var end = D(to);
        var roomId = await sut.FindAvailableRoom(start, end);
        Assert.NotEqual(-1, roomId);

        var anyOverlap = (await bookingRepo.GetAllAsync())
            .Any(b => b.IsActive && b.RoomId == roomId && !(end < b.StartDate || start > b.EndDate));
        Assert.False(anyOverlap);
    }
}