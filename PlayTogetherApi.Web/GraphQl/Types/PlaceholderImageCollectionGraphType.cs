using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using GraphQL.Types;
using PlayTogetherApi.Data;

namespace PlayTogetherApi.Web.GraphQl.Types
{
    public class PlaceholderImageCollectionGraphType : ObjectGraphType<IQueryable<PlaceholderImage>>
    {
        public PlaceholderImageCollectionGraphType()
        {
            Name = "PlaceholderImageCollection";

            FieldAsync<IntGraphType>("total",
                description: "The total number of images available",
                resolve: async context =>
                {
                    var db = context.RequestServices.GetService<PlayTogetherDbContext>();

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
