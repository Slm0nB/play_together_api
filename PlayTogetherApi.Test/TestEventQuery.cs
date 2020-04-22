using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlayTogetherApi.Web.Services;

namespace PlayTogetherApi.Test
{
    [TestClass]
    public class TestEventQuery
    {
        [TestMethod]
        public void QueryDefaultEvents()
        {
            var query = MockData.Events.AsQueryable();

            var queryService = new EventsQueryService();
            query = queryService.Process(query);

            var result = query.ToArray();

            Assert.AreEqual(2, result.Count());
            Assert.AreSame(MockData.Events[2], result[0]);
            Assert.AreSame(MockData.Events[3], result[1]);
        }

        [TestMethod]
        public void QueryAllEvents()
        {
            var result = (new EventsQueryService
            {
                StartsAfterDate = DateTime.UtcNow.AddYears(-10)
            }).Process(MockData.Events.AsQueryable()).ToArray();

            Assert.AreEqual(4, result.Count());
        }


        [TestMethod]
        public void QueryTitles()
        {
            var result = (new EventsQueryService
            {
                StartsAfterDate = DateTime.UtcNow.AddYears(-10),
                SearchTerm = "Yesterday"
            }).Process(MockData.Events.AsQueryable()).ToArray();

            Assert.AreEqual(2, result.Count());
        }

        [TestMethod]
        public void QueryFutureEvents()
        {
            var result = (new EventsQueryService
            {
                StartsAfterDate = DateTime.UtcNow
            }).Process(MockData.Events.AsQueryable()).ToArray();

            Assert.AreEqual(2, result.Count());
            Assert.AreSame(MockData.Events[2], result[0]);
            Assert.AreSame(MockData.Events[3], result[1]);
        }

        [TestMethod]
        public void QueryPastEvents()
        {
            var result = (new EventsQueryService
            {
                StartsBeforeDate = DateTime.UtcNow
            }).Process(MockData.Events.AsQueryable()).ToArray();

            Assert.AreEqual(2, result.Count());
            Assert.AreSame(MockData.Events[0], result[0]);
            Assert.AreSame(MockData.Events[1], result[1]);
        }

        [TestMethod]
        public void QueryInclusiveEvents()
        {
            var result = (new EventsQueryService
            {
                StartsAfterDate = DateTime.UtcNow.AddYears(-10),
                IncludeByUsersFilter = new[] { MockData.Events[0].CreatedByUserId },
                IncludeGamesFilter = new [] { MockData.Events[3].GameId.Value }
            }).Process(MockData.Events.AsQueryable()).ToArray();

            Assert.AreEqual(2, result.Count());
            Assert.AreSame(MockData.Events[0], result[0]);
            Assert.AreSame(MockData.Events[3], result[1]);
        }

        [TestMethod]
        public void QueryExclusiveEvents()
        {
            var result = (new EventsQueryService
            {
                StartsAfterDate = DateTime.UtcNow.AddYears(-10),
                OnlyByUsersFilter = new[] { MockData.Events[0].CreatedByUserId },
                OnlyGamesFilter = new[] { MockData.Events[3].GameId.Value }
            }).Process(MockData.Events.AsQueryable()).ToArray();

            Assert.AreEqual(0, result.Count());

            result = (new EventsQueryService
            {
                StartsAfterDate = DateTime.UtcNow.AddYears(-10),
                OnlyByUsersFilter = new[] { MockData.Events[1].CreatedByUserId },
                OnlyGamesFilter = new[] { MockData.Events[1].GameId.Value, MockData.Events[3].GameId.Value }
            }).Process(MockData.Events.AsQueryable()).ToArray();

            Assert.AreEqual(1, result.Count());
            Assert.AreSame(MockData.Events[1], result[0]);
        }

        [TestMethod]
        public void QueryPrivateEvents()
        {
            var result = (new EventsQueryService
            {
                OnlyPrivateFilter = true,
                UserId = MockData.Users[0].UserId,
                FriendIds = MockData.Users.Select(n => n.UserId).ToList()
            }).Process(MockData.Events.AsQueryable()).ToArray();

            Assert.AreEqual(1, result.Count());
            Assert.AreSame(MockData.Events[4], result[0]);
        }
    }
}
