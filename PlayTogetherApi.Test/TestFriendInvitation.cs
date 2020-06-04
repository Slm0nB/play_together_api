using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlayTogetherApi.Data;
using PlayTogetherApi.Services;

namespace PlayTogetherApi.Test
{
    [TestClass]
    public class TestFriendInvitation
    {
        ObservablesService observables;
        FriendLogicService friendLogic = new FriendLogicService();

        public TestFriendInvitation(ObservablesService observables)
        {
            this.observables = observables;
        }

        [TestMethod]
        public void TestInviteFriend()
        {
            // todo

        }
    }
}
