using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using GraphQL.Types;
using PlayTogetherApi.Domain;
using PlayTogetherApi.Web.Models;

namespace PlayTogetherApi.Web.GraphQl.Types
{
    public class EventCollectionType : ObjectGraphType<EventCollectionModel>
    {
        public EventCollectionType(PlayTogetherDbContext db, IConfiguration config)
        {
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

            FieldAsync<ListGraphType<EventType>>("items",
                resolve: async context => await context.Source.EventsQuery.ToListAsync());
        }
    }
}
