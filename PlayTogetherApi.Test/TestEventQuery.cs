using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlayTogetherApi.Data;
using PlayTogetherApi.Services;
using PlayTogetherApi.Web.Services;

namespace PlayTogetherApi.Test
{
    [TestClass]
    public class TestEventQuery
    {
        static List<User> Users = new List<User>()
        {
            new User
            {
                DisplayName = "User1",
                UserId = Guid.Parse("00000000-0000-0000-0001-000000000000")
            },
            new User
            {
                DisplayName = "User2",
                UserId = Guid.Parse("00000000-0000-0000-0002-000000000000")
            }
        };

        static List<Game> Games = new List<Game>()
        {
            new Game
            {
                Title = "Overwatch",
                GameId = Guid.Parse("00000000-0000-0001-0000-000000000000")
            },
            new Game
            {
                Title = "GTFO",
                GameId = Guid.Parse("00000000-0000-0002-0000-000000000000")
            }
        };

        static List<Event> Events = new List<Event>()
        {
            new Event {
                Title = "Yesterdays event",
                CreatedByUserId = Users[0].UserId,
                CreatedByUser = Users[0],
                EventId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                GameId = Games[0].GameId,
                Game = Games[0],
                EventDate = DateTime.UtcNow.AddHours(-24),
                EventEndDate = DateTime.UtcNow.AddHours(-23)
            },
            new Event {
                Title = "Tomorrows event",
                CreatedByUserId = Users[1].UserId,
                CreatedByUser = Users[1],
                EventId = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                GameId = Games[1].GameId,
                Game = Games[1],
                EventDate = DateTime.UtcNow.AddHours(24),
                EventEndDate = DateTime.UtcNow.AddHours(25)
           }
        };

        [TestMethod]
        public void QueryFutureEvents()
        {
            var query = Events.AsQueryable();

            var queryService = new EventsQueryService
            {
                StartsAfterDate = DateTime.UtcNow
            };

            query = queryService.Process(query);

            var result = query.ToArray();

            Assert.AreEqual(1, result.Count());
            Assert.AreSame(Events[1], result[0]);
        }
    }
}
