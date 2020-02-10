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
    public class UserRelationChangeType : ObjectGraphType<UserRelationChangedExtModel>
    {
        public UserRelationChangeType(/*PlayTogetherDbContext db, */FriendLogicService friendLogicService)
        {
            Name = "UserRelationChange";

            Field("action", model => model.ActiveUserAction, type: typeof(UserRelationActionType)).Description("The action on the relation.");

            Field<UserPreviewType>("sendingUser", resolve: context => context.Source.ActiveUser, description: "The user that triggered the action. This can be the subscriber (if excludeChangesFromCaller is false) or another user.");
            Field<UserPreviewType>("targetUser", resolve: context => context.Source.TargetUser, description: "The user targetted by the action. This can be the subscriber or another user (if excludeChangesFromCaller is false).");
            Field<UserPreviewType>("otherUser", resolve: context => context.Source.ActiveUser.UserId == context.Source.SubscribingUserId ? context.Source.TargetUser : context.Source.ActiveUser, description: "The sending or target user that is NOT the subscriber.");

            Field<BooleanGraphType>("sentBySubscriber", resolve: context => context.Source.ActiveUser.UserId == context.Source.SubscribingUserId, description: "If the relation-change was sent by the subscriber.");
            Field<UserRelationStatusType>("statusSubscriber", resolve: context => friendLogicService.GetStatusForUser(context.Source.Relation, context.Source.SubscribingUserId), description: "The updated relation status for the subscriber");
        }
    }
}
