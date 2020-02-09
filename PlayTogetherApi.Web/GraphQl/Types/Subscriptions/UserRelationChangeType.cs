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

            Field<UserPreviewType>("sendingUser", resolve: context => context.Source.ActiveUser);
            Field<UserPreviewType>("targetUser", resolve: context => context.Source.TargetUser);
            Field<UserPreviewType>("otherUser", resolve: context => context.Source.ActiveUser.UserId == context.Source.SubscribingUserId ? context.Source.TargetUser : context.Source.ActiveUser);

            Field<BooleanGraphType>("sentBySubscriber", resolve: context => context.Source.ActiveUser.UserId == context.Source.SubscribingUserId);
            Field<UserRelationStatusType>("statusSubscriber", resolve: context => friendLogicService.GetStatusForUser(context.Source.Relation, context.Source.SubscribingUserId));
        }
    }
}
