using HotelBooking.Core;
using Moq;

namespace HotelBooking.Tests
{
    public class GetFullyOccupiedDatesTests
    {
        private Mock<IRepository<Booking>> bookingRepo;
        private Mock<IRepository<Room>> roomRepo;
        private BookingManager manager;

        [SetUp]
        public void Setup()
        {
            bookingRepo = new Mock<IRepository<Booking>>();
            roomRepo = new Mock<IRepository<Room>>();

            manager = new BookingManager(bookingRepo.Object, roomRepo.Object);
        }

        [Test]
        public void GetFullyOccupiedDates_StartAfterEnd_Throws()
        {
            var start = new DateTime(2025, 1, 10);
            var end = new DateTime(2025, 1, 5);

            Assert.That(
                async () => await manager.GetFullyOccupiedDates(start, end),
                Throws.TypeOf<ArgumentException>()
            );
        }

        [Test]
        public async Task GetFullyOccupiedDates_NoRooms_ReturnsEmpty()
        {
            roomRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Room>());
            bookingRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Booking>());

            var result = await manager.GetFullyOccupiedDates(
                new DateTime(2025, 1, 1),
                new DateTime(2025, 1, 5));

            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetFullyOccupiedDates_RoomsButNoBookings_ReturnsEmpty()
        {
            roomRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Room> {
                new Room { Id = 1 },
                new Room { Id = 2 }
            });

            bookingRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Booking>());

            var result = await manager.GetFullyOccupiedDates(
                new DateTime(2025, 1, 1),
                new DateTime(2025, 1, 3));

            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetFullyOccupiedDates_AllRoomsFullEveryDay_ReturnsAllDates()
        {
            roomRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Room> {
                new Room { Id = 1 },
                new Room { Id = 2 }
            });

            bookingRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Booking> {
                new Booking { RoomId = 1, StartDate = new DateTime(2025,1,1), EndDate = new DateTime(2025,1,5), IsActive = true },
                new Booking { RoomId = 2, StartDate = new DateTime(2025,1,1), EndDate = new DateTime(2025,1,5), IsActive = true }
            });

            var result = await manager.GetFullyOccupiedDates(
                new DateTime(2025, 1, 1),
                new DateTime(2025, 1, 5));

            Assert.That(result.Count, Is.EqualTo(5));
            Assert.That(result[0], Is.EqualTo(new DateTime(2025, 1, 1)));
            Assert.That(result[4], Is.EqualTo(new DateTime(2025, 1, 5)));
        }

        [Test]
        public async Task GetFullyOccupiedDates_OnlySomeDaysFull_ReturnsCorrectDays()
        {
            roomRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Room> {
                new Room { Id = 1 },
                new Room { Id = 2 },
                new Room { Id = 3 }
            });

            bookingRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Booking> {
                new Booking { RoomId = 1, StartDate = new DateTime(2025,1,1), EndDate = new DateTime(2025,1,3), IsActive = true },
                new Booking { RoomId = 2, StartDate = new DateTime(2025,1,2), EndDate = new DateTime(2025,1,3), IsActive = true },
                new Booking { RoomId = 3, StartDate = new DateTime(2025,1,3), EndDate = new DateTime(2025,1,4), IsActive = true }
            });

            var result = await manager.GetFullyOccupiedDates(
                new DateTime(2025, 1, 1),
                new DateTime(2025, 1, 4));

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0], Is.EqualTo(new DateTime(2025, 1, 3)));
        }

        [Test]
        public async Task GetFullyOccupiedDates_PartialOverlap_CorrectOutput()
        {
            roomRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Room> {
                new Room { Id = 1 },
                new Room { Id = 2 }
            });

            bookingRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Booking> {
                new Booking { RoomId = 1, StartDate = new DateTime(2025,1,5), EndDate = new DateTime(2025,1,7), IsActive = true },
                new Booking { RoomId = 2, StartDate = new DateTime(2025,1,6), EndDate = new DateTime(2025,1,7), IsActive = true }
            });

            var result = await manager.GetFullyOccupiedDates(
                new DateTime(2025, 1, 1),
                new DateTime(2025, 1, 10));

            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[0], Is.EqualTo(new DateTime(2025, 1, 6)));
            Assert.That(result[1], Is.EqualTo(new DateTime(2025, 1, 7)));
        }

        [Test]
        public async Task GetFullyOccupiedDates_SingleDayRange_CorrectHandling()
        {
            roomRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Room> {
                new Room { Id = 1 },
                new Room { Id = 2 }
            });

            bookingRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Booking> {
                new Booking { RoomId = 1, StartDate = new DateTime(2025,1,10), EndDate = new DateTime(2025,1,10), IsActive = true },
                new Booking { RoomId = 2, StartDate = new DateTime(2025,1,10), EndDate = new DateTime(2025,1,10), IsActive = true }
            });

            var result = await manager.GetFullyOccupiedDates(
                new DateTime(2025, 1, 10),
                new DateTime(2025, 1, 10));

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0], Is.EqualTo(new DateTime(2025, 1, 10)));
        }
    }
}
