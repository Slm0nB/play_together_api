using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using GraphQL.Types;
using PlayTogetherApi.Domain;

namespace PlayTogetherApi.Web.GraphQl.Types
{
    public class EventCollectionType : ObjectGraphType<IQueryable<Event>>
    {
        public EventCollectionType(PlayTogetherDbContext db, IConfiguration config)
        {
            FieldAsync<IntGraphType>("total",
                description: "The total number of events created",
                resolve: async context =>
                {
                    var total = await db.Events.CountAsync();
                    return total;
                }
            );

            FieldAsync<IntGraphType>("count",
                description: "The number of events selected by the query",
                resolve: async context =>
                {
                    var count = await context.Source.CountAsync();
                    return count;
                }
            );

            FieldAsync<ListGraphType<EventType>>("items",
                resolve: async context => await context.Source.ToListAsync());
        }
    }
}
