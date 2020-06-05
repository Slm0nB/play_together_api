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
                await MockData.PopulateDbAsync(db);

                var games = await db.Games.ToListAsync();
                Assert.AreEqual(3, games.Count);

                var users = await db.Users.ToListAsync();
                Assert.AreEqual(4, users.Count);

                var events = await db.Events.ToListAsync();
                Assert.AreEqual(5, events.Count);

                var eventSignups = await db.UserEventSignups.ToListAsync();
                Assert.AreEqual(13, eventSignups.Count);
            }
        }


        [TestMethod]
        public async Task TestInviteFriend()
        {
            using (var db = di.GetService<PlayTogetherDbContext>())
            {
                await MockData.PopulateDbAsync(db);

                var interactionsService = di.GetService<InteractionsService>();
                interactionsService.EnablePushMessages = false;

                Assert.IsNotNull(interactionsService);

                var user1 = MockData.Users[1];
                var user2 = MockData.Users[2];

                var relations = await db.UserRelations.Where(n =>
                    (n.UserAId == user1.UserId && n.UserBId == user2.UserId) || (n.UserAId == user2.UserId && n.UserBId == user1.UserId)
                ).ToListAsync();

                Assert.AreEqual(0, relations.Count);

                var res = await interactionsService.ChangeUserRelationAsync(user1.UserId, user2.UserId, UserRelationAction.Invite);

                Assert.IsNotNull(res);

                relations = await db.UserRelations.Where(n =>
                    (n.UserAId == user1.UserId && n.UserBId == user2.UserId) || (n.UserAId == user2.UserId && n.UserBId == user1.UserId)
                ).ToListAsync();

                Assert.AreEqual(1, relations.Count);
                Assert.AreEqual(user1.UserId, relations[0].UserAId);
                Assert.AreEqual(user2.UserId, relations[0].UserBId);
                Assert.AreEqual(UserRelationInternalStatus.A_Invited, relations[0].Status);
            }

        }
    }
}
