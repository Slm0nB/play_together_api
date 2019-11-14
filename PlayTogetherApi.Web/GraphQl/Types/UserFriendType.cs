using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphQL.Types;
using Microsoft.EntityFrameworkCore;
using PlayTogetherApi.Domain;
using PlayTogetherApi.Web.Models;

namespace PlayTogetherApi.Web.GraphQl.Types
{
    public class UserFriendType : ObjectGraphType<UserRelationExtModel>
    {
        public UserFriendType(PlayTogetherDbContext db)
        {
            Name = "Friend";

            Field("date", model => model.Relation.CreatedDate, type: typeof(DateTimeGraphType)).Description("Invitation date.");

            Field<UserType>("user", resolve: context => {
                var user = context.Source.PrimaryUserId == context.Source.Relation.UserAId
                    ? context.Source.Relation.UserB
                    : context.Source.Relation.UserA;
                return user;
            });
        }
    }
}
