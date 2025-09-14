using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotelBooking.Core;
using HotelBooking.Core.Time;
using Moq;
using Xunit;

namespace HotelBooking.UnitTests;

public class BookingManagerWithMoq : UsesSystemTime
{
    private static DateTime D(int days) => SystemTime.Now.Date.AddDays(days);
        private static Room R(int id) => new Room { Id = id };

        private static BookingManager Build(
            IEnumerable<Booking> seedBookings,
            IEnumerable<Room> seedRooms,
            out Mock<IRepository<Booking>> bookingRepo,
            out Mock<IRepository<Room>> roomRepo)
        {
            bookingRepo = new Mock<IRepository<Booking>>(MockBehavior.Strict);
            roomRepo    = new Mock<IRepository<Room>>(MockBehavior.Strict);

            bookingRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(seedBookings.ToList());
            bookingRepo.Setup(r => r.AddAsync(It.IsAny<Booking>())).Returns(Task.CompletedTask);
            roomRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(seedRooms.ToList());

            return new BookingManager(bookingRepo.Object, roomRepo.Object);
        }

        // data-driven: IsActive controls availability
        public static TheoryData<bool,int> AvailabilityByActivity => new() {
            { false, 1 }, // inactive overlap -> room free
            { true, -1 }, // active overlap   -> blocked
        };

        [Theory]
        [MemberData(nameof(AvailabilityByActivity))]
        public async Task FindAvailableRoom_RespectsIsActive(bool isActive, int expectedRoomId)
        {
            var bookings = new List<Booking> {
                new Booking { RoomId = 1, StartDate = D(10), EndDate = D(12), IsActive = isActive }
            };
            var sut = Build(bookings, new[] { R(1) }, out _, out _);
            var roomId = await sut.FindAvailableRoom(D(10), D(12));
            Assert.Equal(expectedRoomId, roomId);
        }

        // data-driven: fully occupied requires all rooms booked & active
        public static TheoryData<bool,bool,int> FullyOccupiedCases => new() {
            { true,  true,  3 }, // both active over 3-day window
            { true,  false, 0 }, // one inactive -> never fully occupied
        };

        [Theory]
        [MemberData(nameof(FullyOccupiedCases))]
        public async Task GetFullyOccupiedDates_RequiresAllRoomsActive(bool r1Active, bool r2Active, int expectedCount)
        {
            var start = D(10); var end = D(12);
            var bookings = new List<Booking> {
                new Booking { RoomId = 1, StartDate = start, EndDate = end, IsActive = r1Active },
                new Booking { RoomId = 2, StartDate = start, EndDate = end, IsActive = r2Active }
            };
            var sut = Build(bookings, new[] { R(1), R(2) }, out _, out _);
            var fully = await sut.GetFullyOccupiedDates(start, end);
            Assert.Equal(expectedCount, fully.Count);
        }

        [Fact]
        public async Task CreateBooking_CallsAddAsync_WithAssignedRoom_AndIsActive_WhenAvailable()
        {
            var bookings = new List<Booking> {
                new Booking { RoomId = 1, StartDate = D(10), EndDate = D(12), IsActive = true }
            };
            var sut = Build(bookings, new[] { R(1), R(2) }, out var bookingRepo, out _);
            var req = new Booking { StartDate = D(10), EndDate = D(12) };

            var ok = await sut.CreateBooking(req);

            Assert.True(ok);
            Assert.Equal(1, req.RoomId);
            Assert.True(req.IsActive);
            bookingRepo.Verify(r => r.AddAsync(It.Is<Booking>(b => b.RoomId == 1 && b.IsActive)), Times.Once);
        }

        [Fact]
        public async Task FindAvailableRoom_Throws_WhenStartAfterEnd()
        {
            var sut = Build(Enumerable.Empty<Booking>(), new[] { R(1) }, out _, out _);
            await Assert.ThrowsAsync<ArgumentException>(() => sut.FindAvailableRoom(D(10), D(5)));
        }
    }
