using System;
using System.Linq;
using System.Threading.Tasks;
using HotelBooking.Core;
using HotelBooking.Core.Time;
using HotelBooking.UnitTests.Fakes;
using Xunit;

namespace HotelBooking.UnitTests
{
    public class BookingManagerTests : UsesSystemTime
    {
        private readonly IBookingManager bookingManager;
        private readonly IRepository<Booking> bookingRepository;

        public BookingManagerTests()
        {
            var start = SystemTime.Now.Date.AddDays(10);
            var end = SystemTime.Now.Date.AddDays(20);
            bookingRepository = new FakeBookingRepository(start, end);
            IRepository<Room> roomRepository = new FakeRoomRepository();
            bookingManager = new BookingManager(bookingRepository, roomRepository);
        }

        [Fact]
        public async Task FindAvailableRoom_StartDateNotInTheFuture_ThrowsArgumentException()
        {
            var date = SystemTime.Now.Date; // today (pinned)
            Task act() => bookingManager.FindAvailableRoom(date, date);
            await Assert.ThrowsAsync<ArgumentException>(act);
        }

        [Fact]
        public async Task FindAvailableRoom_RoomAvailable_RoomIdNotMinusOne()
        {
            var date = SystemTime.Now.Date.AddDays(1);
            int roomId = await bookingManager.FindAvailableRoom(date, date);
            Assert.NotEqual(-1, roomId);
        }

        [Fact]
        public async Task FindAvailableRoom_RoomAvailable_ReturnsAvailableRoom()
        {
            var date = SystemTime.Now.Date.AddDays(1);

            int roomId = await bookingManager.FindAvailableRoom(date, date);

            var bookingForReturnedRoomId = (await bookingRepository.GetAllAsync()).Where(b =>
                b.RoomId == roomId && b.StartDate <= date && b.EndDate >= date && b.IsActive
            );

            Assert.Empty(bookingForReturnedRoomId);
        }
    }
}
