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
            // Arrange
            var date = SystemTime.Now.Date; // today (pinned)
            // Act
            Task act() => bookingManager.FindAvailableRoom(date, date);
            // Assert
            await Assert.ThrowsAsync<ArgumentException>(act);
        }

        [Fact]
        public async Task FindAvailableRoom_RoomAvailable_RoomIdNotMinusOne()
        {
            // Arrange
            var date = SystemTime.Now.Date.AddDays(1);
            // Act
            int roomId = await bookingManager.FindAvailableRoom(date, date);
            // Assert
            Assert.NotEqual(-1, roomId);
        }

        [Fact]
        public async Task FindAvailableRoom_RoomAvailable_ReturnsAvailableRoom()
        {
            // This test was added to satisfy the following test design
            // principle: "Tests should have strong assertions".

            // Arrange
            var date = SystemTime.Now.Date.AddDays(1);
            // Act
            int roomId = await bookingManager.FindAvailableRoom(date, date);

            var bookingForReturnedRoomId = (await bookingRepository.GetAllAsync()).Where(b =>
                b.RoomId == roomId && b.StartDate <= date && b.EndDate >= date && b.IsActive
            );
            // Assert
            Assert.Empty(bookingForReturnedRoomId);
        }
    }
}
