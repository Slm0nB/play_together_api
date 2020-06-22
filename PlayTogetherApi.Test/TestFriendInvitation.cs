using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlayTogetherApi.Data;
using PlayTogetherApi.Services;
using PlayTogetherApi.Web.Models;

namespace PlayTogetherApi.Test
{
    [TestClass]
    public sealed class TestFriendInvitation : IDisposable
    {
        DependencyInjection di;

        public TestFriendInvitation()
        {
            di = new DependencyInjection();
        }

        public void Dispose()
        {
            di?.Dispose();
        }

        [TestMethod]
        public async Task TestDbContext()
        {
            using (var db = di.GetService<PlayTogetherDbContext>())
            {
                await MockData.PopulateDbAsync(db, true);

                var games = await db.Games.ToListAsync();
                Assert.AreEqual(MockData.Games.Length, games.Count);

                var users = await db.Users.ToListAsync();
                Assert.AreEqual(MockData.Users.Length, users.Count);

                var events = await db.Events.ToListAsync();
                Assert.AreEqual(MockData.Events.Length, events.Count);

                var eventSignups = await db.UserEventSignups.ToListAsync();
                Assert.AreEqual(MockData.Events.SelectMany(n => n.Signups).Distinct().Count(), eventSignups.Count);
            }
        }

