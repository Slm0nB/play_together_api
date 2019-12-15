using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphQL.Types;
using Microsoft.EntityFrameworkCore;
using PlayTogetherApi.Data;
using PlayTogetherApi.Services;
using PlayTogetherApi.Web.Models;

namespace PlayTogetherApi.Web.GraphQl.Types
{
    public class UserRelationType : ObjectGraphType<UserRelationExtModel>
    {
        public UserRelationType(PlayTogetherDbContext db, FriendLogicService friendLogicService)
        {
            Name = "Relation";

            Field("invitedDate", model => model.Relation.CreatedDate, type: typeof(DateTimeGraphType)).Description("Invitation date.");
            Field("status", model => friendLogicService.GetStatusForUser(model.Relation, model.PrimaryUserId), type: typeof(UserRelationStatusType)).Description("Status of the relation.");

            Field<StringGraphType>("statusUser", resolve: context => {
                var model = context.Source;
                friendLogicService.ExtractStatuses(model.Relation, context.Source.PrimaryUserId, out var userFlags, out var relationFlags);
                return userFlags.ToString();
            }, deprecationReason: "Debug only");

            Field<StringGraphType>("statusFriend", resolve: context => {
                var model = context.Source;
                friendLogicService.ExtractStatuses(model.Relation, context.Source.PrimaryUserId, out var userFlags, out var relationFlags);
                return relationFlags.ToString();
            }, deprecationReason: "Debug only");

            Field<UserType>("user", resolve: context => {
                var user = context.Source.PrimaryUserId == context.Source.Relation.UserAId
                    ? context.Source.Relation.UserB
                    : context.Source.Relation.UserA;
                return user;
            });
        }
    }
}
