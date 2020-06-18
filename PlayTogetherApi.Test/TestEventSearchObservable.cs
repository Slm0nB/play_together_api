using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
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
            IDisposable sub1 = null;
            try
            {
                using (var db = di.GetService<PlayTogetherDbContext>())
                {
                    var observables = di.GetService<ObservablesService>();
                    var interactions = di.GetService<InteractionsService>();
                    interactions.EnablePushMessages = false;

                    await MockData.PopulateDbAsync(db, true);

                    var testUser = MockData.Users[0];
                    var friendUser = MockData.Users[1];

                    await interactions.ChangeUserRelationAsync(testUser.UserId, friendUser.UserId, UserRelationAction.Invite);
                    await interactions.ChangeUserRelationAsync(friendUser.UserId, testUser.UserId, UserRelationAction.Accept);

                    var queryService = new EventsQueryService
                    {
                        UserId = testUser.UserId,
                        FriendIds = new List<Guid> { friendUser.UserId },
                        StartsAfterDate = DateTime.Today.AddDays(-1),
                        IncludeJoinedFilter = true,
                        IncludeByUsersFilter = new [] { testUser.UserId }
                    };

                    var searchObservable = new EventSearchObservable(observables, queryService, new List<Event>());

                    EventSearchUpdateModel searchUpdate = null;
                    searchObservable.AsObservable().Subscribe(esum =>
                    {
                        searchUpdate = esum;
                    });

                    EventChangedModel ecm1 = null;
                    sub1 = observables.GameEventStream.AsObservable().Subscribe(ecm =>
                    {
                        ecm1 = ecm;
                    });

                    var newEvent = await interactions.CreateEventAsync(testUser.UserId, DateTime.UtcNow.AddHours(2), DateTime.UtcNow.AddHours(3), "testevent1", "", false, MockData.Games[0].GameId);

                    Assert.IsNotNull(searchUpdate);
                    Assert.IsNotNull(ecm1);


                    // todo: verify queryservice also emitted it




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
            finally
            {
                sub1?.Dispose();
            }
        }
    }
}
