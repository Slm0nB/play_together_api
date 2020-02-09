using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlayTogetherApi.Data;

namespace PlayTogetherApi.Web.Models
{
    public class UserRelationExtModel
    {
        // Wrapped object

        public UserRelation Relation;

        // Additional data

        public Guid ActiveUserId;
        public UserRelationAction? ActiveUserAction;
        public UserRelationStatus? PreviousStatusForTargetUser;
    }
}
