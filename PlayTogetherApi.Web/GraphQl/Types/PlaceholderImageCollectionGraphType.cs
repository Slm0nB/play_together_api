using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using GraphQL.Types;
using PlayTogetherApi.Data;

namespace PlayTogetherApi.Web.GraphQl.Types
{
    public class PlaceholderImageCollectionGraphType : ObjectGraphType<IQueryable<PlaceholderImage>>
    {
        public PlaceholderImageCollectionGraphType(PlayTogetherDbContext db, IConfiguration config)
        {
            Name = "PlaceholderImageCollection";

            FieldAsync<IntGraphType>("total",
                description: "The total number of images available",
                resolve: async context =>
                {
                    var total = await db.PlaceholderImages.CountAsync();
                    return total;
                }
            );

            FieldAsync<IntGraphType>("count",
                description: "The number of images selected by the query",
                resolve: async context =>
                {
                    var count = await context.Source.CountAsync();
                    return count;
                }
            );

            FieldAsync<ListGraphType<PlaceholderImageGraphType>>("items",
                resolve: async context => await context.Source.ToListAsync());
        }
    }
}
