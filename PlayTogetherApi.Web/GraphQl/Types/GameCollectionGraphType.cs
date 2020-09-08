using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using GraphQL.Types;
using PlayTogetherApi.Data;

namespace PlayTogetherApi.Web.GraphQl.Types
{
    public class GameCollectionGraphType : ObjectGraphType<IQueryable<Game>>
    {
        public GameCollectionGraphType()
        {
            Name = "GameCollection";

            FieldAsync<IntGraphType>("total",
                description: "The total number of games available",
                resolve: async context =>
                {
                    var db = context.RequestServices.GetService<PlayTogetherDbContext>();

                    var total = await db.Games.CountAsync();
                    return total;
                }
            );

            FieldAsync<IntGraphType>("count",
                description: "The number of games selected by the query",
                resolve: async context =>
                {
                    var count = await context.Source.CountAsync();
                    return count;
                }
            );

            FieldAsync<ListGraphType<GameGraphType>>("items",
                resolve: async context => await context.Source.ToListAsync());
        }
    }
}
