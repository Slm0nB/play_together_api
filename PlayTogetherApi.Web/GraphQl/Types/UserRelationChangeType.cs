using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphQL.Types;
using Microsoft.EntityFrameworkCore;
using PlayTogetherApi.Domain;
using PlayTogetherApi.Web.Models;
using static PlayTogetherApi.Extensions.Helpers;

namespace PlayTogetherApi.Web.GraphQl.Types
{
    public class UserRelationChangeType : UserRelationType
    {
        public UserRelationChangeType(PlayTogetherDbContext db) : base(db)
        {
            Name = "RelationChange";

            Field("oldStatus", model => model.PreviousStatusForSecondaryUser, type: typeof(UserRelationStatusType)).Description("Status of the relation before the change.");
            Field("action", model => model.PrimaryUserAction, type: typeof(UserRelationStatusChangeType)).Description("The action on the relation.");
        }
    }
}
