using HotelBooking.Core;
using Moq;

namespace HotelBooking.Tests
{
    public class BookingManagerTests
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
        public void FindAvailableRoom_StartDateInPast_Throws()
        {
            var start = DateTime.Today.AddDays(-1);
            var end = DateTime.Today.AddDays(1);

            Assert.That(
                async () => await manager.FindAvailableRoom(start, end),
                Throws.TypeOf<ArgumentException>()
            );
        }

        [Test]
        public void FindAvailableRoom_StartAfterEnd_Throws()
        {
            var start = DateTime.Today.AddDays(5);
            var end = DateTime.Today.AddDays(1);

            Assert.That(
                async () => await manager.FindAvailableRoom(start, end),
                Throws.TypeOf<ArgumentException>()
            );
        }

        [Test]
        public async Task FindAvailableRoom_NoRooms_ReturnsMinus1()
        {
            roomRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Room>());
            bookingRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Booking>());

            var result = await manager.FindAvailableRoom(
                DateTime.Today.AddDays(1), 
                DateTime.Today.AddDays(2));

            Assert.That(result, Is.EqualTo(-1));
        }

        [Test]
        public async Task FindAvailableRoom_OneRoom_NoBookings_ReturnsId()
        {
            roomRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Room> {
                new Room { Id = 1 }
            });
            bookingRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Booking>());

            var result = await manager.FindAvailableRoom(
                DateTime.Today.AddDays(1),
                DateTime.Today.AddDays(2));

            Assert.That(result, Is.EqualTo(1));
        }

        [Test]
        public async Task FindAvailableRoom_AllRoomsOccupied_ReturnsMinus1()
        {
            roomRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Room> {
                new Room { Id = 1 }
            });

            bookingRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Booking> {
                new Booking {
                    RoomId = 1,
                    StartDate = DateTime.Today.AddDays(1),
                    EndDate = DateTime.Today.AddDays(5),
                    IsActive = true
                }
            });

            var result = await manager.FindAvailableRoom(
                DateTime.Today.AddDays(2),
                DateTime.Today.AddDays(3));

            Assert.That(result, Is.EqualTo(-1));
        }

        [Test]
        public async Task FindAvailableRoom_MultipleRooms_OneFree_ReturnsThatRoom()
        {
            roomRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Room> {
                new Room { Id = 1 },
                new Room { Id = 2 }
            });

            bookingRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Booking> {
                new Booking {
                    RoomId = 1,
                    StartDate = DateTime.Today.AddDays(1),
                    EndDate = DateTime.Today.AddDays(5),
                    IsActive = true
                }
            });

            var result = await manager.FindAvailableRoom(
                DateTime.Today.AddDays(2),
                DateTime.Today.AddDays(3));

            Assert.That(result, Is.EqualTo(2));
        }
    }
}
