using System;
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
    public class UserEventSignupCollectionGraphType : ObjectGraphType<UserEventSignupCollectionModel>
    {
        public UserEventSignupCollectionGraphType(PlayTogetherDbContext db, IConfiguration config)
        {
            Name = "UserEventSignupCollection";

            FieldAsync<IntGraphType>("total",
                description: "The total number of signup available",
                resolve: async context =>
                {
                    var total = await context.Source.TotalItemsQuery.CountAsync();
                    return total;
                }
            );

            FieldAsync<IntGraphType>("count",
                description: "The number of signups selected by the query",
                resolve: async context =>
                {
                    var count = await context.Source.ItemsQuery.CountAsync();
                    return count;
                }
            );

            FieldAsync<ListGraphType<UserEventSignupGraphType>>("items",
                resolve: async context => await context.Source.ItemsQuery.ToListAsync());
        }
    }
}
