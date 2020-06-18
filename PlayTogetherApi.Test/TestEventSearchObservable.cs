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
        public async Task TestEventSearch_UserAddingEvent()
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

                    // subscribe to events that the user has joined
                    var queryService = new EventsQueryService
                    {
                        UserId = testUser.UserId,
                        StartsAfterDate = DateTime.Today.AddDays(-1),
                        IncludeJoinedFilter = true,
                        IncludeByUsersFilter = new[] { testUser.UserId } // TODO: THIS SHOULDN'T BE NECESSARY, SINCE THE USER AUTOMATICALLY JOINS THE EVENT!
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


                    // todo: leave the event, and verify that we get an event-removed update
                }
            }
            finally
            {
                sub1?.Dispose();
            }
        }




        [TestMethod]
        public async Task TestEventSearch_FriendAddingEvent()
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

                    // setup friends in advance
                    await interactions.ChangeUserRelationAsync(testUser.UserId, friendUser.UserId, UserRelationAction.Invite);
                    await interactions.ChangeUserRelationAsync(friendUser.UserId, testUser.UserId, UserRelationAction.Accept);

                    var queryService = new EventsQueryService
                    {
                        UserId = testUser.UserId,
                        FriendIds = new List<Guid> { friendUser.UserId },
                        StartsAfterDate = DateTime.Today.AddDays(-1),
                        IncludeByFriendsFilter = true
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

                    // create event, which should be added to our search
                    var newEvent = await interactions.CreateEventAsync(friendUser.UserId, DateTime.UtcNow.AddHours(2), DateTime.UtcNow.AddHours(3), "testevent1", "", false, MockData.Games[0].GameId);

                    Assert.IsNotNull(searchUpdate);
                    Assert.IsNotNull(ecm1);


                    // todo: remove the friendship, and verify that we get an event-removed update




                }
            }
            finally
            {
                sub1?.Dispose();
            }
        }





        [TestMethod]
        public async Task TestEventSearch_EventWhenBefriendingCreator()
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

                    // search for events created by our friends
                    var queryService = new EventsQueryService
                    {
                        UserId = testUser.UserId,
                        FriendIds = new List<Guid> { },
                        StartsAfterDate = DateTime.Today.AddDays(-1),
                        //IncludeJoinedFilter = true,
                        IncludeByFriendsFilter = true
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

                    var newEvent = await interactions.CreateEventAsync(friendUser.UserId, DateTime.UtcNow.AddHours(2), DateTime.UtcNow.AddHours(3), "testevent1", "", false, MockData.Games[0].GameId);

                    Assert.IsNull(searchUpdate);
                    Assert.IsNotNull(ecm1);

                    ecm1 = null;

                    // befriend the user who created the event, which should make the event show up in our search
                    await interactions.ChangeUserRelationAsync(testUser.UserId, friendUser.UserId, UserRelationAction.Invite);
                    await interactions.ChangeUserRelationAsync(friendUser.UserId, testUser.UserId, UserRelationAction.Accept);

                    //Assert.IsNotNull(searchUpdate);       todo, will be set once the friendupdate is updated
                    Assert.IsNull(ecm1);


                }
            }
            finally
            {
                sub1?.Dispose();
            }
        }


        // testcases:
        // - testuser subscribes to friend events, friend creates an event, the event should then be added
        // - testuser subscribes to friend events, unrelated user creates an event, testuser befriends the other user, the event should then be added
        // - testuser subscribes to friend events, friend creates an event, the event should then be added, friend is unfriended, the event should be removed

    }
}
