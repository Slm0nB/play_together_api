using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
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

                    EventChangedModel eventChangeUpdate = null;
                    sub1 = observables.GameEventStream.AsObservable().Subscribe(ecm =>
                    {
                        eventChangeUpdate = ecm;
                    });

                    // create event, which shoudl trigger the search to add the event
                    var newEvent = await interactions.CreateEventAsync(testUser.UserId, DateTime.UtcNow.AddHours(2), DateTime.UtcNow.AddHours(3), "testevent1", "", false, MockData.Games[0].GameId);

                    Assert.IsNotNull(eventChangeUpdate);
                    Assert.AreEqual(EventAction.Created, eventChangeUpdate.Action);
                    Assert.AreSame(newEvent, eventChangeUpdate.Event);

                    // verify that the event was added to the search
                    Assert.IsNotNull(searchUpdate);
                    Assert.IsNotNull(searchUpdate.Added);
                    Assert.AreEqual(1, searchUpdate.Added.Count);
                    Assert.IsNull(searchUpdate.Removed);

                    searchUpdate = null;
                    eventChangeUpdate = null;

                    // leave the event, which should cause it to 
                    await interactions.LeaveEvent(testUser.UserId, newEvent.EventId);


                    // todo: leave the event, and verify that we get an event-removed update
                    // todo: move joinevent and leaveevent into the interactions-service
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
            IDisposable sub1 = null, sub2 = null, sub3 = null;
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
                    sub1 = searchObservable.AsObservable().Subscribe(o_esum =>
                    {
                        searchUpdate = o_esum;
                    });

                    EventChangedModel eventChangeUpdate = null;
                    sub2 = observables.GameEventStream.AsObservable().Subscribe(o_ecm =>
                    {
                        eventChangeUpdate = o_ecm;
                    });

                    UserRelationChangedModel userRelationChangeUpdate = null;
                    sub3 = observables.UserRelationChangeStream.AsObservable().Subscribe(o_urcm => {
                        userRelationChangeUpdate = o_urcm;
                    });

                    // create event, which should trigger the search to add the event
                    var newEvent = await interactions.CreateEventAsync(friendUser.UserId, DateTime.UtcNow.AddHours(2), DateTime.UtcNow.AddHours(3), "testevent1", "", false, MockData.Games[0].GameId);

                    Assert.IsNull(userRelationChangeUpdate);
                    Assert.IsNotNull(eventChangeUpdate);
                    Assert.AreEqual(EventAction.Created, eventChangeUpdate.Action);
                    Assert.AreSame(newEvent, eventChangeUpdate.Event);

                    // verify that the event was added to the search
                    Assert.IsNotNull(searchUpdate);
                    Assert.IsNotNull(searchUpdate.Added);
                    Assert.AreEqual(1, searchUpdate.Added.Count);
                    Assert.IsNull(searchUpdate.Removed);

                    searchUpdate = null;
                    eventChangeUpdate = null;

                    // remove the friendship, which should trigger the search to remove the event
                    await interactions.ChangeUserRelationAsync(testUser.UserId, friendUser.UserId, UserRelationAction.Reject);
                    await interactions.ChangeUserRelationAsync(friendUser.UserId, testUser.UserId, UserRelationAction.Reject);

                    Assert.IsNotNull(userRelationChangeUpdate);

                    // todo: verify that the event was removed from the search
                    Assert.IsNotNull(userRelationChangeUpdate);
                    Assert.AreEqual(UserRelationInternalStatus.A_Rejected | UserRelationInternalStatus.B_Rejected, userRelationChangeUpdate.Relation.Status);
                    //Assert.IsNotNull(searchUpdate);
                    //Assert.IsNotNull(eventChangeUpdate);



                }
            }
            finally
            {
                sub1?.Dispose();
                sub2?.Dispose();
                sub3?.Dispose();
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

                    EventChangedModel eventChangeUpdate = null;
                    sub1 = observables.GameEventStream.AsObservable().Subscribe(ecm =>
                    {
                        eventChangeUpdate = ecm;
                    });

                    // create event, which shouldn't be added to the search yet, since the users aren't friends
                    var newEvent = await interactions.CreateEventAsync(friendUser.UserId, DateTime.UtcNow.AddHours(2), DateTime.UtcNow.AddHours(3), "testevent1", "", false, MockData.Games[0].GameId);

                    Assert.IsNull(searchUpdate);
                    Assert.IsNotNull(eventChangeUpdate);
                    Assert.AreEqual(EventAction.Created, eventChangeUpdate.Action);

                    eventChangeUpdate = null;

                    // befriend the user who created the event, which should make the event show up in our search
                    await interactions.ChangeUserRelationAsync(testUser.UserId, friendUser.UserId, UserRelationAction.Invite);
                    await interactions.ChangeUserRelationAsync(friendUser.UserId, testUser.UserId, UserRelationAction.Accept);

                    //Assert.IsNotNull(searchUpdate);       todo, will be set once the friendupdate is updated
                    Assert.IsNull(eventChangeUpdate);


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
