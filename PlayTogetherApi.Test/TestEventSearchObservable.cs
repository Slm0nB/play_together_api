using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlayTogetherApi.Data;
using PlayTogetherApi.Services;
using PlayTogetherApi.Models;

namespace PlayTogetherApi.Test
{
    [TestClass]
    public sealed class TestEventSearchObservable : IDisposable
    {
        DependencyInjection di;
        int delay = 25;

        public TestEventSearchObservable()
        {
            di = new DependencyInjection();
        }

        public void Dispose()
        {
            di?.Dispose();
        }

        /// <summary>
        /// We should be able to subscribe to a search for events we have joined.
        /// Creating an event (which is automatically joined) should add the event to the search.
        /// Leaving a joined event should remove the event from the search.
        /// Joining an event should add the event to the search.
        /// </summary>
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
                    sub1 = observables.GetGameEventStream().Subscribe(ecm =>
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
                    await interactions.LeaveEventAsync(testUser.UserId, newEvent.EventId);

                    Assert.IsNull(eventChangeUpdate);

                    Assert.IsNotNull(searchUpdate);
                    Assert.IsNotNull(searchUpdate.Removed);
                    Assert.AreEqual(1, searchUpdate.Removed.Count);
                    Assert.IsTrue(searchUpdate.Added == null || !searchUpdate.Added.Any());

                    searchUpdate = null;

                    // rejoin the event, which should cause it to be added to the search
                    await interactions.JoinEventAsync(testUser.UserId, newEvent.EventId);

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

        /// <summary>
        /// We should be able to subscribe to a search for events created by friends.
        /// Friends creating new events should update the search.
        /// Unfriending a user who has created events, should remove those events from the search.
        /// Befrending a user who has created events, should add those events to the search.
        /// </summary>
        [TestMethod]
        public async Task TestEventSearch_FriendCreatingEvent_ThenUnfriending_ThenRefriending()
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
                    sub2 = observables.GetGameEventStream().Subscribe(o_ecm =>
                    {
                        eventChangeUpdate = o_ecm;
                    });

                    UserRelationChangedModel userRelationChangeUpdate = null;
                    sub3 = observables.GetUserRelationChangeStream().Subscribe(o_urcm => {
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
                    await Task.Delay(delay);

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

        /// <summary>
        /// We should be able to subscribe to a search for events joined by friends.
        /// A friend creating an event (and thereby joining it) should add the event to the search.
        /// A friend joining an event should add the event to the search.
        /// A friend leaving an event should remove the event from the search.
        /// </summary>
        [TestMethod]
        public async Task TestEventSearch_FriendJoinsEvent_ThenLeavesEvent_ThenRejoins()
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

                    // search for events joined by our friends
                    var queryService = new EventsQueryService
                    {
                        UserId = testUser.UserId,
                        FriendIds = new List<Guid> { friendUser.UserId },
                        StartsAfterDate = DateTime.Today,
                        IncludeJoinedByFriendsFilter = true
                    };
                    var searchObservable = new EventSearchObservable(di.ScopedServiceProvider, observables, queryService, new List<Event>());

                    // subscribe to updates from the search (the actual test)
                    EventSearchUpdateModel searchUpdate = null;
                    sub1 = searchObservable.AsObservable().Subscribe(esum =>
                    {
                        searchUpdate = esum;
                    });

                    // create event for friend, which shouldn't be added to the search yet, since the users aren't friends
                    var newEvent = await interactions.CreateEventAsync(friendUser.UserId, DateTime.UtcNow.AddHours(2), DateTime.UtcNow.AddHours(3), "testevent1", "", false, MockData.Games[0].GameId);

                    Assert.IsNotNull(searchUpdate);
                    Assert.IsNotNull(searchUpdate.Added);
                    Assert.AreEqual(1, searchUpdate.Added.Count);
                    Assert.AreSame(newEvent, searchUpdate.Added[0]);
                    Assert.IsTrue(searchUpdate.Removed == null || !searchUpdate.Removed.Any());

                    searchUpdate = null;

                    // friend leaves their own event, which should trigger the search to remove the event
                    await interactions.LeaveEventAsync(friendUser.UserId, newEvent.EventId);

                    Assert.IsNotNull(searchUpdate);
                    Assert.IsNotNull(searchUpdate.Removed);
                    Assert.AreEqual(1, searchUpdate.Removed.Count);
                    Assert.AreSame(newEvent, searchUpdate.Removed[0]);
                    Assert.IsTrue(searchUpdate.Added == null || !searchUpdate.Added.Any());

                    searchUpdate = null;

                    // friend rejoins their own event, which should trigger the search to re-add the event
                    await interactions.JoinEventAsync(friendUser.UserId, newEvent.EventId);

                    Assert.IsNotNull(searchUpdate);
                    Assert.IsNotNull(searchUpdate.Added);
                    Assert.AreEqual(1, searchUpdate.Added.Count);
                    Assert.AreSame(newEvent, searchUpdate.Added[0]);
                    Assert.IsTrue(searchUpdate.Removed == null || !searchUpdate.Removed.Any());

                    searchUpdate = null;

                    // create event, which shouldn't be added to the search yet, since the no friend has joined it
                    newEvent = await interactions.CreateEventAsync(testUser.UserId, DateTime.UtcNow.AddHours(2), DateTime.UtcNow.AddHours(3), "testevent1", "", false, MockData.Games[0].GameId);

                    Assert.IsNull(searchUpdate);

                    // friend joins the new event, which should trigger the search to add the event
                    await interactions.JoinEventAsync(friendUser.UserId, newEvent.EventId);

                    Assert.IsNotNull(searchUpdate);
                    Assert.IsNotNull(searchUpdate.Added);
                    Assert.AreEqual(1, searchUpdate.Added.Count);
                    Assert.AreSame(newEvent, searchUpdate.Added[0]);
                    Assert.IsTrue(searchUpdate.Removed == null || !searchUpdate.Removed.Any());
                }
            }
            finally
            {
                sub1?.Dispose();
            }
        }


