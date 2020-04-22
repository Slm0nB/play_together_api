using System;
using System.Collections.Generic;
using PlayTogetherApi.Data;

namespace PlayTogetherApi.Test
{
    public static class MockData
    {
        public static User[] Users = new []
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

        public static Game[] Games = new []
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

        public static Event[] Events = new []
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
    }
}
