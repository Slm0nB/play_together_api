using System;

namespace PlayTogetherApi.Models
{
    /// <summary>
    /// The additional data primarily neeeded for the "Users" subscription.
    /// </summary>
    public class UserChangedSubscriptionModel
    {
        // Wrapped object
        public Data.User ChangingUser;

        public Data.UserRelation[] FriendsOfChangingUser;

        public UserAction Action;
    }
}
