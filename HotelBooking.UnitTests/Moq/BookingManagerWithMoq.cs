using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotelBooking.Core;
using Moq;
using Xunit;

namespace HotelBooking.UnitTests;

public class BookingManagerWithMoq
{
    private static DateTime D(int days) => DateTime.Today.AddDays(days);
        private static Room R(int id) => new Room { Id = id };

        private static BookingManager Build(
            IEnumerable<Booking> bookings,
            IEnumerable<Room> rooms,
            out Mock<IRepository<Booking>> bookingRepo,
            out Mock<IRepository<Room>> roomRepo)
        {
            bookingRepo = new Mock<IRepository<Booking>>(MockBehavior.Strict);
            roomRepo    = new Mock<IRepository<Room>>(MockBehavior.Strict);

            bookingRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(bookings.ToList());
            bookingRepo.Setup(r => r.AddAsync(It.IsAny<Booking>())).Returns(Task.CompletedTask);

            roomRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms.ToList());

            return new BookingManager(bookingRepo.Object, roomRepo.Object);
        }

        [Fact]
        public async Task CreateBooking_CallsAddAsync_WhenARoomIsAvailable()
        {
            // ARRANGE (room 1 has a conflicting booking; room 2 is free)
            var existing = new List<Booking> {
                new Booking { RoomId = 1, StartDate = D(10), EndDate = D(12), IsActive = true }
            };
            var rooms   = new List<Room> { R(1), R(2) };
            var request = new Booking { StartDate = D(10), EndDate = D(12) };
            var sut = Build(existing, rooms, out var bookingRepo, out _);

            // ACT
            var ok = await sut.CreateBooking(request);

            // ASSERT
            Assert.True(ok);
            Assert.True(request.IsActive);
            Assert.Equal(2, request.RoomId); // picked the free room
            bookingRepo.Verify(r => r.AddAsync(It.IsAny<Booking>()), Times.Once);
        }

        [Fact]
        public async Task CreateBooking_DoesNotCallAddAsync_WhenNoRoomAvailable()
        {
            // ARRANGE (both rooms are taken)
            var existing = new List<Booking> {
                new Booking { RoomId = 1, StartDate = D(10), EndDate = D(12), IsActive = true },
                new Booking { RoomId = 2, StartDate = D(10), EndDate = D(12), IsActive = true }
            };
            var rooms   = new List<Room> { R(1), R(2) };
            var request = new Booking { StartDate = D(11), EndDate = D(12) };
            var sut = Build(existing, rooms, out var bookingRepo, out _);

            // ACT
            var ok = await sut.CreateBooking(request);

            // ASSERT
            Assert.False(ok);
            bookingRepo.Verify(r => r.AddAsync(It.IsAny<Booking>()), Times.Never);
        }
    }

