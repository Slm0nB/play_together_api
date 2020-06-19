using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data = PlayTogetherApi.Data;

namespace PlayTogetherApi.Web.Models
{
    /// <summary>
    /// The additional data primarily neeeded for the "Events" subscription.
    /// </summary>
    public class EventChangedModel
    {
        // Wrapped object

        public Data.Event Event;

        // Additional data

        public Data.User ChangingUser;
        public Data.UserRelation[] FriendsOfChangingUser;
        public EventAction? Action;

        public Guid? RecipientUserId;
    }
}
