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
    public class TestFriendInvitation
    {
        IServiceProvider serviceProvider;

        public TestFriendInvitation()
        {
            serviceProvider = DependencyInjection.ConfigureServices();
        }

        [TestMethod]
        public async Task TestDbContext()
        {
            // todo

            using (var db = serviceProvider.GetService<PlayTogetherDbContext>())
            {
                var games = await db.Games.ToListAsync();
                Assert.AreEqual(0, games.Count);
            }
        }
    }
}
