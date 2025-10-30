using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotelBooking.Core;
using HotelBooking.Core.Time;
using Moq;
using Xunit;

namespace HotelBooking.UnitTests.BlackBoxTests;
    /// <summary>
    /// CreateBooking – Black-box tests derived by ECT + BVT + Decision Table (B/O/A).
    /// Overlap is inclusive; inactive bookings do not block.
    /// </summary>
    public class CreateBookingDerivedTests : UsesSystemTime
    {
        private static DateTime D(int days) => SystemTime.Now.Date.AddDays(days);
        private static Room R(int id) => new Room { Id = id };

        private static BookingManager Build(
            IEnumerable<Booking> seedBookings,
            IEnumerable<Room> seedRooms,
            out Mock<IRepository<Booking>> bookingRepo,
            out Mock<IRepository<Room>> roomRepo,
            bool expectAdd = false)
        {
            bookingRepo = new Mock<IRepository<Booking>>(MockBehavior.Strict);
            roomRepo    = new Mock<IRepository<Room>>(MockBehavior.Strict);

            bookingRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(seedBookings.ToList());
            if (expectAdd)
            {
                bookingRepo.Setup(r => r.AddAsync(It.IsAny<Booking>())).Returns(Task.CompletedTask);
            }
            roomRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(seedRooms.ToList());

            return new BookingManager(bookingRepo.Object, roomRepo.Object);
        }

        //fully-occupied window O = [D+10..D+20]
        private static (DateTime S, DateTime E) O => (D(10), D(20));
        private static Booking ActiveO(int roomId) =>
            new Booking { RoomId = roomId, StartDate = O.S, EndDate = O.E, IsActive = true };

        // ==========================================
        // Invalid classes (IC1, IC2)
        // ECT = groups tests that behave the same.
        // These tests pick one representative from each class.
        // ==========================================  
        
        [Fact] // ETC - IC1: StartDate ≤ today -> throw
        public async Task CreateBooking_StartToday_Throws()
        {
            var sut = Build(Enumerable.Empty<Booking>(), new[] { R(1), R(2) }, out _, out _);
            var req = new Booking { StartDate = SystemTime.Now.Date, EndDate = D(1) };
            await Assert.ThrowsAsync<ArgumentException>(() => sut.CreateBooking(req));
        }

        [Fact] // ETC - IC2: StartDate > EndDate -> throw
        public async Task CreateBooking_StartAfterEnd_Throws()
        {
            var sut = Build(Enumerable.Empty<Booking>(), new[] { R(1), R(2) }, out _, out _);
            var req = new Booking { StartDate = D(12), EndDate = D(10) };
            await Assert.ThrowsAsync<ArgumentException>(() => sut.CreateBooking(req));
        }

        // ==========================================
        // BVT — Boundaries of the occupied window
        // (touching the window counts as overlap)
       
        // B = Before the fully occupied window
        // A = After the fully occupied window
        // SD = Start Date of the requested booking
        // ED = End Date of the requested booking
        // O = [D+10 … D+20] be the fully-occupied window (inclusive).
        // ==========================================
        
        [Fact] // BVT: B..O (ED hits start of O) -> N
        public async Task BVT_CreateBooking_B2O_TouchesStart_Fails()
        {
            var rooms = new[] { R(1), R(2) };
            var bookings = new List<Booking> { ActiveO(1), ActiveO(2) };
            var sut = Build(bookings, rooms, out var bookingRepo, out _);
            var req = new Booking { StartDate = D(9), EndDate = D(10) };

            var ok = await sut.CreateBooking(req);

            Assert.False(ok);
            bookingRepo.Verify(r => r.AddAsync(It.IsAny<Booking>()), Times.Never);
        }
        
        [Fact] // BVT: B..O (ED hits end of O) -> N
        public async Task BVT_CreateBooking_B2O_TouchesEnd_Fails()
        {
            var rooms = new[] { R(1), R(2) };
            var bookings = new List<Booking> { ActiveO(1), ActiveO(2) };
            var sut = Build(bookings, rooms, out var bookingRepo, out _);
            var req = new Booking { StartDate = D(9), EndDate = D(20) };

            var ok = await sut.CreateBooking(req);

            Assert.False(ok);
            bookingRepo.Verify(r => r.AddAsync(It.IsAny<Booking>()), Times.Never);
        }
        
        [Fact] // BVT: O..A (SD at start of O) -> N
        public async Task CreateBooking_O2A_FromStart_Fails()
        {
            var rooms = new[] { R(1), R(2) };
            var bookings = new List<Booking> { ActiveO(1), ActiveO(2) };
            var sut = Build(bookings, rooms, out var bookingRepo, out _);
            var req = new Booking { StartDate = D(10), EndDate = D(21) };
        
            var ok = await sut.CreateBooking(req);
        
            Assert.False(ok);
            bookingRepo.Verify(r => r.AddAsync(It.IsAny<Booking>()), Times.Never);
        }
        
        [Fact] // BVT: O..A (SD at end of O) -> N
        public async Task CreateBooking_O2A_FromEnd_Fails()
        {
            var rooms = new[] { R(1), R(2) };
            var bookings = new List<Booking> { ActiveO(1), ActiveO(2) };
            var sut = Build(bookings, rooms, out var bookingRepo, out _);
            var req = new Booking { StartDate = D(20), EndDate = D(21) };
        
            var ok = await sut.CreateBooking(req);
        
            Assert.False(ok);
            bookingRepo.Verify(r => r.AddAsync(It.IsAny<Booking>()), Times.Never);
        }
        
        
        // ==========================================
        // Decision table – TC#1..#10
        // ==========================================

        [Fact] // DT - TC1: B..B -> Y
        public async Task DT_CreateBooking_BeforeWindow_Succeeds()
        {
            var rooms = new[] { R(1), R(2) };
            var sut = Build(Enumerable.Empty<Booking>(), rooms, out var bookingRepo, out _, expectAdd: true);
            var req = new Booking { StartDate = D(1), EndDate = D(9) };

            var ok = await sut.CreateBooking(req);

            Assert.True(ok);
            Assert.True(req.IsActive);
            Assert.True(req.RoomId > 0);
            bookingRepo.Verify(r => r.AddAsync(It.Is<Booking>(b => b.RoomId == req.RoomId && b.IsActive)), Times.Once);
        }

        [Fact] // DT - TC2: O..O -> N (fully inside occupied)
        public async Task DT_CreateBooking_InsideWindow_Fails()
        {
            var rooms = new[] { R(1), R(2) };
            var bookings = new List<Booking> { ActiveO(1), ActiveO(2) };
            var sut = Build(bookings, rooms, out var bookingRepo, out _);
            var req = new Booking { StartDate = D(12), EndDate = D(14) };

            var ok = await sut.CreateBooking(req);

            Assert.False(ok);
            bookingRepo.Verify(r => r.AddAsync(It.IsAny<Booking>()), Times.Never);
        }

        [Fact] // DT - TC3: A..A -> Y
        public async Task DT_CreateBooking_AfterWindow_Succeeds()
        {
            var rooms = new[] { R(1), R(2) };
            var sut = Build(Enumerable.Empty<Booking>(), rooms, out var bookingRepo, out _, expectAdd: true);
            var req = new Booking { StartDate = D(21), EndDate = D(23) };

            var ok = await sut.CreateBooking(req);

            Assert.True(ok);
            Assert.True(req.IsActive);
            Assert.True(req.RoomId > 0);
            bookingRepo.Verify(r => r.AddAsync(It.Is<Booking>(b => b.RoomId == req.RoomId && b.IsActive)), Times.Once);
        }

        [Fact] // DT - TC8: B..A (spans across O) -> N
        public async Task DT_CreateBooking_B2A_SpansWindow_Fails()
        {
            var rooms = new[] { R(1), R(2) };
            var bookings = new List<Booking> { ActiveO(1), ActiveO(2) };
            var sut = Build(bookings, rooms, out var bookingRepo, out _);
            var req = new Booking { StartDate = D(9), EndDate = D(21) };

            var ok = await sut.CreateBooking(req);

            Assert.False(ok);
            bookingRepo.Verify(r => r.AddAsync(It.IsAny<Booking>()), Times.Never);
        }

        [Fact] // DT- TC9: B..A (earliest valid SD, spans O) -> N
        public async Task DT_CreateBooking_B2A_FromEarliestValid_Fails()
        {
            var rooms = new[] { R(1), R(2) };
            var bookings = new List<Booking> { ActiveO(1), ActiveO(2) };
            var sut = Build(bookings, rooms, out var bookingRepo, out _);
            var req = new Booking { StartDate = D(1), EndDate = D(21) };

            var ok = await sut.CreateBooking(req);

            Assert.False(ok);
            bookingRepo.Verify(r => r.AddAsync(It.IsAny<Booking>()), Times.Never);
        }

        // ============================
        // Capacity edge as a case
        // if every room is already booked on the requested dates, a new booking must be rejected and not saved.
        // ============================

        [Fact] // TC10: Capacity full on requested range -> N
        public async Task CreateBooking_CapacityFullOnRange_Fails()
        {
            var rooms = new[] { R(1), R(2) };
            var start = D(10); var end = D(12);
            var bookings = new List<Booking>
            {
                new Booking { RoomId = 1, StartDate = start, EndDate = end, IsActive = true },
                new Booking { RoomId = 2, StartDate = start, EndDate = end, IsActive = true }
            };
            var sut = Build(bookings, rooms, out var bookingRepo, out _);
            var req = new Booking { StartDate = start, EndDate = end };

            var ok = await sut.CreateBooking(req);

            Assert.False(ok);
            bookingRepo.Verify(r => r.AddAsync(It.IsAny<Booking>()), Times.Never);
        }
    }

