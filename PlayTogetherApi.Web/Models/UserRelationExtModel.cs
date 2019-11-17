using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlayTogetherApi.Domain;

namespace PlayTogetherApi.Web.Models
{
    public class UserRelationExtModel
    {
        public UserRelation Relation;
        public Guid PrimaryUserId;
        public UserRelationStatusChange? PrimaryUserAction;
        public UserRelationStatus? PreviousStatusForSecondaryUser;
    }
}
