using System;
using GraphQL.Types;
using PlayTogetherApi.Models;

namespace PlayTogetherApi.Web.GraphQl.Types
{
    public class EventChangedGraphType : ObjectGraphType<EventChangedModel>
    {
        public EventChangedGraphType()
        {
            Name = "EventChanged";

            Field<EventGraphType>("event",
                resolve: context => context.Source.Event
            );

            Field<UserGraphType>("actionBy",
                resolve: context => context.Source.ChangingUser
            );

            Field<EventActionGraphType>("action",
                resolve: context => context.Source.Action
            );
        }
    }
}
