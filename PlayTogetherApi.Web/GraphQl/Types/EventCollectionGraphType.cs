﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using GraphQL.Types;
using PlayTogetherApi.Data;
using PlayTogetherApi.Web.Models;

namespace PlayTogetherApi.Web.GraphQl.Types
{
    public class EventCollectionGraphType : ObjectGraphType<EventCollectionModel>
    {
        public EventCollectionGraphType(PlayTogetherDbContext db, IConfiguration config)
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