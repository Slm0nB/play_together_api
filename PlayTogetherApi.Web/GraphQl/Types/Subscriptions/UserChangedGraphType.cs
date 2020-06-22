using System;
using GraphQL.Types;
using PlayTogetherApi.Models;

namespace PlayTogetherApi.Web.GraphQl.Types
{
    public class UserChangedGraphType : ObjectGraphType<UserChangedSubscriptionModel>
    {
        public UserChangedGraphType()
        {
            Name = "UserChanged";

            Field<UserPreviewGraphType>("user",
                resolve: context => context.Source.ChangingUser
            );

            Field<UserActionGraphType>("action",
                resolve: context => context.Source.Action
            );
        }
    }
}
