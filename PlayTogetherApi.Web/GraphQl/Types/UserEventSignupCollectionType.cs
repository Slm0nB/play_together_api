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
    public class UserEventSignupCollectionType : ObjectGraphType<UserEventSignupCollectionModel>
    {
        public UserEventSignupCollectionType(PlayTogetherDbContext db, IConfiguration config)
        {
            FieldAsync<IntGraphType>("total",
                description: "The total number of signup available",
                resolve: async context =>
                {
                    var total = await context.Source.TotalSignupsQuery.CountAsync();
                    return total;
                }
            );

            FieldAsync<IntGraphType>("count",
                description: "The number of signups selected by the query",
                resolve: async context =>
                {
                    var count = await context.Source.SignupsQuery.CountAsync();
                    return count;
                }
            );

            FieldAsync<ListGraphType<UserEventSignupType>>("items",
                resolve: async context => await context.Source.SignupsQuery.ToListAsync());
        }
    }
}