        [TestMethod]
        public async Task TestInviteFriendAndAcceptInvitation()
        {
            IDisposable sub1 = null, sub2 = null;
            PlayTogetherDbContext db = di.GetService<PlayTogetherDbContext>();
            try
            {
                await MockData.PopulateDbAsync(db);

                var observables = di.GetService<ObservablesService>();
                var friendLogic = di.GetService<FriendLogicService>();
                var interactionsService = di.GetService<InteractionsService>();

                Assert.IsNotNull(interactionsService);

                var user1 = MockData.Users[1];
                var user2 = MockData.Users[2];

                // Make sure there weren't any relations in the db already
                var relations = await db.UserRelations.Where(n =>
                    (n.UserAId == user1.UserId && n.UserBId == user2.UserId) || (n.UserAId == user2.UserId && n.UserBId == user1.UserId)
                ).ToListAsync();
                Assert.AreEqual(0, relations.Count);

                // Set up observers of relation-changes for each user
                UserRelationChangedExtModel userChange1 = null;
                UserRelationChangedExtModel userChange2 = null;
                sub1 = observables.GetRelationChangedSubscription(user1.UserId, false).Subscribe(model =>
                {
                    userChange1 = model;
                });
                sub2 = observables.GetRelationChangedSubscription(user2.UserId, false).Subscribe(model =>
                {
                    userChange2 = model;
                });

                // Create the invitation
                var relationExt = await interactionsService.ChangeUserRelationAsync(user1.UserId, user2.UserId, UserRelationAction.Invite);

                // The extended relation should reflect that they had no relation earlier
                Assert.IsNotNull(relationExt);
                Assert.AreEqual(user1.UserId, relationExt.Relation.UserAId);
                Assert.AreEqual(user2.UserId, relationExt.Relation.UserBId);
                Assert.AreEqual(UserRelationInternalStatus.A_Invited, relationExt.Relation.Status);
                Assert.AreEqual(UserRelationStatus.None, relationExt.PreviousStatusForTargetUser);

                // User1 should be getting an update
                Assert.IsNotNull(userChange1);
                Assert.AreEqual(user1.UserId, userChange1.SubscribingUserId);
                Assert.AreEqual(UserRelationAction.Invite, userChange1.ActiveUserAction);
                Assert.AreEqual(user1.UserId, userChange1.Relation.UserAId);
                Assert.AreEqual(user2.UserId, userChange1.Relation.UserBId);
                Assert.AreEqual(UserRelationInternalStatus.A_Invited, userChange1.Relation.Status);
                Assert.AreEqual(UserRelationStatus.Inviting, friendLogic.GetStatusForUser(userChange1.Relation, user1.UserId));

                // User2 should be getting an update
                Assert.IsNotNull(userChange2);
                Assert.AreEqual(user2.UserId, userChange2.SubscribingUserId);
                Assert.AreEqual(UserRelationAction.Invite, userChange2.ActiveUserAction);
                Assert.AreEqual(user1.UserId, userChange2.Relation.UserAId);
                Assert.AreEqual(user2.UserId, userChange2.Relation.UserBId);
                Assert.AreEqual(UserRelationInternalStatus.A_Invited, userChange2.Relation.Status);
                Assert.AreEqual(UserRelationStatus.Invited, friendLogic.GetStatusForUser(userChange2.Relation, user2.UserId));

                // The db should now contain a relation
                relations = await db.UserRelations.Where(n =>
                    (n.UserAId == user1.UserId && n.UserBId == user2.UserId) || (n.UserAId == user2.UserId && n.UserBId == user1.UserId)
                ).ToListAsync();
                Assert.AreEqual(1, relations.Count);
                Assert.AreEqual(user1.UserId, relations[0].UserAId);
                Assert.AreEqual(user2.UserId, relations[0].UserBId);
                Assert.AreEqual(UserRelationInternalStatus.A_Invited, relations[0].Status);

                userChange1 = userChange2 = null;

                // Accept the invitation
                relationExt = await interactionsService.ChangeUserRelationAsync(user2.UserId, user1.UserId, UserRelationAction.Accept);

                // The extended relation should reflect that their previous state was an invitation
                Assert.IsNotNull(relationExt);
                Assert.AreEqual(user1.UserId, relationExt.Relation.UserAId);
                Assert.AreEqual(user2.UserId, relationExt.Relation.UserBId);
                Assert.AreEqual(UserRelationInternalStatus.A_Befriended | UserRelationInternalStatus.B_Befriended, relationExt.Relation.Status);
                Assert.AreEqual(UserRelationStatus.Inviting, relationExt.PreviousStatusForTargetUser);

                // User1 should be getting an update
                Assert.IsNotNull(userChange1);
                Assert.AreEqual(user1.UserId, userChange1.SubscribingUserId);
                Assert.AreEqual(UserRelationAction.Accept, userChange1.ActiveUserAction);
                Assert.AreEqual(user1.UserId, userChange1.Relation.UserAId);
                Assert.AreEqual(user2.UserId, userChange1.Relation.UserBId);
                Assert.AreEqual(UserRelationInternalStatus.A_Befriended | UserRelationInternalStatus.B_Befriended, userChange1.Relation.Status);
                Assert.AreEqual(UserRelationStatus.Friends, friendLogic.GetStatusForUser(userChange1.Relation, user1.UserId));

                // User2 should be getting an update
                Assert.IsNotNull(userChange2);
                Assert.AreEqual(user2.UserId, userChange2.SubscribingUserId);
                Assert.AreEqual(UserRelationAction.Accept, userChange2.ActiveUserAction);
                Assert.AreEqual(user1.UserId, userChange2.Relation.UserAId);
                Assert.AreEqual(user2.UserId, userChange2.Relation.UserBId);
                Assert.AreEqual(UserRelationInternalStatus.A_Befriended | UserRelationInternalStatus.B_Befriended, userChange2.Relation.Status);
                Assert.AreEqual(UserRelationStatus.Friends, friendLogic.GetStatusForUser(userChange2.Relation, user2.UserId));

                // The db should still contain a relation
                relations = await db.UserRelations.Where(n =>
                    (n.UserAId == user1.UserId && n.UserBId == user2.UserId) || (n.UserAId == user2.UserId && n.UserBId == user1.UserId)
                ).ToListAsync();
                Assert.AreEqual(1, relations.Count);
                Assert.AreEqual(user1.UserId, relations[0].UserAId);
                Assert.AreEqual(user2.UserId, relations[0].UserBId);
                Assert.AreEqual(UserRelationInternalStatus.A_Befriended | UserRelationInternalStatus.B_Befriended, relations[0].Status);
            }
            finally
            {
                db?.Dispose();
                sub1?.Dispose();
                sub2?.Dispose();
            }
        }
    }
}
