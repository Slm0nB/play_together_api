using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlayTogetherApi.Data;
using PlayTogetherApi.Services;

namespace PlayTogetherApi.Test
{
    [TestClass]
    public class TestFriendLogic
    {
        FriendLogicService friendLogic = new FriendLogicService();

        [TestMethod]
        public void TestExtractStatuses()
        {
            var user1Id = Guid.NewGuid();
            var user2Id = Guid.NewGuid();
            var relation = new UserRelation
            {
                UserAId = user1Id,
                UserBId = user2Id,
                Status = UserRelationInternalStatus.A_Rejected | UserRelationInternalStatus.B_Invited
            };

            // Extract status flags for userA.
            // This is the way they're stored in the relation, so we expect them to simply be masked out.
            friendLogic.ExtractStatuses(relation, user1Id, out var userFlags, out var relationFlags);
            Assert.AreEqual(UserRelationInternalStatus.A_Rejected, userFlags);
            Assert.AreEqual(UserRelationInternalStatus.B_Invited, relationFlags);

            // Extract status flags for userB.
            // This is the opposite of how they're stored in the relation, so we expecte them to be masked and swapped.
            friendLogic.ExtractStatuses(relation, user2Id, out userFlags, out relationFlags);
            Assert.AreEqual(UserRelationInternalStatus.A_Invited, userFlags);
            Assert.AreEqual(UserRelationInternalStatus.B_Rejected, relationFlags);
        }
    }
}
