using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data = PlayTogetherApi.Data;

namespace PlayTogetherApi.Web.Models
{
    /// <summary>
    /// The additional data primarily neeeded for the "Users" subscription.
    /// </summary>
    public class UserChangedSubscriptionModel
    {
        // Wrapped object
        public Data.User ChangingUser;

        public Data.UserRelation[] FriendsOfChangingUser;

        public bool IsDeleted = false;
    }
}
