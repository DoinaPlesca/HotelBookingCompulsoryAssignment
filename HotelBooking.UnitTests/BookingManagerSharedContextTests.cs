using System;
using System.Linq;
using System.Threading.Tasks;
using HotelBooking.Core;
using HotelBooking.Core.Time;
using HotelBooking.UnitTests.Fakes;
using Xunit;

namespace HotelBooking.UnitTests;

public class BookingManagerSharedContextTests : UsesSystemTime
    {
        private readonly IRepository<Booking> bookingRepo;
        private readonly IRepository<Room> roomRepo;
        private readonly IBookingManager sut; //System Under Test — the object we're testing.

        private static DateTime D(int days) => SystemTime.Now.Date.AddDays(days);

        public BookingManagerSharedContextTests()
        {
            var fullyStart = D(10);
            var fullyEnd   = D(20);

            bookingRepo = new FakeBookingRepository(fullyStart, fullyEnd);
            roomRepo    = new FakeRoomRepository();
            sut         = new BookingManager(bookingRepo, roomRepo);
        }

        [Fact]
        public async Task FindAvailableRoom_Throws_When_StartIsTodayOrPast()
        {
            var today = SystemTime.Now.Date;
            Task act() => sut.FindAvailableRoom(today, today);
            await Assert.ThrowsAsync<ArgumentException>(act);
        }

        [Fact]
        public async Task FindAvailableRoom_ReturnsFreeRoom_ForNonOverlap_AndRoomHasNoConflicts()
        {
            var start = D(3); var end = D(5);
            var roomId = await sut.FindAvailableRoom(start, end);
            Assert.NotEqual(-1, roomId);

            var anyOverlap = (await bookingRepo.GetAllAsync())
                .Any(b => b.IsActive && b.RoomId == roomId && !(end < b.StartDate || start > b.EndDate));
            Assert.False(anyOverlap);
        }

        [Fact]
        public async Task FindAvailableRoom_ReturnsMinusOne_ForClearOverlap()
        {
            var roomId = await sut.FindAvailableRoom(D(12), D(14));
            Assert.Equal(-1, roomId);
        }

        [Fact]
        public async Task CreateBooking_AssignsFirstAvailableRoom_AndMarksActive()
        {
            var req = new Booking { StartDate = D(3), EndDate = D(4) };
            var ok = await sut.CreateBooking(req);
            Assert.True(ok);
            Assert.True(req.IsActive);
            Assert.Equal(1, req.RoomId); // rooms are [1,2]; first free is 1
        }

        [Fact]
        public async Task CreateBooking_ReturnsFalse_When_NoRoomAvailable()
        {
            var req = new Booking { StartDate = D(12), EndDate = D(14) }; // fully occupied window
            var ok = await sut.CreateBooking(req);
            Assert.False(ok);
            Assert.False(req.IsActive);
            Assert.Equal(0, req.RoomId);
        }
    }
