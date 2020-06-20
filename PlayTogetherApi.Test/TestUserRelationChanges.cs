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
    public class TestUserRelationChanges
    {
        DependencyInjection di;

        public TestUserRelationChanges()
        {
            di = new DependencyInjection();
        }

        /// <summary>
        /// We should be able to subscribe to created/deleted events.
        /// Befriending a user who has created friendsonly events, should make those events appear in the subscription.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task TestFriendOnlyEventsWhenChangingUserRelations()
        {
            IDisposable sub1 = null, sub2 = null;
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

                    EventChangedModel eventChange1 = null;
                    sub1 = observables.GetEventsSubscriptioon(testUser.UserId).Subscribe(ecm =>
                    {
                        eventChange1 = ecm;
                    });

                    EventChangedModel eventChange2 = null;
                    sub2 = observables.GetEventsSubscriptioon(friendUser.UserId).Subscribe(ecm =>
                    {
                        eventChange2 = ecm;
                    });

                    // create a friends-only event
                    var newEvent1 = await interactions.CreateEventAsync(friendUser.UserId, DateTime.UtcNow.AddHours(2), DateTime.UtcNow.AddHours(3), "testevent1", "", true, MockData.Games[0].GameId);

                    eventChange1 = null;
                    eventChange2 = null;

                    // setup the user-relation, causing the friend-only event to become visible
                    await interactions.ChangeUserRelationAsync(testUser.UserId, friendUser.UserId, UserRelationAction.Invite);
                    await interactions.ChangeUserRelationAsync(friendUser.UserId, testUser.UserId, UserRelationAction.Accept);

                    Assert.IsNotNull(eventChange1);
                    Assert.AreEqual(EventAction.Created, eventChange1.Action);
                    Assert.AreSame(newEvent1, eventChange1.Event);
                    Assert.IsNull(eventChange2);

                    eventChange1 = null;
                    eventChange2 = null;

                    // remove the user-relation, causing the friend-only event to become hidden
                    await interactions.ChangeUserRelationAsync(testUser.UserId, friendUser.UserId, UserRelationAction.Reject);

                    Assert.IsNotNull(eventChange1);
                    Assert.AreEqual(EventAction.Deleted, eventChange1.Action);
                    Assert.AreSame(newEvent1, eventChange1.Event);
                    Assert.IsNull(eventChange2);

                    // todo: repeat with an event from the other user

                    // create a friends-only event
                    var newEvent2 = await interactions.CreateEventAsync(testUser.UserId, DateTime.UtcNow.AddHours(2), DateTime.UtcNow.AddHours(3), "testevent1", "", true, MockData.Games[0].GameId);

                    eventChange1 = null;
                    eventChange2 = null;

                    // add the user-relation, causing both friend-only events to become visible
                    await interactions.ChangeUserRelationAsync(testUser.UserId, friendUser.UserId, UserRelationAction.Invite);
                    await interactions.ChangeUserRelationAsync(friendUser.UserId, testUser.UserId, UserRelationAction.Accept);

                    Assert.IsNotNull(eventChange1);
                    Assert.AreEqual(EventAction.Created, eventChange1.Action);
                    Assert.AreSame(newEvent1, eventChange1.Event);

                    Assert.IsNotNull(eventChange2);
                    Assert.AreEqual(EventAction.Created, eventChange2.Action);
                    Assert.AreSame(newEvent2, eventChange2.Event);

                    eventChange1 = null;
                    eventChange2 = null;

                    // remove the user-relation, causing both friend-only event to become hidden
                    await interactions.ChangeUserRelationAsync(testUser.UserId, friendUser.UserId, UserRelationAction.Reject);

                    Assert.IsNotNull(eventChange1);
                    Assert.AreEqual(EventAction.Deleted, eventChange1.Action);
                    Assert.AreSame(newEvent1, eventChange1.Event);

                    Assert.IsNotNull(eventChange2);
                    Assert.AreEqual(EventAction.Deleted, eventChange2.Action);
                    Assert.AreSame(newEvent2, eventChange2.Event);
                }
            }
            finally
            {
                sub1?.Dispose();
                sub2?.Dispose();
            }
        }

        /// <summary>
        /// Unfriending a user who has friendonly events that wer are signed up for, should cause us to leave those events.
        /// </summary>
        [TestMethod]
        public async Task TestLeavingFriendOnlyEventsWhenUnfriendingOwner()
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

                    var user1 = MockData.Users[0];
                    var user2 = MockData.Users[1];

                    UserEventSignup eventChange = null;
                    sub1 = observables.UserEventSignupStream.AsObservable().Subscribe(ues =>
                    {
                        eventChange = ues;
                    });

                    // setup the user-relation
                    await interactions.ChangeUserRelationAsync(user1.UserId, user2.UserId, UserRelationAction.Invite);
                    await interactions.ChangeUserRelationAsync(user2.UserId, user1.UserId, UserRelationAction.Accept);

                    // user2 creates a friends-only event
                    var newEvent = await interactions.CreateEventAsync(user2.UserId, DateTime.UtcNow.AddHours(2), DateTime.UtcNow.AddHours(3), "testevent1", "", true, MockData.Games[0].GameId);

                    // sign user1 up to the event created by user2
                    await interactions.JoinEventAsync(user1.UserId, newEvent.EventId);

                    // we should get a notification about the signup
                    Assert.IsNotNull(eventChange);
                    Assert.AreEqual(UserEventStatus.AcceptedInvitation, eventChange.Status);
                    Assert.AreSame(newEvent, eventChange.Event);
                    Assert.AreSame(user1, eventChange.User);
                    eventChange = null;

                    // user1 unfriends
                    await interactions.ChangeUserRelationAsync(user1.UserId, user2.UserId, UserRelationAction.Reject);

                    // we should get a notification about the signup being removed
                    Assert.IsNotNull(eventChange);
                    Assert.AreEqual(UserEventStatus.Cancelled, eventChange.Status);
                    Assert.AreSame(newEvent, eventChange.Event);
                    Assert.AreSame(user1, eventChange.User);
                    eventChange = null;

                    // setup the user-relation
                    await interactions.ChangeUserRelationAsync(user1.UserId, user2.UserId, UserRelationAction.Invite);
                    await interactions.ChangeUserRelationAsync(user2.UserId, user1.UserId, UserRelationAction.Accept);

                    // sign user1 up to the event created by user2
                    await interactions.JoinEventAsync(user1.UserId, newEvent.EventId);

                    // we should get a notification about the signup
                    Assert.IsNotNull(eventChange);
                    Assert.AreEqual(UserEventStatus.AcceptedInvitation, eventChange.Status);
                    Assert.AreSame(newEvent, eventChange.Event);
                    Assert.AreSame(user1, eventChange.User);
                    eventChange = null;

                    // user2 unfriends
                    await interactions.ChangeUserRelationAsync(user2.UserId, user1.UserId, UserRelationAction.Reject);

                    // we should get a notification about the signup being removed
                    Assert.IsNotNull(eventChange);
                    Assert.AreEqual(UserEventStatus.Cancelled, eventChange.Status);
                    Assert.AreSame(newEvent, eventChange.Event);
                    Assert.AreSame(user1, eventChange.User);
                    eventChange = null;
                }
            }
            finally
            {
                sub1?.Dispose();
            }
        }
    }
}
