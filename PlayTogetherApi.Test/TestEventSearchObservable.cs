using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlayTogetherApi.Data;
using PlayTogetherApi.Services;
using PlayTogetherApi.Web.Models;
using PlayTogetherApi.Web.Services;

namespace PlayTogetherApi.Test
{
    [TestClass]
    public sealed class TestEventSearchObservable : IDisposable
    {
        DependencyInjection di;

        public TestEventSearchObservable()
        {
            di = new DependencyInjection();
        }

        public void Dispose()
        {
            di?.Dispose();
        }


        [TestMethod]
        public async Task TestEventSearch()
        {
            using (var db = di.GetService<PlayTogetherDbContext>())
            {
                await MockData.PopulateDbAsync(db);

                var testUser = MockData.Users[0];
                var friendUser = MockData.Users[1];

                // todo: setup a friend for the test user

                var queryService = new EventsQueryService
                {
                    UserId = testUser.UserId,
                    FriendIds = new System.Collections.Generic.List<Guid> { friendUser.UserId },
                    StartsAfterDate = DateTime.Today.AddDays(-1),
                    IncludeJoinedFilter = true
                };

                // todo: populate queryService, events joined by friends

                // todo: subscripbe to EventSearchObservable

                // todo: move the "join event" logic into the interaction service

                // todo: make the friend of the test-user join an event
                // todo: validate that the observable added the event, since the friend joined

                // todo: remove the user-relation
                // todo: validate that the observable removed the event since the friend disappeared

                // todo: add the user-relation
                // todo: validate that the observable added the event since the friend was added

                // todo: make the friend leave the event
                // todo: observe that the observable removed the event since the friend is no longer joined

            }
        }
    }
}
