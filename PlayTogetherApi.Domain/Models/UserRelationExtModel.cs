using System;
using PlayTogetherApi.Data;

namespace PlayTogetherApi.Models
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
