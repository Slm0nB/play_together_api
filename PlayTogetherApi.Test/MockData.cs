using System;
using System.Collections.Generic;
using PlayTogetherApi.Data;

namespace PlayTogetherApi.Test
{
    public static class MockData
    {
        public static User[] Users;
        public static Game[] Games;
        public static Event[] Events;

        static MockData()
        {
            Users = new[]
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
                },
                new User
                {
                    DisplayName = "User3",
                    UserId = Guid.Parse("00000000-0000-0000-0001-000000000003")
                },
                new User
                {
                    DisplayName = "User4",
                    UserId = Guid.Parse("00000000-0000-0000-0002-000000000004")
                }
            };

            Games = new[]
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
                },
                new Game
                {
                    Title = "WWZ",
                    GameId = Guid.Parse("00000000-0000-0003-0000-000000000000")
                }
            };

            Events = new[]
            {
                new Event {
                    Title = "Yesterdays event 1",
                    Description = "",
                    CreatedByUserId = Users[0].UserId,
                    CreatedByUser = Users[0],
                    EventId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    GameId = Games[0].GameId,
                    Game = Games[0],
                    EventDate = DateTime.UtcNow.AddHours(-24),
                    EventEndDate = DateTime.UtcNow.AddHours(-23)
                },
                new Event {
                    Title = "Yesterdays event 2",
                    Description = "",
                    CreatedByUserId = Users[1].UserId,
                    CreatedByUser = Users[1],
                    EventId = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                    GameId = Games[1].GameId,
                    Game = Games[1],
                    EventDate = DateTime.UtcNow.AddHours(-20),
                    EventEndDate = DateTime.UtcNow.AddHours(-19)
                },
                new Event {
                    Title = "Tomorrows event 1",
                    Description = "",
                    CreatedByUserId = Users[2].UserId,
                    CreatedByUser = Users[2],
                    EventId = Guid.Parse("00000000-0000-0000-0000-000000000003"),
                    GameId = Games[1].GameId,
                    Game = Games[1],
                    EventDate = DateTime.UtcNow.AddHours(24),
                    EventEndDate = DateTime.UtcNow.AddHours(25)
               },
               new Event {
                    Title = "Tomorrows event 2",
                    Description = "",
                    CreatedByUserId = Users[3].UserId,
                    CreatedByUser = Users[3],
                    EventId = Guid.Parse("00000000-0000-0000-0000-000000000004"),
                    GameId = Games[2].GameId,
                    Game = Games[2],
                    EventDate = DateTime.UtcNow.AddHours(28),
                    EventEndDate = DateTime.UtcNow.AddHours(29)
               },
               new Event {
                    Title = "Tomorrows event 3",
                    Description = "",
                    CreatedByUserId = Users[3].UserId,
                    CreatedByUser = Users[3],
                    EventId = Guid.Parse("00000000-0000-0000-0000-000000000005"),
                    GameId = Games[2].GameId,
                    Game = Games[2],
                    EventDate = DateTime.UtcNow.AddHours(38),
                    EventEndDate = DateTime.UtcNow.AddHours(39),
                    FriendsOnly = true
               }
            };

            AddSignup(Events[0], Events[0].CreatedByUser);
            AddSignup(Events[0], Users[1]);

            AddSignup(Events[1], Events[1].CreatedByUser);
            AddSignup(Events[1], Users[0]);

            AddSignup(Events[2], Events[2].CreatedByUser);
            AddSignup(Events[2], Users[0]);
            AddSignup(Events[2], Users[1]);

            AddSignup(Events[3], Events[3].CreatedByUser);
            AddSignup(Events[3], Users[1]);
            AddSignup(Events[3], Users[2]);

            AddSignup(Events[4], Events[4].CreatedByUser);
            AddSignup(Events[4], Users[0]);
            AddSignup(Events[4], Users[2]);
        }

        static private void AddSignup(Event evt, User usr, UserEventStatus status = UserEventStatus.AcceptedInvitation)
        {
            var signup = new UserEventSignup
            {
                EventId = evt.EventId,
                Event = evt,
                Status = status,
                UserId = usr.UserId,
                User = usr
            };
            evt.Signups.Add(signup);
            usr.Signups.Add(signup);
        }
    }
}
