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
    public sealed class TestUserStatisticsObservable
    {
        DependencyInjection di;

        public TestUserStatisticsObservable()
        {
            di = new DependencyInjection();
        }

        /// <summary>
        /// Test that unsubscribing from a UserStatistics observable causes it to remove itself from the collection,
        /// so the service that updates it doens't have to compute it any more.
        /// </summary>
        [TestMethod]
        public void TestStatisticsCleanUp()
        {
            IDisposable sub1 = null;
            try
            {
                var observables = di.GetService<ObservablesService>();

                Assert.AreEqual(0, observables.UserStatisticsStreams.Count);

                sub1 = observables.GetUserStatisticsStream(MockData.Users[0].UserId, true)
                    .Subscribe(model => { });

                Assert.AreEqual(1, observables.UserStatisticsStreams.Count);

                sub1.Dispose();

                Assert.AreEqual(0, observables.UserStatisticsStreams.Count);
            }
            finally
            {
                sub1?.Dispose();
            }
        }
    }
}
