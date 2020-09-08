using Microsoft.EntityFrameworkCore;
using GraphQL.Types;
using PlayTogetherApi.Web.Models;

namespace PlayTogetherApi.Web.GraphQl.Types
{
    public class EventCollectionGraphType : ObjectGraphType<EventCollectionModel>
    {
        public EventCollectionGraphType()
        {
            Name = "EventCollection";

            FieldAsync<IntGraphType>("total",
                description: "The total number of events available",
                resolve: async context =>
                {
                    var total = await context.Source.TotalEventsQuery.CountAsync();
                    return total;
                }
            );

            FieldAsync<IntGraphType>("count",
                description: "The number of events selected by the query",
                resolve: async context =>
                {
                    var count = await context.Source.EventsQuery.CountAsync();
                    return count;
                }
            );

            FieldAsync<ListGraphType<EventGraphType>>("items",
                resolve: async context => await context.Source.EventsQuery.ToListAsync());
        }
    }
}
