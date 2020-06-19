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
        public async Task TestEventSearch_UserCreatingEvent_ThenLeaving_ThenRejoining()
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

                    // search for future events that the user has joined
                    var queryService = new EventsQueryService
                    {
                        UserId = testUser.UserId,
                        StartsAfterDate = DateTime.Today.AddDays(-1),
                        IncludeJoinedFilter = true
                    };
                    var searchObservable = new EventSearchObservable(di.ScopedServiceProvider, observables, queryService, new List<Event>());

                    // subscribe to updates from the search (the actual test)
                    EventSearchUpdateModel searchUpdate = null;
                    searchObservable.AsObservable().Subscribe(esum =>
                    {
                        searchUpdate = esum;
                    });

                    // subscribe to eventchanges, as a sanity check
                    EventChangedModel eventChangeUpdate = null;
                    sub1 = observables.GameEventStream.AsObservable().Subscribe(ecm =>
                    {
                        eventChangeUpdate = ecm;
                    });

                    // create event, which should trigger the search to add the event
                    var newEvent = await interactions.CreateEventAsync(testUser.UserId, DateTime.UtcNow.AddHours(2), DateTime.UtcNow.AddHours(3), "testevent1", "", false, MockData.Games[0].GameId);

                    Assert.IsNotNull(eventChangeUpdate);
                    Assert.AreEqual(EventAction.Created, eventChangeUpdate.Action);
                    Assert.AreSame(newEvent, eventChangeUpdate.Event);

                    // verify that the event was added to the search
                    Assert.IsNotNull(searchUpdate);
                    Assert.IsNotNull(searchUpdate.Added);
                    Assert.AreEqual(1, searchUpdate.Added.Count);
                    Assert.IsTrue(searchUpdate.Removed == null || !searchUpdate.Removed.Any());

                    searchUpdate = null;
                    eventChangeUpdate = null;

                    // leave the event, which should cause it to be removed from the search
                    await interactions.LeaveEvent(testUser.UserId, newEvent.EventId);

                    Assert.IsNull(eventChangeUpdate);

                    Assert.IsNotNull(searchUpdate);
                    Assert.IsNotNull(searchUpdate.Removed);
                    Assert.AreEqual(1, searchUpdate.Removed.Count);
                    Assert.IsTrue(searchUpdate.Added == null || !searchUpdate.Added.Any());

                    searchUpdate = null;

                    // rejoin the event, which should cause it to be added to the search
                    await interactions.JoinEvent(testUser.UserId, newEvent.EventId);

                    Assert.IsNull(eventChangeUpdate);

                    Assert.IsNotNull(searchUpdate);
                    Assert.IsNotNull(searchUpdate.Added);
                    Assert.AreEqual(1, searchUpdate.Added.Count);
                    Assert.IsTrue(searchUpdate.Removed == null || !searchUpdate.Removed.Any());
                }
            }
            finally
            {
                sub1?.Dispose();
            }
        }




        [TestMethod]
        public async Task TestEventSearch_FriendCreatingEvent_ThenUnfriending()
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

                    // search for future events that are created by friends
                    var queryService = new EventsQueryService
                    {
                        UserId = testUser.UserId,
                        FriendIds = new List<Guid> { friendUser.UserId },
                        StartsAfterDate = DateTime.Today,
                        IncludeByFriendsFilter = true,
                    };
                    var searchObservable = new EventSearchObservable(di.ScopedServiceProvider, observables, queryService, new List<Event>());

                    // subscribe to updates from the search (the actual test)
                    EventSearchUpdateModel searchUpdate = null;
                    sub1 = searchObservable.AsObservable().Subscribe(o_esum =>
                    {
                        searchUpdate = o_esum;
                    });

                    // subscribe to eventchanges and userrelationchanges, as a sanity check
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
                    Assert.IsTrue(searchUpdate.Removed == null || !searchUpdate.Removed.Any());

                    searchUpdate = null;
                    eventChangeUpdate = null;

                    // remove the friendship, which should trigger the search to remove the event
                    await interactions.ChangeUserRelationAsync(testUser.UserId, friendUser.UserId, UserRelationAction.Reject);
                    await interactions.ChangeUserRelationAsync(friendUser.UserId, testUser.UserId, UserRelationAction.Reject);

                    Assert.IsNotNull(userRelationChangeUpdate);
                    Assert.AreEqual(UserRelationInternalStatus.A_Rejected | UserRelationInternalStatus.B_Rejected, userRelationChangeUpdate.Relation.Status);
                    Assert.AreEqual(testUser.UserId, userRelationChangeUpdate.Relation.UserAId);
                    Assert.AreEqual(friendUser.UserId, userRelationChangeUpdate.Relation.UserBId);

                    Assert.IsNotNull(searchUpdate);
                    Assert.IsNotNull(searchUpdate.Removed);
                    Assert.AreEqual(1, searchUpdate.Removed.Count);
                    Assert.IsTrue(searchUpdate.Added == null || !searchUpdate.Added.Any());

                    searchUpdate = null;
                    eventChangeUpdate = null;

                    // re-add the friendship, which should trigger the search to re-add the event
                    await interactions.ChangeUserRelationAsync(testUser.UserId, friendUser.UserId, UserRelationAction.Invite);
                    await interactions.ChangeUserRelationAsync(friendUser.UserId, testUser.UserId, UserRelationAction.Accept);

                    // wait for the async processing of the search
                    await Task.Delay(200);

                    Assert.IsNotNull(userRelationChangeUpdate);
                    Assert.AreEqual(UserRelationInternalStatus.A_Befriended| UserRelationInternalStatus.B_Befriended, userRelationChangeUpdate.Relation.Status);
                    Assert.AreEqual(testUser.UserId, userRelationChangeUpdate.Relation.UserAId);
                    Assert.AreEqual(friendUser.UserId, userRelationChangeUpdate.Relation.UserBId);

                    // verify that the event was added to the search
                    Assert.IsNotNull(searchUpdate);
                    Assert.IsNotNull(searchUpdate.Added);
                    Assert.AreEqual(1, searchUpdate.Added.Count);
                    Assert.IsTrue(searchUpdate.Removed == null || !searchUpdate.Removed.Any());
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

                    var searchObservable = new EventSearchObservable(di.ScopedServiceProvider, observables, queryService, new List<Event>());

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
