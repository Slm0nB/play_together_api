﻿using GraphQL.Types;
using PlayTogetherApi.Services;
using PlayTogetherApi.Models;

namespace PlayTogetherApi.Web.GraphQl.Types
{
    public class UserRelationGraphType : ObjectGraphType<UserRelationExtModel>
    {
        public UserRelationGraphType(FriendLogicService friendLogicService)
        {
            Name = "UserRelation";

            Field("invitedDate", model => model.Relation.CreatedDate, type: typeof(DateTimeGraphType)).Description("Invitation date.");
            Field("status", model => friendLogicService.GetStatusForUser(model.Relation, model.ActiveUserId), type: typeof(UserRelationStatusGraphType)).Description("Status of the relation.");

            Field<StringGraphType>("statusUser", resolve: context => {
                var model = context.Source;
                friendLogicService.ExtractStatuses(model.Relation, context.Source.ActiveUserId, out var userFlags, out var relationFlags);
                return userFlags.ToString();
            }, deprecationReason: "Debug only");

            Field<StringGraphType>("statusFriend", resolve: context => {
                var model = context.Source;
                friendLogicService.ExtractStatuses(model.Relation, context.Source.ActiveUserId, out var userFlags, out var relationFlags);
                return relationFlags.ToString();
            }, deprecationReason: "Debug only");

            Field<UserGraphType>("user", resolve: context => {
                var user = context.Source.ActiveUserId == context.Source.Relation.UserAId
                    ? context.Source.Relation.UserB
                    : context.Source.Relation.UserA;
                return user;
            });
        }
    }
}
