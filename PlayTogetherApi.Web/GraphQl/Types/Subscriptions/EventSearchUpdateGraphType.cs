using System;
using GraphQL.Types;
using PlayTogetherApi.Models;

namespace PlayTogetherApi.Web.GraphQl.Types
{
    public class EventSearchUpdateGraphType : ObjectGraphType<EventSearchUpdateModel>
    {
        public EventSearchUpdateGraphType()
        {
            Name = "EventSearchUpdate";

            Field<ListGraphType<EventGraphType>>("added",
                resolve: context => context.Source.Added
            );

            Field<ListGraphType<EventGraphType>>("removed",
                resolve: context => context.Source.Removed
            );
        }
    }
}
