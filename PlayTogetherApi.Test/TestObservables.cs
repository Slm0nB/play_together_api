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
    public class TestObservables
    {
        DependencyInjection di;

        public TestObservables()
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

                    await MockData.PopulateDbAsync(db, force: true, addEvents: false);

                    var testUser = MockData.Users[0];
                    var friendUser = MockData.Users[1];

                    // signup for the GameEventStream observable (the real test)
                    EventChangedModel eventChange1 = null;
                    sub1 = observables.GetEventsStream(testUser.UserId).Subscribe(ecm =>
                    {
                        eventChange1 = ecm;
                    });

                    EventChangedModel eventChange2 = null;
                    sub2 = observables.GetEventsStream(friendUser.UserId).Subscribe(ecm =>
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

                    // sign up for the UserEventSignup observable (the real test)
                    UserEventSignup eventChange = null;
                    sub1 = observables.GetUserEventSignupStream().Subscribe(ues =>
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

        /// <summary>
        /// When we delete a user, we should get observable notification for the deletion of all its events and relations and signups.
        /// </summary>
        [TestMethod]
        public async Task TestDeletingUser()
        {
            IDisposable sub1 = null, sub2 = null, sub3 = null, sub4 = null;
            try
            {
                using (var db = di.GetService<PlayTogetherDbContext>())
                {
                    var observables = di.GetService<ObservablesService>();
                    var interactions = di.GetService<InteractionsService>();
                    interactions.EnablePushMessages = false;

                    await MockData.PopulateDbAsync(db, force: true, addEvents: false);

                    var userToBeDeleted = MockData.Users[0];
                    var user2 = MockData.Users[1];

                    // setup the user-relation that'll be deleted
                    await interactions.ChangeUserRelationAsync(userToBeDeleted.UserId, user2.UserId, UserRelationAction.Invite);
                    await interactions.ChangeUserRelationAsync(user2.UserId, userToBeDeleted.UserId, UserRelationAction.Accept);

                    // setup the events that'll be deleted
                    var newEvent1 = await interactions.CreateEventAsync(userToBeDeleted.UserId, DateTime.UtcNow.AddHours(2), DateTime.UtcNow.AddHours(3), "testevent1", "will be deleted", false, MockData.Games[0].GameId);
                    var newEvent2 = await interactions.CreateEventAsync(userToBeDeleted.UserId, DateTime.UtcNow.AddHours(2), DateTime.UtcNow.AddHours(3), "testevent2", "will be deleted", true, MockData.Games[0].GameId);

                    // setup event that won't be deleted because its public and another user has joined
                    var newEvent3 = await interactions.CreateEventAsync(userToBeDeleted.UserId, DateTime.UtcNow.AddHours(2), DateTime.UtcNow.AddHours(3), "testevent3", "kept", false, MockData.Games[0].GameId);
                    await interactions.JoinEventAsync(user2.UserId, newEvent3.EventId);

                    // setup friendsonly event that will be deleted even though it has a participant
                    var newEvent4 = await interactions.CreateEventAsync(userToBeDeleted.UserId, DateTime.UtcNow.AddHours(2), DateTime.UtcNow.AddHours(3), "testevent4", "will be deleted", true, MockData.Games[0].GameId);
                    await interactions.JoinEventAsync(user2.UserId, newEvent4.EventId);

                    // create event from other user that this user signs up for
                    var newEvent5 = await interactions.CreateEventAsync(user2.UserId, DateTime.UtcNow.AddHours(2), DateTime.UtcNow.AddHours(3), "testevent5", "kept", true, MockData.Games[0].GameId);
                    await interactions.JoinEventAsync(userToBeDeleted.UserId, newEvent5.EventId);

                    // sign up for the UserChange observable
                    var userChanges = new List<UserChangedSubscriptionModel>();
                    sub1 = observables.GetUserChangeStream().Subscribe(next =>
                    {
                        userChanges.Add(next);
                    });

                    // sign up for the UserEventSignup observable
                    var signupChanges = new List<UserEventSignup>();
                    sub2 = observables.GetUserEventSignupStream().Subscribe(next =>
                    {
                        signupChanges.Add(next);
                    });

                    // sign up for the UseRelationChange observable
                    var relationChanges = new List<UserRelationChangedModel>();
                    sub3 = observables.GetUserRelationChangeStream().Subscribe(next =>
                    {
                        relationChanges.Add(next);
                    });

                    // sign up for the GameEvent observable
                    var eventChanges = new List<EventChangedModel>();
                    sub4 = observables.GetGameEventStream().Subscribe(next =>
                    {
                        eventChanges.Add(next);
                    });

                    // delete the user, so we get all the observable-events
                    await interactions.DeleteUserAsync(userToBeDeleted.UserId);

                    // verify the observation of the deleted user
                    Assert.AreEqual(1, userChanges.Count);
                    Assert.AreSame(userToBeDeleted, userChanges[0].ChangingUser);
                    Assert.AreEqual(UserAction.Deleted, userChanges[0].Action);

                    // verify the observation of the deleted signups
                    Assert.AreEqual(6, signupChanges.Count);
                    Assert.IsTrue(signupChanges.Any(n => n.EventId == newEvent1.EventId));
                    Assert.IsTrue(signupChanges.Any(n => n.EventId == newEvent2.EventId));
                    Assert.IsTrue(signupChanges.Any(n => n.EventId == newEvent3.EventId));
                    Assert.IsTrue(signupChanges.Any(n => n.EventId == newEvent4.EventId));
                    Assert.IsTrue(signupChanges.Any(n => n.EventId == newEvent5.EventId));
                    Assert.AreEqual(5, signupChanges.Count(n => n.UserId == userToBeDeleted.UserId));
                    Assert.AreEqual(1, signupChanges.Count(n => n.UserId == user2.UserId));

                    // verify the observation of the deleted relation
                    Assert.AreEqual(1, relationChanges.Count);
                    Assert.AreSame(userToBeDeleted, relationChanges[0].ActiveUser);

                    // verify the deleted events
                    Assert.AreEqual(3, eventChanges.Count);
                    Assert.IsTrue(eventChanges.Any(n => n.Event == newEvent1));
                    Assert.IsTrue(eventChanges.Any(n => n.Event == newEvent2));
                    Assert.IsTrue(eventChanges.Any(n => n.Event == newEvent4));
                    Assert.IsTrue(eventChanges.All(n => n.Action == EventAction.Deleted));
                }
            }
            finally
            {
                sub1?.Dispose();
                sub2?.Dispose();
                sub3?.Dispose();
            }
        }
    }
}
