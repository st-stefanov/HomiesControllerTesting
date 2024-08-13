using Homies.Data;
using Homies.Data.Models;
using Homies.Models.Event;
using Homies.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace Homies.Tests
{
    [TestFixture]
    internal class EventServiceTests
    {
        private HomiesDbContext _dbContext;
        private EventService _eventService;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<HomiesDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Use unique database name to avoid conflicts
                .Options;
            _dbContext = new HomiesDbContext(options);

            _eventService = new EventService(_dbContext);
        }

        [Test]
        public async Task AddEventAsync_ShouldAddEvent_WhenValidEventModelAndUserId()
        {
            // Step 1: Arrange - Set up the initial conditions for the test
            // Create a new event model with test data
            var eventModel = new EventFormModel
            {
                Name = "Test Event",
                Description = "Test Description",
                Start = DateTime.Now,
                End = DateTime.Now.AddHours(2)
            };
            // Define a user ID for testing purposes
            string userId = "testUserId";

            // Step 2: Act - Perform the action being tested
            // Call the service method to add the event
            await _eventService.AddEventAsync(eventModel, userId);
            // Step 3: Assert - Verify the outcome of the action
            // Retrieve the added event from the database
            var eventInTheDatabase = await _dbContext.Events.FirstOrDefaultAsync(x => x.Name == eventModel.Name && x.OrganiserId == userId);

            // Assert that the added event is not null, indicating it was successfully added
            Assert.IsNotNull(eventInTheDatabase);
            // Assert that the description of the added event matches the description provided in the event model
            Assert.That(eventInTheDatabase.Description, Is.EqualTo(eventModel.Description));
            Assert.That(eventInTheDatabase.Start, Is.EqualTo(eventModel.Start));
            Assert.That(eventInTheDatabase.End, Is.EqualTo(eventModel.End));
        }


        [Test]
        public async Task GetAllEventsAsync_ShouldReturnAllEvents()
        {
            // Step 1: Arrange - Set up the initial conditions for the test
            // Create two event models with test data
            var firstEventModel = new EventFormModel
            {
                Name = "First Test Event",
                Description = "First Test Description",
                Start = DateTime.Now,
                End = DateTime.Now.AddHours(2)
            };

            var secondEventModel = new EventFormModel
            {
                Name = "Second Test Event",
                Description = "Second Test Description",
                Start = DateTime.Now.AddDays(2),
                End = DateTime.Now.AddDays(3)
            };


            // Define a user ID for testing purposes
            string userId = "testUserId";

            _eventService.AddEventAsync(firstEventModel, userId);
            _eventService.AddEventAsync(secondEventModel, userId);
            // Step 2: Act - Perform the action being tested
            // Add the two events to the database using the event service
            var result = await _eventService.GetAllEventsAsync();
            // Step 3: Act - Retrieve the count of events from the database

            // Step 4: Assert - Verify the outcome of the action
            // Assert that the count of events in the database is equal to the expected count (2)
            Assert.That(result.Count, Is.EqualTo(2));
        }


        [Test]
        public async Task GetEventDetailsAsync_ShouldReturnAllEventDetails()
        {
            //Arrange
            var firstEventModel = new EventFormModel
            {
                Name = "First Test Event",
                Description = "First Test Description",
                Start = DateTime.Now,
                End = DateTime.Now.AddHours(2),
                TypeId = 2,
            };

            await _eventService.AddEventAsync(firstEventModel, "nonExistingUserId");
            var eventInTheDb = await _dbContext.Events.FirstAsync();
            //Act
            var result = await _eventService.GetEventDetailsAsync(eventInTheDb.Id);
            //Assert
            Assert.IsNotNull(result);
            Assert.That(result.Name, Is.EqualTo(firstEventModel.Name));
            Assert.That(result.Description, Is.EqualTo(firstEventModel.Description));

        }

        [Test]
        public async Task GetEventForEditAsync_ShouldGetEventIfPresentInTheDb()
        {
            //Arrange
            var firstEventModel = new EventFormModel
            {
                Name = "First Test Event",
                Description = "First Test Description",
                Start = DateTime.Now,
                End = DateTime.Now.AddHours(2),
                TypeId = 2,
            };

            await _eventService.AddEventAsync(firstEventModel, "nonExistingUserId");
            var eventInTheDb = await _dbContext.Events.FirstAsync();
            //Act
            var result = await _eventService.GetEventForEditAsync(eventInTheDb.Id);
            //Assert
            Assert.IsNotNull(result);
            Assert.That(result.Name, Is.EqualTo(firstEventModel.Name));
            
        }

        [Test]
        public async Task GetEventForEditAsync_ShouldReturnNullIfEventIsNotFound()
        {
            var result = await _eventService.GetEventForEditAsync(90);

            Assert.IsNull(result);
        }

        [Test]
        public async Task GetEventOrganiserId_ShouldReturnOrganiserIdIfExisting()
        {
            //Arrange
            var firstEventModel = new EventFormModel
            {
                Name = "First Test Event",
                Description = "First Test Description",
                Start = DateTime.Now,
                End = DateTime.Now.AddHours(2),
                TypeId = 2,
            };

            const string userId = "userId";

            await _eventService.AddEventAsync(firstEventModel, userId);
            var eventInTheDb = await _dbContext.Events.FirstAsync();

            //Act
            var result = await _eventService.GetEventOrganizerIdAsync(eventInTheDb.Id);

            //Assert
            Assert.NotNull(result);
            Assert.That(result, Is.EqualTo(userId));
        }

        [Test]
        public async Task GetEventOrganiserId_ShouldReturnNullIfEventIsNotExisting()
        {
            var result = await _eventService.GetEventOrganizerIdAsync(99);

            Assert.IsNull(result);
        }

        [Test]
        public async Task GetUserJoinedEventsAsync_ShouldReturnAllJoinedUsers()
        {
            //Arrange
            var testType = new Data.Models.Type
            {
                Name = "TestType",
            };

            await _dbContext.Types.AddAsync(testType);

            var testEvent = new Event
            {
                Name = "First Test Event",
                Description = "First Test Description",
                Start = DateTime.Now,
                End = DateTime.Now.AddHours(2),
                TypeId = testType.Id,
            };

            await _dbContext.Events.AddAsync(testEvent);
        }

        [Test]
        public async Task JoinEventAsync_ShouldReturnFalseIfEventDoesNotExist()
        {
            //Act
            var result = await _eventService.JoinEventAsync(99, "");
            //Assert
            Assert.False(result);
        }

        [Test]
        public async Task JoinEventAsync_ShouldReturnFalseIfUserIsAlreadyPartInTheEvent()
        {
            //Arrange
            const string userId = "userId";

            var testType = new Data.Models.Type
            {
                Name = "TestType",
            };
            await _dbContext.Types.AddAsync(testType);
            await _dbContext.SaveChangesAsync();

            var testEvent = new Event
            {
                Name = "First Test Event",
                Description = "First Test Description",
                Start = DateTime.Now,
                End = DateTime.Now.AddHours(2),
                TypeId = testType.Id,
                OrganiserId = userId
            };
            await _dbContext.Events.AddAsync(testEvent);
            await _dbContext.SaveChangesAsync();

            await _dbContext.EventsParticipants.AddAsync(new EventParticipant()
            {
                EventId = testEvent.Id,
                HelperId = userId
            });
            await _dbContext.SaveChangesAsync();

            //Act
            var result = await _eventService.JoinEventAsync(testEvent.Id, userId);
            //Assert
            Assert.False(result);
        }

        [Test]
        public async Task JoinEventAsync_ShouldReturnTrueIfUserIsAddedInTheEvent()
        {
            //Arrange
            const string userId = "userId";

            var testType = new Data.Models.Type
            {
                Name = "TestType",
            };
            await _dbContext.Types.AddAsync(testType);
            await _dbContext.SaveChangesAsync();

            var testEvent = new Event
            {
                Name = "First Test Event",
                Description = "First Test Description",
                Start = DateTime.Now,
                End = DateTime.Now.AddHours(2),
                TypeId = testType.Id,
                OrganiserId = userId
            };
            await _dbContext.Events.AddAsync(testEvent);
            await _dbContext.SaveChangesAsync();

            //Act
            var result = await _eventService.JoinEventAsync(testEvent.Id, userId);

            //Assert
            Assert.True(result);
        }


        [Test]
        public async Task LeaveEventAsync_ShouldReturnFalseIfWeTryToLeaveEventWeAreNotPartOf()
        {
            const string userId = "user-id";

            var result = await _eventService.LeaveEventAsync(123, userId);

            Assert.False(result);
        }

        [Test]
        public async Task LeaveEventAsync_ShouldReturnTrueIfWeLeaveEvent()
        {
            var testType = new Data.Models.Type
            {
                Name = "TestType",
            };
            await _dbContext.Types.AddAsync(testType);
            await _dbContext.SaveChangesAsync();

            var testEvent = new Event
            {
                Name = "First Test Event",
                Description = "First Test Description",
                Start = DateTime.Now,
                End = DateTime.Now.AddHours(2),
                TypeId = testType.Id,
                OrganiserId = "a-sample-user"
            };
            await _dbContext.Events.AddAsync(testEvent);
            await _dbContext.SaveChangesAsync();

            string userId = "new-participant";

            await _eventService.JoinEventAsync(testEvent.Id, userId);

            //Act
            var result = await _eventService.LeaveEventAsync(testEvent.Id, userId);

            //Arrange
            Assert.True(result);
        }

        [Test]
        public async Task UpdateEventAsync_ShouldReturnFalseIfEventDoesNotExist()
        {
            var result = await _eventService.UpdateEventAsync(999, new EventFormModel { }, "user-id");
            
            Assert.False(result);
        }

        [Test]
        public async Task UpdateEventAsync_ShouldReturnFalseIfTheOrganiserOfEventIsDifferent()
        {

            const string firstUserId = "first-User-Id";
            const string secondUserId = "second-User-Id";
            
            var testType = new Data.Models.Type
            {
                Name = "TestType",
            };
            await _dbContext.Types.AddAsync(testType);
            await _dbContext.SaveChangesAsync();

            var testEvent = new Event
            {
                Name = "First Test Event",
                Description = "First Test Description",
                Start = DateTime.Now,
                End = DateTime.Now.AddHours(2),
                TypeId = testType.Id,
                OrganiserId = "a-sample-user"
            };
            await _dbContext.Events.AddAsync(testEvent);
            await _dbContext.SaveChangesAsync();

            var result = await _eventService.UpdateEventAsync(testEvent.Id, new EventFormModel { }, secondUserId);

            Assert.False(result);
        }

        [Test]
        public async Task UpdateEventAsync_ShouldReturnTrueIfWeUpdateEventSuccessfully()
        {
            const string firstUserId = "first-User-Id";

            var testType = new Data.Models.Type
            {
                Name = "TestType",
            };
            await _dbContext.Types.AddAsync(testType);
            await _dbContext.SaveChangesAsync();

            var testEvent = new Event
            {
                Name = "First Test Event",
                Description = "First Test Description",
                Start = DateTime.Now,
                End = DateTime.Now.AddHours(2),
                TypeId = testType.Id,
                OrganiserId = firstUserId
            };
            await _dbContext.Events.AddAsync(testEvent);
            await _dbContext.SaveChangesAsync();

            var result = await _eventService.UpdateEventAsync(
                testEvent.Id,
                new EventFormModel 
                {
                    Name = "UPDATED",
                    Description = testEvent.Description,
                    Start = testEvent.Start,
                    End = testEvent.End,
                    TypeId = testType.Id,
                },
                firstUserId);

            Assert.True(result);
            
            var eventFromDb = await _dbContext.Events.FirstOrDefaultAsync(x => x.Id == testEvent.Id);

            Assert.That(eventFromDb.Name, Is.EqualTo("UPDATED"));
            Assert.NotNull(eventFromDb);

        }
    }
}
