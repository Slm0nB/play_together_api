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
    public class UserRelationType : ObjectGraphType<UserRelationExtModel>
    {
        public UserRelationType(PlayTogetherDbContext db)
        {
            Name = "Relation";

            Field("date", model => model.Relation.CreatedDate, type: typeof(DateTimeGraphType)).Description("Invitation date.");
            Field("status", model => model.Relation.GetStatusForUser(model.PrimaryUserId), type: typeof(UserRelationStatusType)).Description("Status of the relation.");

            Field<UserType>("user", resolve: context => {
                var user = context.Source.PrimaryUserId == context.Source.Relation.UserAId
                    ? context.Source.Relation.UserB
                    : context.Source.Relation.UserA;
                return user;
            });
        }
    }
}