        /// <summary>
        /// We should be able to subscribe to a search for events joined by friends.
        /// Befriending a user who has joined an event, should add the event to the search.
        /// Unfriending the user should cause the event to be removed from the search.
        /// </summary>
        [TestMethod]
        public async Task TestEventSearch_OtherUserJoinsEvent_BefriendUser_ThenUnfriending()
        {
            IDisposable sub1 = null;
            try
            {
                using (var db = di.GetService<PlayTogetherDbContext>())
                {
                    var observables = di.GetService<ObservablesService>();
                    var interactions = di.GetService<InteractionsService>();
                    interactions.EnablePushMessages = false;

                    await MockData.PopulateDbAsync(db, force: true, addEvents: false);

                    var testUser = MockData.Users[0];
                    var friendUser = MockData.Users[1];

                    // search for events joined by our friends
                    var queryService = new EventsQueryService
                    {
                        UserId = testUser.UserId,
                        FriendIds = new List<Guid> { },
                        StartsAfterDate = DateTime.Today,
                        IncludeJoinedByFriendsFilter = true
                    };
                    var searchObservable = new EventSearchObservable(di.ScopedServiceProvider, observables, queryService, new List<Event>());

                    // subscribe to updates from the search (the actual test)
                    EventSearchUpdateModel searchUpdate = null;
                    sub1 = searchObservable.AsObservable().Subscribe(esum =>
                    {
                        searchUpdate = esum;
                    });

                    // create event, which shouldn't be added to the search yet, since no friends have joined.
                    var newEvent = await interactions.CreateEventAsync(testUser.UserId, DateTime.UtcNow.AddHours(2), DateTime.UtcNow.AddHours(3), "testevent1", "", false, MockData.Games[0].GameId);

                    Assert.IsNull(searchUpdate);

                    // other user joins the event
                    await interactions.JoinEventAsync(friendUser.UserId, newEvent.EventId);

                    Assert.IsNull(searchUpdate);

                    // setup friends, which should cause the event to be added to the search
                    await interactions.ChangeUserRelationAsync(testUser.UserId, friendUser.UserId, UserRelationAction.Invite);
                    await interactions.ChangeUserRelationAsync(friendUser.UserId, testUser.UserId, UserRelationAction.Accept);

                    // wait for the async processing of the search
                    await Task.Delay(delay);

                    // verify that the event was added to the search
                    Assert.IsNotNull(searchUpdate);
                    Assert.IsNotNull(searchUpdate.Added);
                    Assert.AreEqual(1, searchUpdate.Added.Count);
                    Assert.AreEqual(newEvent.EventId, searchUpdate.Added[0].EventId);
                    Assert.IsTrue(searchUpdate.Removed == null || !searchUpdate.Removed.Any());

                    searchUpdate = null;

                    // remove friends, which should cause the event to be removed from the search
                    await interactions.ChangeUserRelationAsync(testUser.UserId, friendUser.UserId, UserRelationAction.Reject);
                    await interactions.ChangeUserRelationAsync(friendUser.UserId, testUser.UserId, UserRelationAction.Reject);

                    Assert.IsNotNull(searchUpdate);
                    Assert.IsNotNull(searchUpdate.Removed);
                    Assert.AreEqual(1, searchUpdate.Removed.Count);
                    Assert.AreEqual(newEvent.EventId, searchUpdate.Removed[0].EventId);
                    Assert.IsTrue(searchUpdate.Added == null || !searchUpdate.Added.Any());
                }
            }
            finally
            {
                sub1?.Dispose();
            }
        }

