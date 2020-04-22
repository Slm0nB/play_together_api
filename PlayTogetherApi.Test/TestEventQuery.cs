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

            Assert.AreEqual(1, result.Count());
            Assert.AreSame(MockData.Events[1], result[0]);
        }

        [TestMethod]
        public void QueryAllEvents()
        {
            var result = (new EventsQueryService
            {
                StartsAfterDate = DateTime.UtcNow.AddYears(-10)
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

            Assert.AreEqual(1, result.Count());
            Assert.AreSame(MockData.Events[1], result[0]);
        }

        [TestMethod]
        public void QueryPastEvents()
        {
            var result = (new EventsQueryService
            {
                StartsBeforeDate = DateTime.UtcNow
            }).Process(MockData.Events.AsQueryable()).ToArray();

            Assert.AreEqual(1, result.Count());
            Assert.AreSame(MockData.Events[0], result[0]);
        }
    }
}
