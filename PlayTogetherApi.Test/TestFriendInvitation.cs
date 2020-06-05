using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlayTogetherApi.Data;
using PlayTogetherApi.Services;

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
            // todo

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
    }
}