        /// <summary>
        /// We should be able to subscribe to a search for events, and see friend-only events created by our friends.
        /// Befriending a user who has created friend-only events, should add those events to the search.
        /// Unfriending a user who has created friend-only events, should remove those events from the search.
        /// </summary>
        [TestMethod]
        public async Task TestEventSearch_BefriendingCreatorOfFriendOnlyEvent_ThenUnfriending()
        {
            IDisposable sub1 = null;
            try
            {
                using (var db = di.GetService<PlayTogetherDbContext>())
                {
                    var observables = di.GetService<ObservablesService>();
                    var interactions = di.GetService<InteractionsService>();
                    interactions.EnablePushMessages = false;

                    await MockData.PopulateDbAsync(db, force: true, addEvents: false);

                    var testUser = MockData.Users[0];
                    var friendUser = MockData.Users[1];

                    // search for events joined by our friends
                    var queryService = new EventsQueryService
                    {
                        UserId = testUser.UserId,
                        FriendIds = new List<Guid> { },
                        StartsAfterDate = DateTime.Today,
                        IncludeJoinedByFriendsFilter = true
                    };
                    var searchObservable = new EventSearchObservable(di.ScopedServiceProvider, observables, queryService, new List<Event>());

                    // subscribe to updates from the search (the actual test)
                    EventSearchUpdateModel searchUpdate = null;
                    sub1 = searchObservable.AsObservable().Subscribe(esum =>
                    {
                        searchUpdate = esum;
                    });

                    // create event, which shouldn't trigger the search to add the event since we're not friends
                    var newEvent = await interactions.CreateEventAsync(friendUser.UserId, DateTime.UtcNow.AddHours(25), DateTime.UtcNow.AddHours(26), "testevent1", "", true, MockData.Games[0].GameId);

                    Assert.IsNull(searchUpdate);

                    // setup friends, which should cause the joined event to be added to the search
                    await interactions.ChangeUserRelationAsync(testUser.UserId, friendUser.UserId, UserRelationAction.Invite);
                    await interactions.ChangeUserRelationAsync(friendUser.UserId, testUser.UserId, UserRelationAction.Accept);

                    // wait for the async processing of the search
                    await Task.Delay(delay);

                    // verify that the event was added to the search
                    Assert.IsNotNull(searchUpdate);
                    Assert.IsNotNull(searchUpdate.Added);
                    Assert.AreEqual(1, searchUpdate.Added.Count);
                    Assert.AreEqual(newEvent.EventId, searchUpdate.Added[0].EventId);
                    Assert.IsTrue(searchUpdate.Removed == null || !searchUpdate.Removed.Any());

                    // remove friends, which should cause the event to be removed from the search
                    await interactions.ChangeUserRelationAsync(testUser.UserId, friendUser.UserId, UserRelationAction.Reject);
                    await interactions.ChangeUserRelationAsync(friendUser.UserId, testUser.UserId, UserRelationAction.Reject);

                    Assert.IsNotNull(searchUpdate);
                    Assert.IsNotNull(searchUpdate.Removed);
                    Assert.AreEqual(1, searchUpdate.Removed.Count);
                    Assert.AreEqual(newEvent.EventId, searchUpdate.Removed[0].EventId);
                    Assert.IsTrue(searchUpdate.Added == null || !searchUpdate.Added.Any());
                }
            }
            finally
            {
                sub1?.Dispose();
            }
        }

    }
}
