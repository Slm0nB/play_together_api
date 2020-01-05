using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlayTogetherApi.Data;

namespace PlayTogetherApi.Web.Models
{
    /// <summary>
    /// The additional data primarily neeeded for the "Friends" subscription.
    /// </summary>
    public class UserRelationExtModel
    {
        // Wrapped object

        public UserRelation Relation;

        // Additional data

        public Guid PrimaryUserId;
        public UserRelationStatusAction? PrimaryUserAction;
        public UserRelationStatus? PreviousStatusForSecondaryUser;
    }
}
